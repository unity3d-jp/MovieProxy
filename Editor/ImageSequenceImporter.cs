using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.StreamingImageSequence;
using UnityEngine.UI;

namespace UnityEditor.StreamingImageSequence {

    public static class ImageSequenceImporter
    {
        private const string PNG_EXTENSION = "png";
        private const string TGA_EXTENSION = "tga";


        /// Import images in the path to create StreamingImageSequence assets with those images
        /// <param name="importerMode"> Importer mode: StreamingAssets or SpriteAnimation</param>
        /// <param name="path"> Can be a directory path or a file path</param>
        public static void ImportPictureFiles(PictureFileImporterParam.Mode importerMode, string path, 
                StreamingImageSequencePlayableAsset targetAsset) 
        {
            Assert.IsFalse(string.IsNullOrEmpty(path));

            //Convert path to folder here
            string folder = path;
            FileAttributes attr = File.GetAttributes(path);
            if (!attr.HasFlag(FileAttributes.Directory)) {
                folder = Path.GetDirectoryName(folder);
            }
            string fullSrcPath = Path.GetFullPath(folder).Replace("\\", "/");
            Uri fullSrcPathUri = new Uri(fullSrcPath + "/");


            if (string.IsNullOrEmpty(folder)) {
                Debug.LogError(@"Folder is empty. Path: " + path);
                return;
            }

            //Enumerate all files with the supported extensions and sort
            List<string> relFilePaths = new List<string>();
            string[] extensions = {
                "*." + ImageSequenceImporter.PNG_EXTENSION, 
                "*." + ImageSequenceImporter.TGA_EXTENSION,
            };
            foreach (string ext in extensions) {
                IEnumerable<string> files = Directory.EnumerateFiles(fullSrcPath, ext, SearchOption.AllDirectories);
                foreach (string filePath in files) {
                    Uri curPathUri = new Uri(filePath.Replace("\\", "/"));
                    Uri diff = fullSrcPathUri.MakeRelativeUri(curPathUri);
                    relFilePaths.Add(diff.OriginalString);
                }
            }
            if (relFilePaths.Count <= 0) {
                EditorUtility.DisplayDialog(StreamingImageSequenceConstants.DIALOG_HEADER, @"No files in folder:: " + folder,"OK");
                return;
            }
            relFilePaths.Sort(FileNameComparer);

            //Estimate the asset name. Use the filename without numbers at the end
            string assetName =  EstimateAssetName(relFilePaths[0]);

            // set dest folder
            string rootDestFolder = Application.streamingAssetsPath;
            if (importerMode == PictureFileImporterParam.Mode.SpriteAnimation) {
                rootDestFolder = Application.dataPath;
            }

            //Set importer param
            PictureFileImporterParam importerParam = new PictureFileImporterParam {
                strAssetName = assetName,
                strSrcFolder = folder,
                RelativeFilePaths = relFilePaths,
                mode = importerMode,
                DoNotCopy = false,
                TargetAsset = targetAsset
            };


            if (fullSrcPath.StartsWith(rootDestFolder)) {
                //Import immediately if the assets are already under Unity
                importerParam.strDstFolder = importerParam.strSrcFolder;
                importerParam.DoNotCopy = true;
                ImageSequenceImporter.Import(importerParam);
            } else {
                importerParam.strDstFolder = Path.Combine(rootDestFolder, assetName).Replace("\\", "/");
                ImageSequenceImportWindow.SetParam(importerParam);
                ImageSequenceImportWindow.InitWindow();
            }
        }

//----------------------------------------------------------------------------------------------------------------------
        
        internal static void Import(PictureFileImporterParam param)
        {
            if (param.DoNotCopy)
            {
                param.strDstFolder = param.strSrcFolder.Replace("\\", "/");

            }
            else
            {

                string dstFolder = param.strDstFolder.Replace("\\", "/");
                if (param.mode == PictureFileImporterParam.Mode.StreamingAssets)
                {
                    if (dstFolder.StartsWith(Application.dataPath) && !dstFolder.StartsWith(Path.Combine(Application.dataPath, "StreamingAssets").Replace("\\", "/")))
                    {
                        Debug.LogError("Files must be located under StreamingAssets folder.");
                        return;
                    }
                }
                else
                {
                    if (dstFolder.StartsWith(Application.dataPath) && dstFolder.StartsWith(Path.Combine(Application.dataPath, "StreamingAssets").Replace("\\", "/")))
                    {
                        Debug.LogError("Files must not be located under StreamingAssets folder.");
                        return;
                    }
                }

                foreach (string relPath in param.RelativeFilePaths)
                {
                    string strAbsFilePathDst = Path.Combine(param.strDstFolder, relPath).Replace("\\", "/");
                    if (File.Exists(strAbsFilePathDst))
                    {
                        File.Delete(strAbsFilePathDst);
                    }
                    string strAbsFilePathSrc = Path.Combine(param.strSrcFolder, relPath).Replace("\\", "/");
                    Directory.CreateDirectory(Path.GetDirectoryName(strAbsFilePathDst));//make sure dir exists
                    FileUtil.CopyFileOrDirectory(strAbsFilePathSrc, strAbsFilePathDst);
                }
            }

            // create assets
            StreamingImageSequencePlayableAssetParam trackMovieContainer = new StreamingImageSequencePlayableAssetParam();
            trackMovieContainer.Pictures = new List<string>();
            foreach (string relPath in param.RelativeFilePaths)
            {
                trackMovieContainer.Pictures.Add(relPath);
            }

            //if possible, convert folder names to relative path.
            string strUnityProjectFolder = null;
            Regex regAssetFolder = new Regex("/Assets$");
            strUnityProjectFolder = Application.dataPath;
            strUnityProjectFolder = regAssetFolder.Replace(strUnityProjectFolder, "");


            if (param.strDstFolder.StartsWith(strUnityProjectFolder))
            {
                int start = strUnityProjectFolder.Length + 1;
                int end = param.strDstFolder.Length - start;
                param.strDstFolder = param.strDstFolder.Substring(start, end);
            }
            trackMovieContainer.Folder = param.strDstFolder;

            if (param.mode == PictureFileImporterParam.Mode.SpriteAnimation)
            {
                Sprite[] sprites = new Sprite[param.RelativeFilePaths.Count];
                for (int ii = 0; ii < param.RelativeFilePaths.Count; ii++)
                {
                    string strAssetPath = Path.Combine(param.strDstFolder, param.RelativeFilePaths[ii]).Replace("\\", "/");

                    AssetDatabase.ImportAsset(strAssetPath);
                    TextureImporter importer = AssetImporter.GetAtPath(strAssetPath) as TextureImporter;
                    importer.textureType = TextureImporterType.Sprite;
                    AssetDatabase.WriteImportSettingsIfDirty(strAssetPath);

                    Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(strAssetPath, typeof(Texture2D));

                    sprites[ii] = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }

                AnimationClip newClip = new AnimationClip();
                newClip.wrapMode = WrapMode.Once;
                SerializedObject serializedClip = new SerializedObject(newClip);
                SerializedProperty settings = serializedClip.FindProperty("m_AnimationClipSettings");
                while (settings.Next(true))
                {
                    if (settings.name == "m_LoopTime")
                    {
                        break;
                    }
                }

                settings.boolValue = true;
                serializedClip.ApplyModifiedProperties();
                ObjectReferenceKeyframe[] Keyframes = new ObjectReferenceKeyframe[param.RelativeFilePaths.Count];
                EditorCurveBinding curveBinding = new EditorCurveBinding();


                for (int ii = 0; ii < param.RelativeFilePaths.Count; ii++)
                {
                    Keyframes[ii] = new ObjectReferenceKeyframe();
                    Keyframes[ii].time = 0.25F * ii;
                    Keyframes[ii].value = sprites[ii];
                }
#if false
            curveBinding.type = typeof(SpriteRenderer);
            curveBinding.path = string.Empty;
            curveBinding.propertyName = "m_Sprite";
#else
                curveBinding.type = typeof(Image);
                curveBinding.path = string.Empty;
                curveBinding.propertyName = "m_Sprite";
#endif
                AnimationUtility.SetObjectReferenceCurve(newClip, curveBinding, Keyframes);
                AssetDatabase.CreateAsset(newClip, Path.Combine(param.strDstFolder, "Animation.anim").Replace("\\", "/"));

                //            var proxyAsset = ScriptableObject.CreateInstance<StreamingImageSequencePlayableAsset>(); //new StreamingImageSequencePlayableAsset(trackMovieContainer);
                //            proxyAsset.SetParam(trackMovieContainer);
                //            var strProxyPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine("Assets", param.strAssetName + "_StreamingImageSequence.playable").Replace("\\", "/"));*/
                AssetDatabase.Refresh();
            }
            else
            {
                //StreamingAsset
                StreamingImageSequencePlayableAsset proxyAsset = param.TargetAsset;
                if (null == proxyAsset) {
                    proxyAsset = ScriptableObject.CreateInstance<StreamingImageSequencePlayableAsset>(); 
                    var strProxyPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine("Assets", param.strAssetName + "_StreamingImageSequence.playable").Replace("\\", "/"));
                    AssetDatabase.CreateAsset(proxyAsset, strProxyPath);
                }

                proxyAsset.SetParam(trackMovieContainer);
                if (!param.DoNotCopy)
                {
                    AssetDatabase.Refresh();
                }

            }


        }

//---------------------------------------------------------------------------------------------------------------------

        private static int FileNameComparer(string x, string y) {
            return string.Compare(x, y, StringComparison.InvariantCultureIgnoreCase);
        }

//---------------------------------------------------------------------------------------------------------------------
        internal static string EstimateAssetName(string fullFilePath) {
            string ret = Path.GetFileNameWithoutExtension(fullFilePath.Replace("\\","/"));
            //From the filename, find the last number sequence that is not followed by a number sequence
            Regex r = new Regex(@"(\d+)(?!.*\d)", RegexOptions.IgnoreCase);
            Match m = ASSET_NAME_REGEX.Match(ret);
            if (m.Success) {
                ret = ret.Substring(0, m.Index);
            }

            //Fallback: just get the directory name. For example, if the fileName is 00000.png
            if (string.IsNullOrEmpty(ret)) {
                ret = Path.GetFileName(Path.GetDirectoryName(fullFilePath));
            }

            return ret;
        }

        private static readonly Regex ASSET_NAME_REGEX = new Regex(@"[^a-zA-Z]*(\d+)(?!.*\d)", RegexOptions.IgnoreCase); 


    }


    public class PictureFileImporterParam
    {
        public enum Mode
        {
            StreamingAssets,
            SpriteAnimation,
        }

        public string strAssetName;
        public List<string> RelativeFilePaths;
        public string strDstFolder;
        public string strSrcFolder;
        public bool DoNotCopy;
        public Mode mode;
        public StreamingImageSequencePlayableAsset TargetAsset = null;
    }
}