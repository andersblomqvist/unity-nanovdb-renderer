using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NanoVolumeSettings : MonoBehaviour
{
    public Light directionalLight;
    public TMP_Text vdbNameText;
    public Slider RaymarchSamples;
    public Slider DensitySlider;
    public Slider LightRayLength;
    public Slider LightSteps;
    public Slider LightAbsorbation;
    public int visualizeSteps = 0;

    // Called from UI Button
    public void VisualizeSteps()
    {
        if (visualizeSteps == 0)
        {
            visualizeSteps = 1;
        }
        else
        {
            visualizeSteps = 0;
        }
    }

    // When clicking reset button
    public void StopVisualizeSteps()
    {
        visualizeSteps = 0;
    }
}
