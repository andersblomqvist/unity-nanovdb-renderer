Shader "FullScreen/TemporalNanoVolumePass"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 5.0
    #pragma use_dxc

    // Commons, includes many others
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    #include "Assets/NanoVDB/NanoVolumePass.hlsl"
    #include "Assets/NanoVDB/TemporalPass.hlsl"

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        float3 viewDirection = GetWorldSpaceNormalizeViewDir(posInput.positionWS);

        float4 color = NanoVolumePass(_WorldSpaceCameraPos, -viewDirection); 
        return float4(color.rgb, color.a);
    }

    float4 TemporalBlendPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);

        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        float2 scaling = _RTHandleScale.xy;
        float2 uv = posInput.positionNDC.xy * scaling;

        // Current pixel world position
        float4 p_wp = mul(uv, UNITY_MATRIX_I_VP);

        // Previous pixel uv position
        float2 uv_prev = mul(p_wp, UNITY_MATRIX_PREV_VP).xy * scaling;

        float4 blendedFrame = TemporalPass(uv, uv_prev);
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
