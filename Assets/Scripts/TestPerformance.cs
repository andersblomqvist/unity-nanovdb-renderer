#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

public class TestPerformance : MonoBehaviour
{
    public string groundTruthPath;

    public bool isGroundTruth;
    public string testName;

    float msFrameTime;
    float avgFrameTime;
    float lastAvgFrameTime;

    int numberOfFrames;
    int numberOfTests;
    int testCounter;

    bool testing;

    StringBuilder fileOutput;
    ImageProcessor imageProcessor;

    private void Start()
    {
        msFrameTime      = 0.0f;
        avgFrameTime     = 0.0f;
        lastAvgFrameTime = 0.0f;

        numberOfFrames = 60;
        numberOfTests  = 10;
        testCounter    = 0;

        testing = false;

        fileOutput = new StringBuilder();
        imageProcessor = GetComponent<ImageProcessor>();
    }

    private void Update()
    {
        msFrameTime += Time.deltaTime * 1000;
        if (Time.frameCount % numberOfFrames == 0)
        {
            msFrameTime /= numberOfFrames;
            lastAvgFrameTime = msFrameTime;
            msFrameTime = 0.0f;

            if (!testing) { return; }

            avgFrameTime += lastAvgFrameTime;
            fileOutput.AppendLine(lastAvgFrameTime.ToString("F4"));

            testCounter++;

            if (testCounter == numberOfTests)
            {
                avgFrameTime /= numberOfTests;

                fileOutput.AppendLine("Total avgerage of " + avgFrameTime + " ms");

                imageProcessor.SaveFrame(testName);

                if (!isGroundTruth)
                {
                    Texture2D groundTruth = imageProcessor.LoadImage(groundTruthPath + ".png");
                    Texture2D testImage = imageProcessor.LoadImage("Assets/TestOutput/" + testName + ".png");

                    float rmse = imageProcessor.ComputeRMSE(groundTruth, testImage);
                    fileOutput.AppendLine("RMSE: " + rmse);
                    Debug.Log("RMSE: " + rmse);
                }

                SaveTextFile(fileOutput.ToString(), testName);

                // Stop testing and reset all
                testCounter = 0;
                avgFrameTime = 0.0f;
                testing = false;
                fileOutput.Clear();
                Debug.Log("Performance test: " + testName + " ended");
            }
        }
    }

    public void StartTest()
    {
        // start test
        if (testName.Length == 0)
        {
            Debug.LogError("Filename is empty. Please name the test!");
            return;
        }

        fileOutput.AppendLine(testName);
        fileOutput.AppendLine("Render time in MS");

        Debug.Log("Performance test: " + testName + " started");
        testing = true;
    }

    private void SaveTextFile(string text, string fileName)
    {
        string path = "Assets/TestOutput/" + fileName + ".txt";
        System.IO.File.WriteAllText(path, text);
        AssetDatabase.ImportAsset(path);

    }
}
#endif