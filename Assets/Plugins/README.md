# Wrapper for NanoVDB to Unity

> See `NanoVDBWrapper.cpp` for source code.

The NanoVDB library is imported to Unity through a C++ wrapper. This wrapper exposes three functions, which we use in our Unity script `Assets\NanoVDB\NanoLoader.cs`:

* `void SetDebugLogCallback(DebugLogCallback callback)`
* `void LoadNVDB(const char* str, struct NanoVolume** volume)`
* `void FreeNVDB(struct NanoVolume* volume)`

The `LoadNVDB` is most important, as it loads a `.nvdb` file from disk and enables Unity to access it through the `NanoVolume` struct. This struct mainly contains a pointer to the VDB grid buffer, which is uploaded to the GPU through the `NanoLoader.cs` script. That is basically it for the wrapper.

*Note:* it is not recommended by the author, Ken Museth, to store `.nvdb` files on disk. Instead it is suggested to convert an OpenVDB (`.vdb`) to NanoVDB (`.nvdb`) at runtime, but that requires importing all of OpenVDB. I decided to accept a larger file size on disk instead of bothering to import OpenVDB.

## Compile to Unity

The source is compiled to Unity with MSVC and CLR. In Visual Studio, the `Common Language Runtime Support` field is set to `.NET Framework Runtime Support (/clr)`, with the target version of `v4.8`. For Unity to accept version `4.8` we need to go into Unity, Edit, Project Settings, Player, Other Settings, and set `Api Compatibility Level*` to `.NET Framework`. This will make Unity import our DLL when we have a Unity script that uses it.

## References

* CLR: https://learn.microsoft.com/en-us/cpp/dotnet/dotnet-programming-with-cpp-cli-visual-cpp
* Logging from DLL in Unity: https://stackoverflow.com/questions/43732825/use-debug-log-from-c
* Unity Managed plugins: https://docs.unity3d.com/6000.0/Documentation/Manual/plug-ins-managed.html
* NanoVDB presentation by Ken Museth under Supplementary Material: https://dl.acm.org/doi/10.1145/3450623.3464653
