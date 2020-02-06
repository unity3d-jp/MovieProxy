using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace UnityEngine.StreamingImageSequence {

    //Delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DelegateStringFunc([MarshalAs(UnmanagedType.LPStr)] string str);
//----------------------------------------------------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
    public struct ReadResult
    {
        public IntPtr Buffer;
        [MarshalAs(UnmanagedType.I4)]
        public int Width;
        [MarshalAs(UnmanagedType.I4)]
        public int Height;
        [MarshalAs(UnmanagedType.I4)]
        public int ReadStatus;
    };

//----------------------------------------------------------------------------------------------------------------------
    public static class StreamingImageSequencePlugin {

//only support Windows and OSX
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX


#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private const string LOADER_DLL             = "Loader";
        private const string DRAWER_DLL             = "Drawer";
        private const string DRAW_OVER_WINDOW_DLL   = "DrawOverWindow";
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        private const string LOADER_DLL             = "Project";
        private const string DRAWER_DLL             = "Project";
        private const string DRAW_OVER_WINDOW_DLL   = "Project";
#endif

        // Implemented in Loader dll
        [DllImport(LOADER_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern bool LoadAndAllocFullTexture([MarshalAs(UnmanagedType.LPStr)]string fileName);

        [DllImport(LOADER_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern bool LoadAndAllocPreviewTexture([MarshalAs(UnmanagedType.LPStr)]string fileName, int width, int height);

        [DllImport(LOADER_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern void NativeFree(IntPtr ptr);

        [DllImport(LOADER_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern bool GetNativeTextureInfo([MarshalAs(UnmanagedType.LPStr)]string fileName, out ReadResult tResult, int textureType);

        [DllImport(LOADER_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int ResetNativeTexture([MarshalAs(UnmanagedType.LPStr)]string fileName);

        [DllImport(LOADER_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern void ListLoadedTextures(int textureType, DelegateStringFunc func);

        [DllImport(LOADER_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern void ResetPlugin();

        [DllImport(LOADER_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern void  DoneResetPlugin();
        [DllImport(LOADER_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int   IsPluginResetting();

        // Implemented in Drawer dll
        [DllImport(DRAWER_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern void SetNativeTexturePtr(IntPtr Texture, UInt32 uWidth, UInt32 height, Int32 sObjectID);

        [DllImport(DRAWER_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern void SetLoadedTexture([MarshalAs(UnmanagedType.LPStr)]string fileName, Int32 sObjectID);

        [DllImport(DRAWER_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern void ResetLoadedTexture(Int32 sObjectID);

        [DllImport(DRAWER_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern void ResetAllLoadedTexture();

        [DllImport(DRAWER_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern IntPtr GetRenderEventFunc();

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        [DllImport(DRAW_OVER_WINDOW_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern IntPtr TestDraw(int posX, int posY);

        [DllImport(DRAW_OVER_WINDOW_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern void LoadAndShowBitMap(int posX, int posY, [MarshalAs(UnmanagedType.LPStr)]string fileName);

        [DllImport(DRAW_OVER_WINDOW_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern void ShowOverwrapWindow(int sInstanceID, int posX, int posY, int sWidth, int sHeight, int forceDraw);

        [DllImport(DRAW_OVER_WINDOW_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern void HideOverwrapWindow(int sInstanceID);
        [DllImport(DRAW_OVER_WINDOW_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern void SetOverwrapWindowData(int sInstanceID, UInt32[] byteArray, int length );
        [DllImport(DRAW_OVER_WINDOW_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern void HideAllOverwrapWindows();

        [DllImport(DRAW_OVER_WINDOW_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern void SetAllAreLoaded(int sInstanceID,int flag);
        [DllImport(DRAW_OVER_WINDOW_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int GetAllAreLoaded(int sInstanceID);

        [DllImport(DRAW_OVER_WINDOW_DLL, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern void ResetOverwrapWindows();

#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX

        public static IntPtr TestDraw(int posX, int posY) { return IntPtr.Zero; }       
        public static void LoadAndShowBitMap(int posX, int posY, [MarshalAs(UnmanagedType.LPStr)]string fileName) { }
        public static void ShowOverwrapWindow(int sInstanceID, int posX, int posY, int sWidth, int sHeight, int forceDraw) { }
        public static void HideOverwrapWindow(int sInstanceID) { }
        public static void SetOverwrapWindowData(int sInstanceID, UInt32[] byteArray, int length ) { }
        public static void HideAllOverwrapWindows() { }
        public static void SetAllAreLoaded(int sInstanceID,int flag) {}
        public static int GetAllAreLoaded(int sInstanceID) { return 0; }
        public static void ResetOverwrapWindows() { }
#endif //Platform-dependent support


#endif //UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX

//----------------------------------------------------------------------------------------------------------------------

        public static bool IsResetting() {
            return (StreamingImageSequencePlugin.IsPluginResetting() != 0);
        }

//----------------------------------------------------------------------------------------------------------------------

        public static Texture2D CreateTexture(ref ReadResult readResult) {
            Assert.IsTrue(StreamingImageSequenceConstants.READ_RESULT_SUCCESS == readResult.ReadStatus);
            
            int length = readResult.Width * readResult.Height * 4;
            Texture2D tex = new Texture2D(readResult.Width, readResult.Height,
                StreamingImageSequenceConstants.TEXTURE_FORMAT, false, false
            );

            unsafe {
                void* src = readResult.Buffer.ToPointer();
                NativeArray<float> rawTextureData = tex.GetRawTextureData<float>();
                void* dest = rawTextureData.GetUnsafePtr();
                Buffer.MemoryCopy(src, dest, length, length);
            }
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();


            return tex;

        }
    }

}
