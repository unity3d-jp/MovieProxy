#include "stdafx.h"

//Loader
#include "LoaderUtility.h"
#include "FileType.h"
#include "TGALoader.h"

//External
#include "External/stb/stb_image_resize.h"

//----------------------------------------------------------------------------------------------------------------------
//Forward declarations
void LoadTGAFileAndAlloc(const charType* fileName, StReadResult* pResult);
void LoadPNGFileAndAlloc(const charType* fileName, StReadResult* pResult);

//----------------------------------------------------------------------------------------------------------------------

namespace StreamingImageSequencePlugin {

FileType LoaderUtility::CheckFileType(const charType* fileName) {
    using namespace StreamingImageSequencePlugin;
   
    const size_t length =
#if USE_WCHAR
        wcslen(fileName);
#else
        strlen(fileName);
#endif

    //Check extension validity
    if (length < 4 || fileName[length - 4] != '.')    {
        return FILE_TYPE_INVALID;
    }

#ifdef _WIN32    // TGA is only supported on Windows
    if ((fileName[length - 3] == 'T' || fileName[length - 3] == 't')
        && (fileName[length - 2] == 'G' || fileName[length - 2] == 'g')
        && (fileName[length - 1] == 'A' || fileName[length - 1] == 'a'))
    {
        return FILE_TYPE_TGA;
    }
#endif
    
    return FILE_TYPE_PNG;
}

//----------------------------------------------------------------------------------------------------------------------
//Returns whether the file has been processed, or is still processed  (inside readResultMap).

bool LoaderUtility::GetTextureInfo(const charType* fileName, StReadResult* pResult,
                                  std::map<strType, StReadResult>* readResultMap, const uint32_t textureType)
{
    using namespace StreamingImageSequencePlugin;
    ASSERT(pResult);
    pResult->readStatus = READ_STATUS_NONE;
    strType wstr(fileName);

    if (readResultMap->find(wstr) != readResultMap->end()) {
        *pResult = readResultMap->at(wstr);
            
        //if success, then the buffer must be not null
        ASSERT(pResult->readStatus != READ_STATUS_SUCCESS || pResult->buffer);
        return true;
    }
    return false;

}

//----------------------------------------------------------------------------------------------------------------------
//Returns whether the file has been processed, or is still processed  (inside readResultMap).
bool LoaderUtility::LoadAndAllocTexture(const charType* fileName, std::map<strType, StReadResult>* readResultMap, const uint32_t texType) 
{
    using namespace StreamingImageSequencePlugin;
    StReadResult readResult;

    const bool isProcessed = LoaderUtility::GetTextureInfo(fileName, &readResult, readResultMap, texType);
    if (isProcessed) {
        return true;
    }

    const FileType fileType = LoaderUtility::CheckFileType(fileName);
    if (FILE_TYPE_INVALID == fileType)
        return false;

    //Loading
    strType wstr(fileName);
    readResult.readStatus = READ_STATUS_LOADING;
    (*readResultMap)[wstr] = readResult;

    switch (fileType) {
        case FILE_TYPE_TGA: {
            LoadTGAFileAndAlloc(fileName, &readResult);
            break;
        }
        case FILE_TYPE_PNG: {
            LoadPNGFileAndAlloc(fileName, &readResult);
            break;
        }
        default: { break; }
    }

    (*readResultMap)[wstr] = readResult;

    return true;

}

//----------------------------------------------------------------------------------------------------------------------
//Returns whether the file has been processed, or is still processed  (inside readResultMap).
bool LoaderUtility::LoadAndAllocTexture(const charType* fileName, std::map<strType, StReadResult>* readResultMap,
    const uint32_t texType, const uint32_t reqWidth, const uint32_t reqHeight) 
{

    if (!LoaderUtility::LoadAndAllocTexture(fileName, readResultMap, texType))
        return false;

    StReadResult readResult;
    if (!GetTextureInfo(fileName, &readResult, readResultMap, texType))
        return false;

    //Buffer is still null. Means, still loading
    if (NULL == readResult.buffer) {
        return true; 
    }

    //Already resized. 
    if (readResult.width == reqWidth && readResult.height == reqHeight)
        return true;

    {
        const uint64_t NUM_CHANNELS = 4;
        u8* resizedBuffer = (u8*)malloc(static_cast<uint32_t>(NUM_CHANNELS * reqWidth * reqHeight));

        stbir_resize_uint8(readResult.buffer, readResult.width, readResult.height, 0,
            resizedBuffer, reqWidth, reqHeight, 0, NUM_CHANNELS);
        free(readResult.buffer);
        readResult.buffer = resizedBuffer;
        readResult.width = reqWidth;
        readResult.height = reqHeight;
        (*readResultMap)[fileName] = readResult;
    }

    return true;
}


} //end namespace
