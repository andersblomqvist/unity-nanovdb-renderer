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

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "Nano Volume Pass"

            ZWrite Off
            Cull Off
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
                #pragma fragment FullScreenPass
            ENDHLSL
        }
    }
}
