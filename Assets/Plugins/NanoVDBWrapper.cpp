#define NANOVDB_USE_OPENVDB
#define DLLExport __declspec(dllexport)

#include <iostream>
#include <nanovdb/util/IO.h>

struct NanoVolume {
	uint32_t* buf;
	uint64_t byteSize;
	uint64_t elementCount;
	uint64_t structStride;
} nanoVolumeStruct;

extern "C" {
	typedef void(*DebugLogCallback)(const char*);

	DLLExport void SetDebugLogCallback(DebugLogCallback callback);
	DLLExport void LoadNVDB(const char* str, struct NanoVolume** volume);
	DLLExport void FreeNVDB(struct NanoVolume* volume);
}

DebugLogCallback UnityLog = nullptr;
void SetDebugLogCallback(DebugLogCallback callback) {
	UnityLog = callback;
}

nanovdb::GridHandle<nanovdb::HostBuffer> gridHandle;
void LoadNVDB(const char* path, struct NanoVolume** volume) {
    try {
        // reads first grid from file
        gridHandle = nanovdb::io::readGrid(path); 

        // get a (raw) pointer to a NanoVDB grid of value type float
        const nanovdb::FloatGrid* buf = gridHandle.grid<float>();

		if (!buf) {
			throw std::runtime_error("File did not contain a grid with value type float");
		}

		*volume = (struct NanoVolume*) malloc(sizeof(struct NanoVolume));

		if (*volume == nullptr) {
			UnityLog("Failed to allocate memory for NanoVolume struct.");
			return;
		}

		const uint64_t byteSize = gridHandle.buffer().bufferSize();
		const uint64_t elementCount = byteSize / sizeof(float);
		const uint64_t structStride = sizeof(float);
		
		uint32_t* buffer_ptr = (uint32_t*)gridHandle.buffer().data();

		(*volume)->buf = buffer_ptr;
		(*volume)->byteSize = byteSize;
		(*volume)->elementCount = elementCount;
		(*volume)->structStride = structStride;
    }
    catch (const std::exception& e) {
        UnityLog("An exception occurred:");
		UnityLog(e.what());
    }
}

void FreeNVDB(struct NanoVolume* volume) {
	free(volume);
	volume = nullptr;
}
