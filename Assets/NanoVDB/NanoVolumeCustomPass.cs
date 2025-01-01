using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

class NanoVolumeCustomPass : CustomPass
{
    const int NANO_VOLUME_PASS_ID = 0;

    public NanoVolumeLoader     nanoVolumeLoaderComponent;
    public NanoVolumeSettings   nanoVolumeSettings;

    Material mat;

    // To make sure the shader ends up in the build, we keep a reference to it
    [SerializeField, HideInInspector]
    Shader volumeShader;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        volumeShader = Shader.Find("FullScreen/NanoVolumePass");
        mat = CoreUtils.CreateEngineMaterial(volumeShader);
    }

    protected override void Execute(CustomPassContext ctx)
    {
        if (!nanoVolumeLoaderComponent.IsLoaded())
        {
            return;
        }

        SetUniforms();

        CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.Color);
        CoreUtils.DrawFullScreen(ctx.cmd, mat, ctx.propertyBlock, shaderPassId: NANO_VOLUME_PASS_ID);
    }

    protected override void Cleanup()
    {
        CoreUtils.Destroy(mat);
    }

    void SetUniforms()
    {
        mat.SetBuffer("buf", nanoVolumeLoaderComponent.GetGPUBuffer());
        mat.SetFloat("_ClipPlaneMin", 0.01f);
        mat.SetFloat("_ClipPlaneMax", 1500.0f);

        mat.SetVector("_LightDir", nanoVolumeSettings.directionalLight.transform.forward);

        mat.SetVector("_Light", nanoVolumeSettings.directionalLight.color);
        mat.SetVector("_Scattering", nanoVolumeSettings.scatteringColor);

        mat.SetFloat("_DensityScale", nanoVolumeSettings.DensitySlider.value);
        mat.SetFloat("_LightAbsorbation", nanoVolumeSettings.LightAbsorbation.value);

        mat.SetInt("_RayMarchSamples", (int)nanoVolumeSettings.RaymarchSamples.value);
        mat.SetInt("_LightSamples", (int)nanoVolumeSettings.LightSteps.value);

        mat.SetInt("_VisualizeSteps", nanoVolumeSettings.visualizeSteps);
    }
}