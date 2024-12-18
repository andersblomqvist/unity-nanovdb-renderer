Shader "FullScreen/TemporalNanoVolumePass"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 5.0
    #pragma use_dxc

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
    #include "Assets/NanoVDB/NanoVolumePass.hlsl"

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        float3 viewDirection = GetWorldSpaceNormalizeViewDir(posInput.positionWS);

        float4 color = NanoVolumePass(_WorldSpaceCameraPos, -viewDirection); 
        return float4(color.rgb, color.a);
    }

    TEXTURE2D_ARRAY(_FrameHistory);
    TEXTURE2D_X(_NextFrame);
    uniform int _TemporalFrames;
    uniform int _FrameIndex;

    float4 TemporalBlendPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

        float2 scaling = _RTHandleScale.xy;
        float2 uv = posInput.positionNDC.xy * scaling;
        float4 nextFrame = SAMPLE_TEXTURE2D_X(_NextFrame, s_linear_clamp_sampler, uv);
        float4 lastFrame = SAMPLE_TEXTURE2D_ARRAY(_FrameHistory, s_linear_clamp_sampler, uv, (_FrameIndex - 1) % _TemporalFrames);

        // blend next and lastFrame
        float4 blendedFrame = lerp(lastFrame, nextFrame, 0.5);

        return blendedFrame;
    }

    TEXTURE2D_X(_BlendedFrame);
    float4 CopyColorPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

        float2 scaling = _RTHandleScale.xy;
        float2 uv = posInput.positionNDC.xy * scaling;
        float4 color = SAMPLE_TEXTURE2D_X(_BlendedFrame, s_linear_clamp_sampler, uv);

        return color;
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "Nano Volume Pass"
            ZWrite Off Cull Off Blend One OneMinusSrcAlpha

            HLSLPROGRAM
                #pragma fragment FullScreenPass
            ENDHLSL
        }

        Pass
        {
            Name "Temporal Blend Pass"
            ZWrite Off Cull Off Blend One OneMinusSrcAlpha

            HLSLPROGRAM
                #pragma fragment TemporalBlendPass
            ENDHLSL
        }

        Pass
        {
            Name "Copy Color Pass"
            ZWrite Off Cull Off Blend One OneMinusSrcAlpha

            HLSLPROGRAM
                #pragma fragment CopyColorPass
            ENDHLSL   
        }
    }
}
