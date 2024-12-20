#ifndef TEMPORAL_PASS
#define TEMPORAL_PASS

TEXTURE2D_X(_FrameHistory);
TEXTURE2D_X(_NextFrame);

uniform float _Alpha;

float4 SampleTextureBilinear(TEXTURE2D_X(tex), float2 uv)
{
    return SAMPLE_TEXTURE2D_X(tex, s_linear_clamp_sampler, uv);
}

float4 TemporalPass(float2 uv, float2 uv_prev)
{    
    float4 history = SAMPLE_TEXTURE2D_X(_FrameHistory, s_linear_clamp_sampler, uv_prev);
    float4 nextFrame = SAMPLE_TEXTURE2D_X(_NextFrame, s_linear_clamp_sampler, uv);

    float a = 0.05;

    // Combine frame N-1 with N
    float4 blendedFrame = nextFrame + history;

    return blendedFrame;
}

#endif // TEMPORAL_PASS