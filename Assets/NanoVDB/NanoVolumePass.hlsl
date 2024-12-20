#ifndef NANO_VOLUME_PASS
#define NANO_VOLUME_PASS

#define MIN_TRANSMITTANCE   0.05
#define MIN_DENSITY         0.01
#define CLOUD_COLOR         float3(1, 1, 1)

#define COLOR_NONE		    float4(0, 0, 0, 0)
#define COLOR_RED           float4(1, 0, 0, 1)
#define COLOR_GREEN         float4(0, 1, 0, 1)
#define COLOR_BLUE          float4(0, 0, 1, 1)

#define PNANOVDB_HLSL
#include "PNanoVDB.hlsl"

uniform pnanovdb_buf_t buf : register(t1);

uniform float4	_LightDir;

uniform float3  _Light;
uniform float3  _Scattering;

uniform float	_DensityScale;
uniform float	_LightRayLength;
uniform float	_LightAbsorbation;
uniform float	_ClipPlaneMin;
uniform float	_ClipPlaneMax;

uniform int		_RayMarchSamples;
uniform int		_LightSamples;

uniform int     _VisualizeSteps;

// For Temporal pass
uniform float   _Offset;
uniform int     _FrameIndex;

struct Ray
{
    float3 origin;
    float3 direction;
    float tmin;
    float tmax;
};

struct NanoVolume
{
    pnanovdb_grid_handle_t  grid;
    pnanovdb_grid_type_t    grid_type;
    pnanovdb_readaccessor_t acc;
};

void initVolume(inout NanoVolume volume)
{
    pnanovdb_grid_handle_t  grid        = { {0} };
    pnanovdb_grid_type_t    grid_type   = pnanovdb_buf_read_uint32(buf, PNANOVDB_GRID_OFF_GRID_TYPE);
    pnanovdb_tree_handle_t  tree        = pnanovdb_grid_get_tree(buf, grid);
	pnanovdb_root_handle_t  root        = pnanovdb_tree_get_root(buf, tree);
    pnanovdb_readaccessor_t acc;
	pnanovdb_readaccessor_init(acc, root);

    volume.grid = grid;
    volume.grid_type = grid_type;
    volume.acc = acc;
}

float get_value_coord(inout pnanovdb_readaccessor_t acc, pnanovdb_vec3_t pos)
{
    pnanovdb_coord_t ijk = pnanovdb_hdda_pos_to_ijk(pos);
    pnanovdb_address_t address = pnanovdb_readaccessor_get_value_address(PNANOVDB_GRID_TYPE_FLOAT, buf, acc, ijk);
    return pnanovdb_read_float(buf, address);
}

uint get_dim_coord(inout pnanovdb_readaccessor_t acc, pnanovdb_vec3_t pos)
{
	pnanovdb_coord_t ijk = pnanovdb_hdda_pos_to_ijk(pos);
	return pnanovdb_readaccessor_get_dim(PNANOVDB_GRID_TYPE_FLOAT, buf, acc, ijk);
}

bool get_hdda_hit(inout NanoVolume volume, inout Ray ray, inout float valueAtHit)
{
    float thit;
	bool hit = pnanovdb_hdda_tree_marcher(
		volume.grid_type,
		buf,
		volume.acc,
		ray.origin, ray.tmin,
		ray.direction, ray.tmax,
		thit,
		valueAtHit
	);
    ray.tmin = thit;
    return hit;
}

// Not used
float hdda_light_step(float3 cloud_sample_pos, Ray sun_ray, inout NanoVolume volume)
{
    if (_LightSamples < 1) { return 0; }

    float not_used;
    bool hit = get_hdda_hit(volume, sun_ray, not_used);

    float3 hit_pos = sun_ray.origin + sun_ray.direction * sun_ray.tmin;
    float distance = length(hit_pos - cloud_sample_pos);
    float density = _DensityScale * distance;

    return density;
}

float light_step_exp(float3 pos, inout NanoVolume volume)
{
    if (_LightSamples < 1) { return 0; }

	float acc_d = 0;
    float step_size = 1;
    float light_dir = -(_LightDir.xyz);

    int step = 0;
    while (step < _LightSamples)
    {
        float3 sample_pos = pos + step_size * light_dir;
        float d = get_value_coord(volume.acc, sample_pos);
        d *= step_size * _DensityScale;
        acc_d += d;

        step_size *= 2;
        step++;
    }
    return acc_d;
}

float beers_Law(float light_density, float3 extinction)
{
    if (light_density <= 0) { return 1; }
    return exp(-light_density * extinction);
}

// Hillaire 2015 analytical integration with implementation by Sebastian Gaida
void scatter_integration(
    inout float3 scatteredLuminance,
    inout float3 transmittance,
    float3 luminance,
    float3 extinction,
    float density,
    float deltaStepSize
)
{
    float3 sampleExtinction = max(float(0.00001), density * extinction);
    float3 sampleTransmittance = exp(-sampleExtinction * deltaStepSize);
    float3 scatterIntegration = (luminance - (luminance * sampleTransmittance)) / sampleExtinction;
    scatteredLuminance += transmittance * scatterIntegration;
    transmittance *= sampleTransmittance;
}

float4 raymarch_volume(Ray ray, inout NanoVolume volume, float step_size)
{
    float acc_density   = 0;
    float transmittance = 1;
    float phase         = 1;
    float3 light_energy = 0;
    float3 extinction   = _Scattering * _LightAbsorbation;

    float not_used;
    bool hit = get_hdda_hit(volume, ray, not_used);
    if (!hit) { return COLOR_NONE; }

    // Every other frame, we start half a step in. Meaning we will hopefully hit
    // voxels that were missed in the previous frame.
    if (_FrameIndex == 0)
    {
        ray.tmin += step_size / 2;
    }

    int step = 0;
    while (step < _RayMarchSamples)
    {
        if (ray.tmin >= ray.tmax)
        {
            break;
        }

        // read density from ray position
        float3  pos = ray.origin + ray.direction * ray.tmin;
        float   d   = get_value_coord(volume.acc, pos);

        // Skip empty space.
        uint dim = get_dim_coord(volume.acc, pos);
        if (d < MIN_DENSITY && dim > 1)
        {
            step++;
            ray.tmin += step_size * 1;
            continue;
        }
        if (d < MIN_DENSITY)
        {
            step++;
            ray.tmin += step_size * 1;
            continue;
        }

        // "interpolate" density from some neighbors
        float   d2  = get_value_coord(volume.acc, pos + float3( 1,  0,  0));
        float   d3  = get_value_coord(volume.acc, pos + float3(-1,  0,  0));
        float   d4  = get_value_coord(volume.acc, pos + float3( 0,  1,  0));
        float   d5  = get_value_coord(volume.acc, pos + float3( 0, -1,  0));
        float   d6  = get_value_coord(volume.acc, pos + float3( 0,  0,  1));
        float   d7  = get_value_coord(volume.acc, pos + float3( 0,  0, -1));
        float avg_d = (d + d2 + d3 + d4 + d5 + d6 + d7) / 7;
        d = avg_d;

        d *= _DensityScale;
        acc_density += d;

        float light_density = light_step_exp(pos, volume);
        float light_transmittance = beers_Law(light_density, extinction);
        float3 luminance = _Light * _Scattering * light_transmittance * phase * d;
        scatter_integration(light_energy, transmittance, luminance, extinction, d, step_size);

        // Early out "half way", other half was done in the previous frame.
        if (acc_density > 0.5)
        {
            break;
        }

        if (transmittance < MIN_TRANSMITTANCE)
        {
            transmittance = 0;
            break;
        }

        step++;
        ray.tmin += step_size;
    }

    // Low step count will be blue, high red.
    if (_VisualizeSteps == 1)
    {
        float t = float(step) / float(_RayMarchSamples);
        if (step <= 0)
        {
            return COLOR_NONE;
        }
        float3 final_color = lerp(COLOR_BLUE, COLOR_RED, t);
        return float4(final_color, 1);
    }

    float3 final_color = saturate(CLOUD_COLOR * transmittance + light_energy) * acc_density;
    return float4(final_color, acc_density);
}

float4 NanoVolumePass(float3 origin, float3 direction)
{
    NanoVolume volume; initVolume(volume);

    Ray ray;
    ray.origin = origin;
    ray.direction = direction;
    ray.tmin = _ClipPlaneMin;
    ray.tmax = _ClipPlaneMax;
    
    float step_size = 2;
    float4 final_color = raymarch_volume(ray, volume, step_size);
    return float4(final_color);
}

#endif // NANO_VOLUME_PASS
