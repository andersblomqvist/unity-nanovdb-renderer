using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class TemporalNanoVolumePass : CustomPass
{
    const int NANO_VOLUME_PASS_ID    = 0;
    const int TEMPORAL_BLEND_PASS_ID = 1;

    public NanoVolumeLoader     nanoVolumeLoaderComponent;
    public NanoVolumeSettings   nanoVolumeSettings;

    [Range(1, 16)]
    public int temporalFrames = 4;

    Material mat;
    RTHandle nextFrame;
    RTHandle frameHistory;
    RTHandle blendedFrame;
    int      frameIndex;

    // To make sure the shader ends up in the build, we keep a reference to it
    [SerializeField, HideInInspector]
    Shader volumeShader;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        frameIndex = 0;

        volumeShader = Shader.Find("FullScreen/TemporalNanoVolumePass");
        mat = CoreUtils.CreateEngineMaterial(volumeShader);

        nextFrame = RTHandles.Alloc(
            Vector2.one, TextureXR.slices, 
            colorFormat: GraphicsFormat.R16G16B16A16_SFloat, 
            dimension: TextureXR.dimension, 
            useDynamicScale: true, 
            name: "Next Frame Buffer"
        );

        frameHistory = RTHandles.Alloc(
            Vector2.one, TextureXR.slices, 
            colorFormat: GraphicsFormat.R16G16B16A16_SFloat, 
            dimension: TextureXR.dimension, 
            useDynamicScale: true, 
            name: "Frame History Buffer"
        );

        blendedFrame = RTHandles.Alloc(
            Vector2.one, TextureXR.slices, 
            colorFormat: GraphicsFormat.R16G16B16A16_SFloat, 
            dimension: TextureXR.dimension, 
            useDynamicScale: true, 
            name: "Blended Frame Buffer"
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

        // Draw newst frame to buffer
        CoreUtils.SetRenderTarget(ctx.cmd, nextFrame, ClearFlag.Color);
        CoreUtils.DrawFullScreen(ctx.cmd, mat, ctx.propertyBlock, shaderPassId: NANO_VOLUME_PASS_ID);

        // Insert textures for temporal blend pass
        ctx.cmd.SetGlobalTexture("_NextFrame", nextFrame);
        ctx.cmd.SetGlobalTexture("_FrameHistory", frameHistory);

        // Combine new frame with old frames
        CoreUtils.SetRenderTarget(ctx.cmd, blendedFrame, ClearFlag.Color);
        CoreUtils.DrawFullScreen(ctx.cmd, mat, ctx.propertyBlock, shaderPassId: TEMPORAL_BLEND_PASS_ID);

        // Save blended frame to history
        ctx.cmd.Blit(blendedFrame, frameHistory, new Vector2(scale.x, scale.y), Vector2.zero, 0, 0);

        // Display blended frame to camera
        ctx.cmd.Blit(blendedFrame, ctx.cameraColorBuffer, new Vector2(scale.x, scale.y), Vector2.zero, 0, 0);

        frameIndex = (frameIndex + 1) % temporalFrames;
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

        mat.SetFloat("_DensityScale", nanoVolumeSettings.DensitySlider.value);
        mat.SetFloat("_LightAbsorbation", nanoVolumeSettings.LightAbsorbation.value);
        mat.SetFloat("_LightRayLength", nanoVolumeSettings.LightRayLength.value);

        mat.SetInt("_RayMarchSamples", (int)nanoVolumeSettings.RaymarchSamples.value);
        mat.SetInt("_LightSamples", (int)nanoVolumeSettings.LightSteps.value);

        mat.SetInt("_VisualizeSteps", nanoVolumeSettings.visualizeSteps);
    }
}