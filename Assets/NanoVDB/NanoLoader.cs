using System;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NanoLoader : MonoBehaviour
{
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct NanoVolume
    {
        public uint* buf;
        public ulong byteSize;
        public ulong elementCount;
        public ulong structStride;
    }; unsafe NanoVolume* nanoVolume;

    public Shader nanoVDBShader;
    public string volumePath;

    ComputeBuffer nvdb;
    uint[] buf;

    GameObject renderOn;
    MeshRenderer mesh;
    Material material;

    float DensityScale = 0.5f;
    float ClipPlaneMin = 0.1f;
    float ClipPlaneMax = 2000f;
    int RayMarchSamples = 4;

    [Header("Other")]

    public TMP_Text vdbNameText;
    public Slider samplesSlider;
    public Slider densitySlider;
    public Slider LightRayLength;
    public Slider LightSteps;
    public Slider LightAbsorbation;
    public Light directionalLight;

    void Start()
    {
        SetDebugLogCallback(DebugLogCallback);

        PrepareRenderOnObject();

        vdbNameText.text = volumePath;

        bool ok = PrepareVolume();
        if (!ok) return;

        unsafe
        {
            int bufferSize = (int)nanoVolume->elementCount;
            int stride = (int)nanoVolume->structStride;

            Debug.Log("Creating buffer with length: " + bufferSize);
            buf = new uint[bufferSize];

            // Go through each element in nanoVolume buf and copy it to the buf array
            for (int i = 0; i < bufferSize; i++)
            {
                buf[i] = nanoVolume->buf[i];
            }

            nvdb = new ComputeBuffer(
                bufferSize,
                stride,
                ComputeBufferType.Default
            );
            nvdb.SetData(buf);
            material.SetBuffer("buf", nvdb);
        }
    }

    private void Update()
    {
        RayMarchSamples = (int)samplesSlider.value;
        DensityScale = densitySlider.value;

        // get the main light direction
        Vector4 lightDir = directionalLight.transform.forward;
        material.SetVector("_LightDir", lightDir);

        material.SetFloat("_DensityScale", DensityScale);
        material.SetFloat("_ClipPlaneMin", ClipPlaneMin);
        material.SetFloat("_ClipPlaneMax", ClipPlaneMax);
        material.SetInt("_RayMarchSamples", RayMarchSamples);
        material.SetFloat("_LightRayLength", LightRayLength.value);
        material.SetInt("_LightSamples", (int)LightSteps.value);
        material.SetFloat("_LightAbsorbation", LightAbsorbation.value);
    }

    unsafe bool PrepareVolume()
    {
        LoadNVDB(volumePath, out nanoVolume);

        if (nanoVolume != null)
        {
            Debug.Log($"NanoVDB initialized successfully. size={nanoVolume->byteSize} bytes, " +
                $"count={nanoVolume->elementCount}, stride={nanoVolume->structStride}");

            return true;
        }
        else
        {
            Debug.LogError("Failed to create NanoVolume, aborting.");
            return false;
        }
    }

    void PrepareRenderOnObject()
    {
        renderOn = transform.GetChild(0).gameObject;
        renderOn.SetActive(true);
        mesh = renderOn.GetComponent<MeshRenderer>();
        material = new Material(nanoVDBShader);
        mesh.material = material;
    }

    void OnDestroy()
    {
        nvdb?.Dispose();

        unsafe
        {
            if (nanoVolume != null)
            {
                FreeNVDB(nanoVolume);
                nanoVolume = null;
            }
        }
    }

    private delegate void DebugLogDelegate(IntPtr message);

    private static void DebugLogCallback(IntPtr message) { Debug.Log($"[NanoVDBWrapper.dll]: {Marshal.PtrToStringAnsi(message)}"); }

    [DllImport("NanoVDBWrapper", EntryPoint = "SetDebugLogCallback")]
    private static extern void SetDebugLogCallback(DebugLogDelegate callback);

    [DllImport("NanoVDBWrapper", EntryPoint = "LoadNVDB")]
    private unsafe static extern void LoadNVDB(string path, out NanoVolume* ptrToStruct);

    [DllImport("NanoVDBWrapper", EntryPoint = "FreeNVDB")]
    private unsafe static extern void FreeNVDB(NanoVolume* ptrToStruct);
}
