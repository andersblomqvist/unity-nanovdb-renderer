#ifndef NANO_VOLUME_PASS
#define NANO_VOLUME_PASS

#define MIN_TRANSMITTANCE   0.01
#define MIN_DENSITY         0.001
#define CLOUD_COLOR         float3(0.8, 0.8, 0.8)

#define COLOR_RED           float4(1, 0, 0, 1)
#define COLOR_GREEN         float4(0, 1, 0, 1)
#define COLOR_BLUE          float4(0, 0, 1, 1)
#define COLOR_NONE		    float4(0, 0, 0, 0)

#define PNANOVDB_HLSL
#include "PNanoVDB.hlsl"

uniform pnanovdb_buf_t buf : register(t1);

uniform float4	_LightDir;

uniform float	_DensityScale;
uniform float	_LightRayLength;
uniform float	_LightAbsorbation;
uniform float	_ClipPlaneMin;
uniform float	_ClipPlaneMax;

uniform int		_RayMarchSamples;
uniform int		_LightSamples;

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

int get_dim_coord(inout pnanovdb_readaccessor_t acc, pnanovdb_vec3_t pos)
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

float light_step(float3 pos, inout NanoVolume volume)
{
    if (_LightSamples < 1) { return 0; }

	float acc_d = 0;
    float step_size = _LightRayLength / _LightSamples;
    float light_dir = -(_LightDir.xyz);

    for (float t = 0; t < _LightRayLength; t += step_size)
	{
		float3 sample_pos = pos + t * light_dir;
		float d = get_value_coord(volume.acc, sample_pos);
        d *= step_size * _DensityScale;
        acc_d += max(0, d);
	}
    return acc_d;
}

float Beers_Law(float light_density)
{
    if (light_density <= 0) { return 1; }
    return exp(light_density * -_LightAbsorbation);
}

float4 NanoVolumePass(float3 origin, float3 direction)
{
    NanoVolume volume; initVolume(volume);

    Ray ray;
    ray.origin = origin;
    ray.direction = direction;
    ray.tmin = _ClipPlaneMin;
    ray.tmax = _ClipPlaneMax;

    // start ray with HDDA
    float not_used;
    bool hit = get_hdda_hit(volume, ray, not_used);
    if (!hit)
	{
		return COLOR_NONE;
	}

    float3 light_energy = 0;
    float transmittance = 1;
    float acc_density = 0;

    // from here on, we ray march at the ray.origin + ray.direction * ray.tmin
    float step_size = 0.5;
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

        if (d < MIN_DENSITY)
		{
            ray.tmin += step_size * 2;
            continue;
		}

        d *= _DensityScale;
        acc_density += d;

        // read density towards sun, and calc light transmittance
        float light_density       = light_step(pos, volume);
        float light_transmittance = Beers_Law(light_density);
        light_energy += d * transmittance * light_transmittance * step_size;
        transmittance *= exp(-d * step_size);

        if (transmittance < MIN_TRANSMITTANCE)
        {
            transmittance = 0;
            break;
        }

        step++;
        ray.tmin += step_size;
    }

    if (transmittance > 0)
    {
		// return COLOR_RED;
	}

    acc_density = min(acc_density, 1.0) * (1 - transmittance);
    float3 final_color = saturate(CLOUD_COLOR * transmittance + light_energy) * acc_density;

    return float4(final_color, acc_density);
}

#endif // NANO_VOLUME_PASS
