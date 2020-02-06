#pragma once

#ifdef _WIN32
#  ifdef COMMONLIBWIN_EXPORTS
#  define COMMONLIB_API __declspec(dllexport)
#  else
#  define COMMONLIB_API __declspec(dllimport)
#  endif
#else
#  define COMMONLIB_API
#endif


#ifdef _WIN32
#  include <d3d11.h>
#  include <objidl.h>
#  include <gdiplus.h>
#endif //_WIN32

#include <thread>
#include <mutex>
#include <string>
#include <map>

#include <stdio.h>
#include <stdlib.h>
#include <locale.h>
#include <string.h>


#ifdef _WIN32
    typedef UINT64 u64;
    typedef UINT32 u32;
    typedef INT32 s32;
    typedef UINT16 u16;
    typedef UINT8 u8;
#else
    typedef unsigned long u64;
    typedef unsigned int u32;
    typedef int s32;
    typedef short u16;
    typedef unsigned char u8;
    typedef wchar_t WCHAR;
#endif


#define  USE_WCHAR 0

#if USE_WCHAR
  typedef WCHAR charType;
  typedef std::wstring strType;
#else
  typedef char charType;
  typedef std::string strType;
#endif


#define BIT(x) (1<<x)
#if _DEBUG && defined(_WIN32)
#define ASSERT( xx_ )  (xx_) ? (void)0 : __debugbreak()
#else
#define ASSERT( xx_ )   (void)0 
#endif
