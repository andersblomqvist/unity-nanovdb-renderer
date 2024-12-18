Shader "FullScreen/TemporalNanoVolumePass"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 5.0
    #pragma use_dxc

    // For DecodeMotionVector   
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"

    // Commons
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

    TEXTURE2D_X(_FrameHistory);
    TEXTURE2D_X(_NextFrame);

    uniform float _Alpha;

    float4 TemporalBlendPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);

        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

        float2 scaling = _RTHandleScale.xy;
        float2 uv = posInput.positionNDC.xy * scaling;

        // Decode the velocity of the pixel
        float2 velocity = float2(0.0, 0.0);
        DecodeMotionVector(LOAD_TEXTURE2D_X(_CameraMotionVectorsTexture, uv), velocity);

        // Calculate previous frame UV
        float2 uv_prev = (posInput.positionNDC  - velocity) * scaling;

        // Sample previous frame
        float4 prevFrame = SAMPLE_TEXTURE2D_X(_FrameHistory, s_linear_clamp_sampler, uv_prev);

        // Sample current frame
        float4 nextFrame = SAMPLE_TEXTURE2D_X(_NextFrame, s_linear_clamp_sampler, uv);

        // Blend current frame with previous frame using alpha
        float4 blendedFrame = nextFrame * _Alpha + prevFrame * (1.0 - _Alpha);

        return blendedFrame;
    }

    TEXTURE2D_X(_BlendedFrame);

    float4 CopyHistoryPass(Varyings varyings) : SV_Target
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
            Name "Copy History Pass"
            ZWrite Off Cull Off Blend One OneMinusSrcAlpha

            HLSLPROGRAM
                #pragma fragment CopyHistoryPass
            ENDHLSL   
        }
    }
}
