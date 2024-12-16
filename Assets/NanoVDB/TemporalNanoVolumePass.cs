using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class TemporalNanoVolumePass : CustomPass
{
    public NanoVolumeLoader nanoVolumeLoaderComponent;
    public NanoVolumeSettings nanoVolumeSettings;

    [SerializeField, HideInInspector]
    Shader volumeShader;

    Material mat;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        volumeShader = Shader.Find("FullScreen/TemporalNanoVolumePass");
        mat = CoreUtils.CreateEngineMaterial(volumeShader);
    }

    protected override void Execute(CustomPassContext ctx)
    {
        if (!nanoVolumeLoaderComponent.IsLoaded())
        {
            return;
        }

        SetUniforms();

        CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);
        CoreUtils.DrawFullScreen(ctx.cmd, mat, ctx.propertyBlock, shaderPassId: 0);
    }

    protected override void Cleanup()
    {
        CoreUtils.Destroy(mat);
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