using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public TMP_Text fpsText;
    public TMP_Text avgFpsText;

    private float avgFrameTime = 0.0f;
    private int lastNumberOfFrames = 60;

    private void Awake()
    {
        // set max fps
        Application.targetFrameRate = 300;
    }

    void Update()
    {
        // update FPS once half a second
        if (Time.frameCount % 30 == 0)
        {
            fpsText.text = "FPS: " + (1.0f / Time.deltaTime).ToString("F0") + "\t ms: " + (Time.deltaTime * 1000).ToString("F2");
        }

        
        avgFrameTime += Time.deltaTime;
        if (Time.frameCount % lastNumberOfFrames == 0)
        {
            avgFrameTime /= lastNumberOfFrames;
            avgFpsText.text = "AVG 60 frames: " + (1.0f / avgFrameTime).ToString("F0");
            avgFrameTime = 0.0f;
        }
    }
}
