using TMPro;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

[RequireComponent(typeof(CustomPassVolume))]
public class FullscreenNanoVolumePass : MonoBehaviour
{
    private CustomPassVolume customPass;
    private FullScreenCustomPass nanoVDBPass;
    private NanoVolumeLoader nanoVolumeLoader;

    private Material mat;

    public Light directionalLight;

    public TMP_Text vdbNameText;
    public Slider RaymarchSamples;
    public Slider DensitySlider;
    public Slider LightRayLength;
    public Slider LightSteps;
    public Slider LightAbsorbation;

    private void Start()
    {
        customPass = GetComponent<CustomPassVolume>();

        if (customPass.customPasses.Count == 0)
        {
            Debug.LogError("No custom passes found in the Nano Volume Pass");
            return;
        }

        nanoVDBPass = (FullScreenCustomPass)customPass.customPasses[0];
        mat = nanoVDBPass.fullscreenPassMaterial;

        nanoVolumeLoader = GetComponent<NanoVolumeLoader>();
        vdbNameText.text = nanoVolumeLoader.volumePath;
    }

    private void Update()
    {
        mat.SetBuffer("buf", nanoVolumeLoader.GetGPUBuffer());
        mat.SetFloat("_ClipPlaneMin", 1f);
        mat.SetFloat("_ClipPlaneMax", 2000.0f);

        mat.SetVector("_LightDir", directionalLight.transform.forward);

        mat.SetFloat("_DensityScale", DensitySlider.value);
        mat.SetFloat("_LightAbsorbation", LightAbsorbation.value);
        mat.SetFloat("_LightRayLength", LightRayLength.value);

        mat.SetInt("_RayMarchSamples", (int)RaymarchSamples.value);
        mat.SetInt("_LightSamples", (int)LightSteps.value);
    }
}
