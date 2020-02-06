#pragma once

#include "Types.h"
#include "ReadStatus.h"

struct StReadResult
{
    u8*  buffer;
    u32    width;
    u32    height;
    StreamingImageSequencePlugin::ReadStatus readStatus;
    
    StReadResult()
    {
        buffer = NULL;
        width = 0;
        height = 0;
        readStatus = StreamingImageSequencePlugin::READ_STATUS_NONE;
    }
};

