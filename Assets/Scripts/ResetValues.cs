using UnityEngine;
using UnityEngine.UI;

public class ResetValues : MonoBehaviour
{
    public Slider samples;
    public Slider densityScale;
    public Slider LightRayLength;
    public Slider LightSteps;
    public Slider LightAbsorbation;

    private float samplesValue;
    private float densityScaleValue;
    private float lightRayLengthValue;
    private float lightStepsValue;
    private float lightAbsorbationValue;

    private void Start()
    {
        samplesValue = samples.value;
        densityScaleValue = densityScale.value;
        lightRayLengthValue = LightRayLength.value;
        lightStepsValue = LightSteps.value;
        lightAbsorbationValue = LightAbsorbation.value;
    }

    public void OnClick()
    {
        samples.value = samplesValue;
        densityScale.value = densityScaleValue;
        LightRayLength.value = lightRayLengthValue;
        LightSteps.value = lightStepsValue;
        LightAbsorbation.value = lightAbsorbationValue;
    }
}
