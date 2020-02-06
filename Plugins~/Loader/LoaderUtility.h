#pragma once

#include "CommonLib/Types.h"
#include "CommonLib/ReadResult.h"
#include "FileType.h"

namespace StreamingImageSequencePlugin {

//[Note-sin: 2020-2-4] The functions in this class are not guaranteed to be thread-safe. 
//Use CriticalSectionController to guarantee thread safe when calling if required.
class LoaderUtility {
public:
    static FileType CheckFileType(const charType* fileName);
    static bool GetTextureInfo(const charType* fileName, StReadResult* pResult,
                                      std::map<strType, StReadResult>* readResultMap, const uint32_t texType);

    static bool LoadAndAllocTexture(const charType* fileName, std::map<strType, StReadResult>* readResultMap, 
        const uint32_t texType);

    static bool LoadAndAllocTexture(const charType* fileName, std::map<strType, StReadResult>* readResultMap,
        const uint32_t texType, const uint32_t reqWidth, const uint32_t reqHeight);

};

} //end namespace

