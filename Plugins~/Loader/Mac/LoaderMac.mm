//  Loader.mm
//  Project
//
//  Created by Hiroki Omae on 2017/10/10.
//  Copyright © 2017 Unity Technologies. All rights reserved.
//

#import <Foundation/Foundation.h>
#include <ApplicationServices/ApplicationServices.h>

#include "CommonLib/CommonLib.h"

//Loader
#include "Loader.h"

#define DEBUG_MAC_DRAWING (0)

CGImageRef CGImageRefLoad(const char *filename) {
    NSString *path = [NSString stringWithUTF8String:filename];
    
    CGDataProviderRef dataProvider = CGDataProviderCreateWithFilename([path UTF8String]);
    CGImageRef image = CGImageCreateWithPNGDataProvider(dataProvider, NULL, NO, kCGRenderingIntentDefault);

    CGDataProviderRelease(dataProvider);
    
    return image;
}

//----------------------------------------------------------------------------------------------------------------------
u8* CGImageRefRetrievePixelData(const CGImageRef image, u32 width, u32 height) {
    u8* data = (u8*)malloc(width*height*4);
    memset(data,0, width*height*4);
    CGContextRef context = CGBitmapContextCreate(data,
                                                 width, height,
                                                 8, width * 4,
                                                 CGImageGetColorSpace(image),
                                                 kCGImageAlphaPremultipliedLast);

    
    CGContextTranslateCTM(context, 0, height);
    CGContextScaleCTM(context, 1.0, -1.0);
#if DEBUG_MAC_DRAWING
    CGContextSetRGBFillColor(context, 1.0f,0.0f,0.0f,0.5f);
    CGContextFillRect(context, CGRectMake(0, 0, width, height));
#endif
    CGContextDrawImage(context,
                       CGRectMake(0.0, 0.0, (float)width, (float)height),
                       image);
    CGContextRelease(context);
    
    return data;
}

//----------------------------------------------------------------------------------------------------------------------

void* LoadPNGFileAndAlloc(const charType* fileName, StReadResult* pResult) {
    u8* pBuffer = NULL;
    
    const CGImageRef image = CGImageRefLoad(fileName);
    if(image != NULL) {
        const u32 width = (u32) CGImageGetWidth(image);
        const u32 height = (u32) CGImageGetHeight(image);
        pBuffer =CGImageRefRetrievePixelData(image, width, height);
        
        if(pBuffer != NULL) {
            pResult->width  = width;
            pResult->height = height;
            pResult->buffer = pBuffer;
            pResult->readStatus = StreamingImageSequencePlugin::READ_STATUS_SUCCESS;
        } else {
            pResult->readStatus = StreamingImageSequencePlugin::READ_STATUS_FAIL;
        }
        
        CGImageRelease(image);
    }
    
    return pBuffer; //  pBuffer;
}

//----------------------------------------------------------------------------------------------------------------------
void LoadTGAFileAndAlloc(const charType* fileName, StReadResult* pResult) {
    assert(false);   //Not implemented yet
}
//----------------------------------------------------------------------------------------------------------------------

