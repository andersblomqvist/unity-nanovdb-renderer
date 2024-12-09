using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UpdateSliderValue : MonoBehaviour
{
    public TMP_Text sliderTextValue;

    private Slider slider;

    public bool roundFloat = false;

    void Start()
    {
        slider = GetComponent<Slider>();
    }

    void Update()
    {
        if (roundFloat)
            sliderTextValue.text = slider.value.ToString("F2");
        else
            sliderTextValue.text = slider.value.ToString("F0");
    }
}
