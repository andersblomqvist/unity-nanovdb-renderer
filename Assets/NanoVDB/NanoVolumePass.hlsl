#ifndef NANO_VOLUME_PASS
#define NANO_VOLUME_PASS

#define MIN_TRANSMITTANCE   0.05
#define MIN_DENSITY         0.001
#define CLOUD_COLOR         float3(1, 1, 1)

#define COLOR_NONE          float4(0, 0, 0, 0)
#define COLOR_RED           float4(1, 0, 0, 1)
#define COLOR_GREEN         float4(0, 1, 0, 1)
#define COLOR_BLUE          float4(0, 0, 1, 1)

#define PNANOVDB_HLSL
#include "PNanoVDB.hlsl"

uniform pnanovdb_buf_t buf : register(t1);

uniform float4	_LightDir;  // directionalLight.transform.forward

uniform float3	_Light;
uniform float3	_Scattering;

uniform float	_DensityScale;
uniform float	_LightRayLength;
uniform float	_LightAbsorbation;
uniform float	_ClipPlaneMin;
uniform float	_ClipPlaneMax;

uniform int		_RayMarchSamples;
uniform int		_LightSamples;

uniform int		_VisualizeSteps;

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

bool get_hdda_hit(inout pnanovdb_readaccessor_t acc, inout Ray ray, inout float valueAtHit)
{
	float thit;
	bool hit = pnanovdb_hdda_tree_marcher(
		PNANOVDB_GRID_TYPE_FLOAT,
		buf,
		acc,
		ray.origin, ray.tmin,
		ray.direction, ray.tmax,
		thit,
		valueAtHit
	);
	ray.tmin = thit;
	return hit;
}

void get_participating_media(out float sigmaS, out float sigmaE, float3 pos, inout pnanovdb_readaccessor_t acc)
{
	sigmaS = get_value_coord(acc, pos) * _DensityScale;
	sigmaE = max(0.000001, sigmaS);
}

// No inout to avoid breaking cache for main ray (Gaida 2022)
float volumetric_shadow(float3 pos, pnanovdb_readaccessor_t acc)
{
	if (_LightSamples < 1) { return 0; }

	float light_dir = -(_LightDir.xyz);

	float shadow = 1;
	float sigmaS = 0.0;
	float sigmaE = 0.0;

	int step = 0;
	int steps = 10;
	float step_size = 1;
	while (step < steps)
	{
		float3 sample_pos = pos + step_size * light_dir;

		get_participating_media(sigmaS, sigmaE, sample_pos, acc);
		shadow *= exp(-sigmaE * step_size);

		if (shadow < MIN_TRANSMITTANCE)
		{
			shadow = 0;
			break;
		}

		step_size *= 2;
		step++;
	}
	return shadow;
}

// Step equal over light ray
float volumetric_shadow_2(float3 pos, pnanovdb_readaccessor_t acc)
{
	if (_LightSamples < 1) { return 0; }

	float light_dir = -(_LightDir.xyz);

	float shadow = 1;
	float sigmaS = 0.0;
	float sigmaE = 0.0;

	float steps = 64;
    float light_ray_length = 650;

	float step_size = light_ray_length / steps;
	for (float t = step_size; t < light_ray_length; t += step_size)
    {
        float3 sample_pos = pos + t * light_dir;

        get_participating_media(sigmaS, sigmaE, sample_pos, acc);
        shadow *= exp(-sigmaE * step_size);

        if (shadow < MIN_TRANSMITTANCE)
        {
            shadow = 0;
            break;
        }
    }
	return shadow;
}

float phase_function()
{
	return 1.0;
}

float4 raymarch_volume(Ray ray, inout NanoVolume volume, float step_size)
{
	float transmittance  = 1.0;
	float sigmaS         = 0.0;
	float sigmaE         = 0.0;
	float acc_density    = 0.0;
	float3 direct_light  = 0.0;
	float3 ambient_light = 0.01;

	float not_used;
	bool hit = get_hdda_hit(volume.acc, ray, not_used);
	if (!hit) { return COLOR_NONE; }

	int step = 0;
	float skip = 0;
	while (step < _RayMarchSamples)
	{
		if (ray.tmin >= ray.tmax)
		{
			break;
		}

		// read density from ray position
		float3 pos = ray.origin + ray.direction * ray.tmin;
		get_participating_media(sigmaS, sigmaE, pos, volume.acc);

		// Skip empty space.
		uint dim = get_dim_coord(volume.acc, pos);
		if (dim > 1)
		{
			step++;
			ray.tmin += 5;
			skip = 5;
			continue;
		}
		if (sigmaS < MIN_DENSITY)
		{
			step++;
			ray.tmin += 2;
			skip = 2;
			continue;
		}

		if (skip > 0) {
			// backtrack a little bit
			ray.tmin -= skip / 2;
			pos = ray.origin + ray.direction * ray.tmin;
			skip = 0;
		}

		acc_density += sigmaS;

		float3 S = sigmaS * phase_function() * volumetric_shadow(pos, volume.acc);
		float3 Sint = (S - S * exp(-sigmaE * step_size)) / sigmaE;
		direct_light += transmittance * Sint;

		transmittance *= exp(-sigmaE * step_size);

		if (acc_density > 1.0)
		{
			break;
		}

		// Early out if no more light is reaching this point
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

	float3 final_color = (direct_light + ambient_light) * acc_density;
	final_color = pow(final_color, 1.0 / 2.2);
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

	float step_size = 0.57;
	float4 final_color = raymarch_volume(ray, volume, step_size);
	return final_color;
}

#endif // NANO_VOLUME_PASS
