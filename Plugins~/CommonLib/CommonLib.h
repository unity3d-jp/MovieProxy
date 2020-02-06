// The following ifdef block is the standard way of creating macros which make exporting 
// from a DLL simpler. All files within this DLL are compiled with the COMMONLIBWIN_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see 
// COMMONLIB_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifndef COMMONLIB
#define COMMONLIB

#include "ReadStatus.h"
#include "Types.h"

#include "../Drawer/PlatformBase.h"
#include "../Drawer/Unity/IUnityGraphics.h"

#if SUPPORT_OPENGL_LEGACY
#  include "../Drawer/GLEW/glew.h"
#endif


typedef void* TexPointer;


#include "ReadResult.h"
#include "CriticalSection.h"

extern COMMONLIB_API std::map<strType, StReadResult> g_fileNameToPtrMap[StreamingImageSequencePlugin::MAX_CRITICAL_SECTION_TYPE_TEXTURES];
extern COMMONLIB_API std::map<int, strType>          g_instanceIdToFileName;
extern COMMONLIB_API std::map<int, void*>            g_instanceIdToUnityTexturePointer;
extern COMMONLIB_API std::map<strType, int>          g_scenePathToSceneStatus;

//----------------------------------------------------------------------------------------------------------------------


#define TEXTURE_CS(texType)         (StreamingImageSequencePlugin::CriticalSection::GetInstance().GetObject(static_cast<StreamingImageSequencePlugin::CriticalSectionType>(texType)))
#define INSTANCEID2FILENAME_CS      (StreamingImageSequencePlugin::CriticalSection::GetInstance().GetObject(StreamingImageSequencePlugin::CRITICAL_SECTION_TYPE_INSTANCE_ID_TO_FILENAME))
#define INSTANCEID2TEXTURE_CS       (StreamingImageSequencePlugin::CriticalSection::GetInstance().GetObject(StreamingImageSequencePlugin::CRITICAL_SECTION_TYPE_INSTANCE_ID_TO_UNITY_TEXTURE_PTR))
#define LOADINGCOUNTER_CS           (StreamingImageSequencePlugin::CriticalSection::GetInstance().GetObject(StreamingImageSequencePlugin::CRITICAL_SECTION_TYPE_LOADING_COUNTER))
#define RESETTING_CS                (StreamingImageSequencePlugin::CriticalSection::GetInstance().GetObject(StreamingImageSequencePlugin::CRITICAL_SECTION_TYPE_RESETTING))


#endif //#ifdef COMMONLIB
