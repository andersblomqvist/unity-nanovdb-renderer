using System.Text;
using UnityEditor;
using UnityEngine;

public class TestPerformance : MonoBehaviour
{
    float msFrameTime;
    float avgFrameTime;
    float lastAvgFrameTime;

    int numberOfFrames;
    int numberOfTests;
    int testCounter;

    bool testing;

    StringBuilder fileOutput;

    public string fileName;

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

                fileOutput.AppendLine("Avg: " + avgFrameTime);
                SaveTextFile(fileOutput.ToString(), fileName);

                // Stop testing and reset all
                testCounter = 0;
                avgFrameTime = 0.0f;
                testing = false;
                fileOutput.Clear();

                Debug.Log("Performance test: " + fileName + " ended");
            }
        }
    }

    public void StartTest()
    {
        // start test
        if (fileName.Length == 0)
        {
            Debug.LogError("Filename is empty. Please name the test!");
            return;
        }
        fileOutput.AppendLine("Performance test: " + fileName);
        fileOutput.AppendLine("Avg render time in MS over: " + numberOfFrames + " frames");

        Debug.Log("Performance test: " + fileName + " started");
        testing = true;
    }

    private void SaveTextFile(string text, string fileName)
    {
        string date = System.DateTime.UtcNow.ToString("HH-mm-dd");
        string path = "Assets/TestOutput/" + fileName + "-" + date + ".txt";
        System.IO.File.WriteAllText(path, text);
        AssetDatabase.ImportAsset(path);

    }
}
