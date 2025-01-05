#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// This code was written by ChatGPT with the prompt:
/// 
///    "In Unity3D, I need a script that saves the rendered frame to a loss less 
///     image file format. Additionally, this script should also have a public 
///     method which compares two images according to the image quality metric 
///     RMSE. Can you do that?"
///  
/// </summary>
public class ImageProcessor : MonoBehaviour
{
    // The directory to save images
    const string saveDirectory = "Assets/TestOutput/";

    // Method to save the current frame as a PNG image
    public void SaveFrame(string fileName)
    {
        // Create a RenderTexture and read the current frame
        RenderTexture renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        Texture2D texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

        Camera.main.targetTexture = renderTexture;
        Camera.main.Render();

        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        texture.Apply();

        Camera.main.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);

        // Save the image as PNG
        byte[] imageBytes = texture.EncodeToPNG();
        string filePath = Path.Combine(saveDirectory, fileName + ".png");
        File.WriteAllBytes(filePath, imageBytes);
        AssetDatabase.ImportAsset(filePath);
        AssetDatabase.Refresh();

        Debug.Log($"Frame saved to: {filePath}");

        // Clean up
        Destroy(texture);
    }

    // Method to compute the RMSE between two images
    public float ComputeRMSE(Texture2D image1, Texture2D image2)
    {
        // Check that the dimensions match
        if (image1.width != image2.width || image1.height != image2.height)
        {
            Debug.LogError("Image dimensions do not match.");
            return float.MaxValue;
        }

        int width = image1.width;
        int height = image1.height;

        // Get pixel data
        Color[] pixels1 = image1.GetPixels();
        Color[] pixels2 = image2.GetPixels();

        // Compute RMSE
        float errorSum = 0f;
        for (int i = 0; i < pixels1.Length; i++)
        {
            float rDiff = pixels1[i].r - pixels2[i].r;
            float gDiff = pixels1[i].g - pixels2[i].g;
            float bDiff = pixels1[i].b - pixels2[i].b;

            float diffSquared = rDiff * rDiff + gDiff * gDiff + bDiff * bDiff;
            errorSum += diffSquared;
        }

        float mse = errorSum / (width * height);
        float rmse = Mathf.Sqrt(mse);
        return rmse;
    }

    // Helper method to load a Texture2D from a file
    public Texture2D LoadImage(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"File not found: {filePath}");
            return null;
        }

        byte[] fileData = File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2); // Size doesn't matter, will be overwritten
        texture.LoadImage(fileData);

        return texture;
    }
}
#endif