// https://www.reedbeta.com/blog/quick-and-easy-gpu-random-numbers-in-d3d11/

uint rand_xorshift(uint seed)
{
    // Xorshift algorithm from George Marsaglia's paper
    seed ^= (seed << 13);
    seed ^= (seed >> 17);
    seed ^= (seed << 5);
    return seed;
}

float random_float(float3 view_dir)
{
    uint seed = asuint(view_dir.x + view_dir.y + view_dir.z);
    float res = float(rand_xorshift(seed)) * (1.0 / 4294967296.0);
    res = float(rand_xorshift(asuint(res))) * (1.0 / 4294967296.0);
    res = float(rand_xorshift(asuint(res))) * (1.0 / 4294967296.0);
    return res;
}