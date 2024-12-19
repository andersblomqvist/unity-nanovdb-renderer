using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class TemporalNanoVolumePass : CustomPass
{
    const int NANO_VOLUME_PASS_ID    = 0;
    const int TEMPORAL_BLEND_PASS_ID = 1;
    const int COPY_HISTORY_PASS_ID   = 2;

    public NanoVolumeLoader     nanoVolumeLoaderComponent;
    public NanoVolumeSettings   nanoVolumeSettings;

    [Range(1, 16)]
    public int temporalFrames = 4;

    Material mat;
    RTHandle nextFrame;
    RTHandle frameHistory;
    RTHandle blendedFrame;
    int      N;

    // To make sure the shader ends up in the build, we keep a reference to it
    [SerializeField, HideInInspector]
    Shader volumeShader;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        N = 0;

        volumeShader = Shader.Find("FullScreen/TemporalNanoVolumePass");
        mat = CoreUtils.CreateEngineMaterial(volumeShader);

        nextFrame = RTHandles.Alloc(
            Vector2.one, TextureXR.slices,
            colorFormat: GraphicsFormat.R16G16B16A16_SFloat, 
            dimension: TextureXR.dimension,
            name: "Next_Frame_Buffer"
        );

        frameHistory = RTHandles.Alloc(
            Vector2.one, TextureXR.slices,
            colorFormat: GraphicsFormat.R16G16B16A16_SFloat, 
            dimension: TextureXR.dimension,
            name: "Frame_History_Buffer"
        );

        blendedFrame = RTHandles.Alloc(
            Vector2.one, TextureXR.slices, 
            colorFormat: GraphicsFormat.R16G16B16A16_SFloat, 
            dimension: TextureXR.dimension, 
            name: "Blended_Frame_Buffer"
        );
    }

    protected override void Execute(CustomPassContext ctx)
    {
        if (!nanoVolumeLoaderComponent.IsLoaded())
        {
            return;
        }

        Vector4 scale = RTHandles.rtHandleProperties.rtHandleScale;

        SetUniforms();

        // Draw newest frame to a buffer
        CoreUtils.SetRenderTarget(ctx.cmd, nextFrame, ClearFlag.Color);
        CoreUtils.DrawFullScreen(ctx.cmd, mat, ctx.propertyBlock, shaderPassId: NANO_VOLUME_PASS_ID);

        // Combine new frame with old frames into another buffer
        ctx.propertyBlock.SetTexture("_NextFrame", nextFrame);
        ctx.propertyBlock.SetTexture("_FrameHistory", frameHistory);
        CoreUtils.SetRenderTarget(ctx.cmd, blendedFrame, ClearFlag.Color);
        CoreUtils.DrawFullScreen(ctx.cmd, mat, ctx.propertyBlock, shaderPassId: TEMPORAL_BLEND_PASS_ID);

        // Save blended frame to history at slice frameIndex
        ctx.propertyBlock.SetTexture("_BlendedFrame", blendedFrame);
        CoreUtils.SetRenderTarget(ctx.cmd, frameHistory, ClearFlag.Color);
        CoreUtils.DrawFullScreen(ctx.cmd, mat, ctx.propertyBlock, shaderPassId: COPY_HISTORY_PASS_ID);
        
        // Display blended frame to camera
        ctx.cmd.Blit(blendedFrame, ctx.cameraColorBuffer, new Vector2(scale.x, scale.y), Vector2.zero, 0, 0);

        N = (N + 1) % temporalFrames;
    }

    protected override void Cleanup()
    {
        CoreUtils.Destroy(mat);
        nextFrame.Release();
        frameHistory.Release();
        blendedFrame.Release();
    }

    void SetUniforms()
    {
        mat.SetBuffer("buf", nanoVolumeLoaderComponent.GetGPUBuffer());
        mat.SetFloat("_ClipPlaneMin", 1f);
        mat.SetFloat("_ClipPlaneMax", 2000.0f);

        mat.SetVector("_LightDir", nanoVolumeSettings.directionalLight.transform.forward);

        mat.SetVector("_Light", nanoVolumeSettings.directionalLight.color);
        mat.SetVector("_Scattering", nanoVolumeSettings.scatteringColor);

        mat.SetFloat("_DensityScale", nanoVolumeSettings.DensitySlider.value);
        mat.SetFloat("_LightAbsorbation", nanoVolumeSettings.LightAbsorbation.value);
        mat.SetFloat("_LightRayLength", nanoVolumeSettings.LightRayLength.value);

        mat.SetInt("_RayMarchSamples", (int)nanoVolumeSettings.RaymarchSamples.value);
        mat.SetInt("_LightSamples", (int)nanoVolumeSettings.LightSteps.value);

        mat.SetInt("_VisualizeSteps", nanoVolumeSettings.visualizeSteps);

        mat.SetFloat("_Alpha", 0.05f);

        // Generate random between 0 and 1
        float r = Random.value;
        mat.SetFloat("_Offset", r);
    }
}