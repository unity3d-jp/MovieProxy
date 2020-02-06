using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.StreamingImageSequence;

namespace UnityEditor.StreamingImageSequence {

    public static class StreamingImageSequenceMenu
    {
        public const string PNG_EXTENSION = "png";
        public const string TGA_EXTENSION = "tga";

        [MenuItem(StreamingImageSequenceConstants.MENU_PATH +  "Create Clip", false, 1)]
        private static void RegisterFilesAndCreateStreamingImageSequence()
        {
            string path = EditorUtility.OpenFilePanel("Open File", "", PNG_EXTENSION + "," + TGA_EXTENSION);
            if (string.IsNullOrEmpty(path)) {
                return;
            }

            ImageSequenceImporter.ImportPictureFiles(PictureFileImporterParam.Mode.StreamingAssets, path, null);
        }

//----------------------------------------------------------------------------------------------------------------------
        [MenuItem(StreamingImageSequenceConstants.MENU_PATH + "Convert MovieProxy to SpriteAnimation", false, 5)]
        private static void ConvertToSpriteAnimation()
        {
            if (Selection.gameObjects == null || 0 == Selection.gameObjects.Length)
            {
                return;
            }
            foreach (var orgGo in Selection.gameObjects)
            {
                StreamingImageSequenceToSpriteAnimation.ConvertIt(orgGo);
            }
        }

//----------------------------------------------------------------------------------------------------------------------

        [MenuItem(StreamingImageSequenceConstants.MENU_PATH + "Import AE Timeline", false, 10)]
        static void ImportAETimeline() {
            string strPath = EditorUtility.OpenFilePanel("Open File", "", "jstimeline");
            if (strPath.Length != 0) {
                JstimelineImporter.ImportTimeline(strPath);
            }
        }

//----------------------------------------------------------------------------------------------------------------------

        /*

        [MenuItem(StreamingImageSequenceConstants.MENU_PATH + "Create MovieProxy/Register files", false, 6)]
        static void ImportAndCreateSpriteAnimation()
        {
            importPictureFiles(PictureFileImporterParam.Mode.SpriteAnimation);

        }
        */

        [MenuItem(StreamingImageSequenceConstants.MENU_PATH + "Reset",false,50)]
        static void Reset()
        {
            UpdateManager.ResetPlugin();
            PreviewTextureFactory.Reset();
        }


//----------------------------------------------------------------------------------------------------------------------
        [MenuItem(StreamingImageSequenceConstants.MENU_PATH + "Show Loaded Textures",false,52)]
        static void ShowLoadedTextures() {
            StringBuilder sb = new StringBuilder();

            for (int textureType = 0; textureType < StreamingImageSequenceConstants.MAX_TEXTURE_TYPES; ++textureType) {
                sb.AppendLine("TEXTURE_TYPE: " + textureType.ToString());

                List<string> loadedTextures = new List<string>();
                StreamingImageSequencePlugin.ListLoadedTextures(textureType, (fileName) => {
                    loadedTextures.Add(fileName);
                });

                foreach (var fileName in loadedTextures) {
                    StreamingImageSequencePlugin.GetNativeTextureInfo(fileName, out ReadResult readResult, textureType);
                    sb.Append("    ");
                    sb.Append(fileName);
                    sb.Append(". Status: " + readResult.ReadStatus);
                    sb.Append(", Size: (" + readResult.Width + ", " + readResult.Height);
                    sb.AppendLine(") ");
                }

                sb.AppendLine("----------------------------------------------------------------");
                sb.AppendLine();
                sb.AppendLine();
            }
            Debug.Log(sb.ToString());
        }

//----------------------------------------------------------------------------------------------------------------------

    }


}