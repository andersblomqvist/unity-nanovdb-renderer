using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[RequireComponent(typeof(CustomPassVolume))]
public class FullscreenNanoVolumePass : MonoBehaviour
{
    private CustomPassVolume customPass;
    private FullScreenCustomPass nanoVDBPass;

    private NanoVolumeLoader nanoVolumeLoader;
    private NanoVolumeSettings nanoVolumeSettings;

    private Material mat;

    private bool loaded = true;

    private void Start()
    {
        customPass = GetComponent<CustomPassVolume>();

        if (customPass.customPasses.Count == 0)
        {
            Debug.LogError("No custom passes found in the Nano Volume Pass");
            return;
        }

        nanoVDBPass = (FullScreenCustomPass)customPass.customPasses[0];

        if (nanoVDBPass.enabled == false)
        {
            loaded = false;
            return;
        }

        mat = nanoVDBPass.fullscreenPassMaterial;

        nanoVolumeLoader = GetComponent<NanoVolumeLoader>();
        nanoVolumeSettings = GetComponent<NanoVolumeSettings>();
        nanoVolumeSettings.vdbNameText.text = nanoVolumeLoader.volumePath;
    }

    private void Update()
    {
        if (!loaded)
        {
            return;
        }

        mat.SetBuffer("buf", nanoVolumeLoader.GetGPUBuffer());
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
    }
}
