// CommonLibWin.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "CommonLib.h"

#include "CriticalSectionObject.h"

using namespace std;

COMMONLIB_API    map<strType, StReadResult>  g_fileNameToPtrMap[StreamingImageSequencePlugin::MAX_CRITICAL_SECTION_TYPE_TEXTURES];
COMMONLIB_API    map<int, strType>           g_instanceIdToFileName;
COMMONLIB_API    map<int, void*>             g_instanceIdToUnityTexturePointer;
COMMONLIB_API    map<strType, int>           g_scenePathToSceneStatus;

//----------------------------------------------------------------------------------------------------------------------

