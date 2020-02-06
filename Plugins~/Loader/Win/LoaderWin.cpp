// LoaderWin.cpp : Win implementation of Loader APIs
//

#include "stdafx.h"

//CommonLib
#include "CommonLib/Types.h"
#include "CommonLib/ReadResult.h"

//Loader
#include "Loader/TGALoader.h"


#pragma comment( lib, "winmm.lib" )
#pragma comment(lib, "gdiplus.lib")


//----------------------------------------------------------------------------------------------------------------------

void LoadPNGFileAndAlloc(const charType* fileName, StReadResult* pResult) {
	u8* pBuffer = NULL;

# if USE_WCHAR
	Gdiplus::Bitmap*    pBitmap = Gdiplus::Bitmap::FromFile(fileName);
# else
	const int size = 8192;
	WCHAR wlocal[size] = { 0x00 };


	int ret = MultiByteToWideChar(
		CP_ACP,
		MB_PRECOMPOSED,
		fileName,
		static_cast<int>(strlen(fileName)),
		wlocal,
		size);
	Gdiplus::Bitmap*    pBitmap = Gdiplus::Bitmap::FromFile(wlocal);

# endif
	Gdiplus::Status status = Gdiplus::FileNotFound;
	if (pBitmap )
	{
		status = pBitmap->GetLastStatus();
	}
	ASSERT(status == Gdiplus::Ok);
	if (status == Gdiplus::Ok)
	{

		u32 width = pBitmap->GetWidth();
		u32 height = pBitmap->GetHeight();

		pBuffer = (u8*)malloc(sizeof(u32)* width * height);
		ASSERT(pBuffer !=nullptr);
		u32* pImage = (u32*)pBuffer;

		pResult->width = width;
		pResult->height = height;
		pResult->buffer = pBuffer;
		pResult->readStatus = StreamingImageSequencePlugin::READ_STATUS_SUCCESS;

		Gdiplus::BitmapData bitmapData;
		pBitmap->LockBits(&Gdiplus::Rect(0, 0, width, height), Gdiplus::ImageLockModeWrite, PixelFormat32bppARGB, &bitmapData);
		u32 *pRawBitmapOrig = (u32*)bitmapData.Scan0;   // for easy access and indexing
		for (u32 yy = 0; yy < height; yy++)
		{
			memcpy(&pImage[yy*width], &pRawBitmapOrig[(height - 1 - yy) * bitmapData.Stride / 4], width*sizeof(u32));

		}

		//memcpy(pImage, bitmapData.Scan0, width*height*sizeof(UINT32));
		pBitmap->UnlockBits(&bitmapData);
		delete pBitmap;
	}
	else
	{
		pResult->readStatus = StreamingImageSequencePlugin::READ_STATUS_FAIL;
		ASSERT(0);
	}

}

//----------------------------------------------------------------------------------------------------------------------
void LoadTGAFileAndAlloc(const charType* fileName, StReadResult* readResult) {
	
	loadTGAFileAndAlloc(fileName, readResult);
}

