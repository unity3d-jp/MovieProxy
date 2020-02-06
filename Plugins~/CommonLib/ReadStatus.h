#pragma once

namespace StreamingImageSequencePlugin {

enum ReadStatus {
    READ_STATUS_NONE = 0,
    READ_STATUS_LOADING,
    READ_STATUS_SUCCESS,
    READ_STATUS_FAIL,
};

} //end namespace
