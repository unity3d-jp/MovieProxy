#include "stdafx.h"

#include "CommonLib/Types.h"

#pragma comment(lib, "gdiplus.lib")

// This class is exported from the CommonLibWin.dll
class COMMONLIB_API CCommonLib {
	Gdiplus::GdiplusStartupInput    startInput;
	ULONG_PTR                       token;

public:
	CCommonLib();
	virtual ~CCommonLib();
};

COMMONLIB_API	CCommonLib		g_CCommonLib;

// This is the constructor of a class that has been exported.
// see CommonLibWin.h for the class definition
CCommonLib::CCommonLib()
{
	int status = Gdiplus::GdiplusStartup(&token, &startInput, NULL);
}

CCommonLib::~CCommonLib()
{
	Gdiplus::GdiplusShutdown(token);
}

