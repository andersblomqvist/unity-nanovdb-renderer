using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class NanoVolumeLoader : MonoBehaviour
{
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct NanoVolume
    {
        public uint* buf;
        public ulong byteSize;
        public ulong elementCount;
        public ulong structStride;
    }; unsafe NanoVolume* nanoVolume;

    [Header("Assets/path/to/volume.nvdb")]

    public string volumePath;

    private ComputeBuffer gpuBuffer;
    private uint[] buf;
    private bool ok;

    private void Awake()
    {
        SetDebugLogCallback(DebugLogCallback);
        
        ok = PrepareVolume();
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

            gpuBuffer = new ComputeBuffer(
                bufferSize,
                stride,
                ComputeBufferType.Default
            );
            gpuBuffer.SetData(buf);

            Debug.Log("GPU Buffer initialized");
        }
    }

    private unsafe bool PrepareVolume()
    {
        LoadNVDB(volumePath, out nanoVolume);

        if (nanoVolume != null)
        {
            Debug.Log($"NanoVDB initialized successfully. size={nanoVolume->byteSize} bytes, " +
                $"array length={nanoVolume->elementCount}, stride={nanoVolume->structStride}");

            return true;
        }
        else
        {
            Debug.LogError("Failed to create NanoVolume, aborting.");
            return false;
        }
    }

    private void OnDestroy()
    {
        gpuBuffer?.Dispose();

        unsafe
        {
            if (nanoVolume != null)
            {
                FreeNVDB(nanoVolume);
                nanoVolume = null;
            }
        }
    }

    public ComputeBuffer GetGPUBuffer()
    {
        if (gpuBuffer == null)
        {
            Debug.LogError("Buffer is null. Make sure the NanoLoader is finished before accessing this buffer.");
            return null;
        }

        return gpuBuffer;
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
