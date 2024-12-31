#ifndef TEMPORAL_PASS
#define TEMPORAL_PASS

TEXTURE2D_X(_FrameHistory);
TEXTURE2D_X(_NextFrame);

float4 TemporalPass(float2 uv, float2 uv_prev)
{    
    float4 history = SAMPLE_TEXTURE2D_X(_FrameHistory, s_linear_clamp_sampler, uv_prev);
    float4 nextFrame = SAMPLE_TEXTURE2D_X(_NextFrame, s_linear_clamp_sampler, uv);

    // _TotalFrames is specificed in NanoVolumePass.hlsl
    if (_TotalFrames == 1)
    {
        return nextFrame;
    }

    float alpha = 1.0 / float(_TotalFrames);

    // Combine new frame with previous ones
    float4 blendedFrame = alpha * nextFrame + (1 - alpha) * history;

    return blendedFrame;
}

#endif // TEMPORAL_PASS