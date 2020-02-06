// Loader.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "Loader.h"

//CommonLib
#include "CommonLib/CommonLib.h"
#include "CommonLib/CriticalSectionController.h"


//Loader
#include "TGALoader.h"
#include "FileType.h"
#include "LoaderUtility.h"


using namespace std;

//----------------------------------------------------------------------------------------------------------------------

int g_IsResetting;

//----------------------------------------------------------------------------------------------------------------------

//Get the texture info and return the result inside ReadResult. Thread-safe
LOADER_API bool GetNativeTextureInfo(const charType* fileName, StReadResult* readResult, const uint32_t textureType) {
    using namespace StreamingImageSequencePlugin;
    CriticalSectionController cs(TEXTURE_CS(textureType));
    return LoaderUtility::GetTextureInfo(fileName, readResult, &g_fileNameToPtrMap[textureType], textureType );
}

//----------------------------------------------------------------------------------------------------------------------

//Returns if the fileName can be loaded (). Thread-safe
LOADER_API bool LoadAndAllocFullTexture(const charType* fileName) {

    using namespace StreamingImageSequencePlugin;
    const uint32_t textureType = CRITICAL_SECTION_TYPE_FULL_TEXTURE;
	CriticalSectionController cs(TEXTURE_CS(textureType));
	return LoaderUtility::LoadAndAllocTexture(fileName, &g_fileNameToPtrMap[textureType], textureType);
}

//----------------------------------------------------------------------------------------------------------------------
//Returns if the fileName can be loaded (). Thread-safe
LOADER_API bool LoadAndAllocPreviewTexture(const charType* fileName, const uint32_t width, const uint32_t height) {
	using namespace StreamingImageSequencePlugin;
	const uint32_t textureType = CRITICAL_SECTION_TYPE_PREVIEW_TEXTURE;
	CriticalSectionController cs(TEXTURE_CS(textureType));
	return LoaderUtility::LoadAndAllocTexture(fileName, &g_fileNameToPtrMap[textureType], textureType, width, height);
}

//----------------------------------------------------------------------------------------------------------------------

LOADER_API void   NativeFree(void* ptr) {
	free(ptr);
}


//----------------------------------------------------------------------------------------------------------------------
// return succ:0 fail:-1
LOADER_API int   ResetNativeTexture(const charType* fileName) {
    using namespace StreamingImageSequencePlugin;

	//Reset all textures
	for (uint32_t texType = 0; texType < MAX_CRITICAL_SECTION_TYPE_TEXTURES; ++texType) {
		StReadResult readResult;
		GetNativeTextureInfo(fileName, &readResult, texType);

		//Check
		if (!readResult.buffer || readResult.readStatus != READ_STATUS_SUCCESS) {
			continue;
		}

		CriticalSectionController cs0(TEXTURE_CS(texType));
		{
			strType wstr(fileName);
			NativeFree(readResult.buffer);
			g_fileNameToPtrMap[texType].erase(wstr);
		}
	}


	return 0;

}

//----------------------------------------------------------------------------------------------------------------------
LOADER_API void ListLoadedTextures(const uint32_t textureType, void(*OnNextTexture)(const char*)) {
	using namespace StreamingImageSequencePlugin;
	ASSERT(textureType < MAX_CRITICAL_SECTION_TYPE_TEXTURES);

	for (auto itr = g_fileNameToPtrMap[textureType].begin(); itr != g_fileNameToPtrMap[textureType].end(); ++itr) {
		OnNextTexture(itr->first.c_str());
	}

}

//----------------------------------------------------------------------------------------------------------------------

LOADER_API void   SetSceneStatus(const charType* scenePath, int sceneStatus)
{
	g_scenePathToSceneStatus[scenePath] = sceneStatus;

}
LOADER_API int    GetSceneStatus(const charType* scenePath)
{
	strType wstr(scenePath);
	if (g_scenePathToSceneStatus.find(wstr) != g_scenePathToSceneStatus.end())
	{
		return g_scenePathToSceneStatus[wstr];
	}
	return -1;	// not found;
}

LOADER_API void  ResetPlugin()
{
	StreamingImageSequencePlugin::CriticalSectionController cs2(RESETTING_CS);
	g_IsResetting = 1;
}

LOADER_API void  DoneResetPlugin()
{
	StreamingImageSequencePlugin::CriticalSectionController cs2(RESETTING_CS);
	g_IsResetting = 0;
}

LOADER_API int   IsPluginResetting()
{
	StreamingImageSequencePlugin::CriticalSectionController cs2(RESETTING_CS);
	return g_IsResetting ;
}
