using System.IO;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.StreamingImageSequence;
using UnityEngine.Timeline;
using UnityEngine.UI;
using System.Collections.Generic;

namespace UnityEditor.StreamingImageSequence {


[ScriptedImporter(1, "jstimeline")]
public class JstimelineImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx) {
        //Ignore test assets
        if (ctx.assetPath.StartsWith("Packages/com.unity.streaming-image-sequence/Tests"))
            return;
        
        ImportTimeline(ctx.assetPath);
    }


//---------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Import a timeline file exported from DCC tools into the scene in the Timeline object
    /// </summary>
    /// <param name="jsTimelinePath">The path of the file</param>
    public static void ImportTimeline(string jsTimelinePath) {

        // prepare asset name, paths, etc
        string assetName = Path.GetFileNameWithoutExtension(jsTimelinePath);
        string timelineFolder = Path.GetDirectoryName(jsTimelinePath);
        if (string.IsNullOrEmpty(timelineFolder)) {
            Debug.LogError("Can't get directory name for: " + jsTimelinePath);
            return;
        }
        timelineFolder = Path.Combine(timelineFolder,assetName).Replace("\\","/");
        //Check if we are exporting from external asset
        if (!timelineFolder.StartsWith("Assets/")) {
            timelineFolder = Path.Combine("Assets", assetName);
        }
        
        Directory.CreateDirectory(timelineFolder);
        string strJson = File.ReadAllText(jsTimelinePath);
        TimelineParam container = JsonUtility.FromJson<TimelineParam>(strJson);
        string assetFolder = container.assetFolder;
        if (string.IsNullOrEmpty(assetFolder)) {
            assetFolder = Path.GetDirectoryName(jsTimelinePath);
        }

        //delete existing objects in the scene that is pointing to the Director
        string timelinePath = Path.Combine(timelineFolder, assetName + "_Timeline.playable").Replace("\\","/");
        PlayableDirector director = RemovePlayableFromDirectorsInScene(timelinePath);
        if (null == director) {
            GameObject directorGo = new GameObject(assetName);
            director = directorGo.AddComponent<PlayableDirector>();
        }

        //Create timeline asset
        TimelineAsset asset = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetEditorUtility.OverwriteAsset(asset, timelinePath);

        director.playableAsset = asset;
        string strHome = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);

        int numTracks = container.Tracks.Length;
        for (int index = numTracks - 1; index >= 0; index--)
        {
            var track = container.Tracks[index];
            string strFootagePath = track.Footage;
            // remove '~' if necessary
            if (strFootagePath.StartsWith("~"))
            {
                strFootagePath = strHome + strFootagePath.Substring(1);
            }
            if (!Path.IsPathRooted(strFootagePath))
            {
                strFootagePath = Path.Combine(assetFolder, strFootagePath);
            }
            string strFootageName = Path.GetFileNameWithoutExtension(strFootagePath);
            string strJsonFootage = File.ReadAllText(strFootagePath);
            StreamingImageSequencePlayableAssetParam trackMovieContainer = JsonUtility.FromJson<StreamingImageSequencePlayableAssetParam>(strJsonFootage);

            int numImages = trackMovieContainer.Pictures.Count;
            if (numImages > 0) {

                List<string> originalImagePaths = new List<string>(trackMovieContainer.Pictures);

                for (int xx = 0; xx < numImages; ++xx) {
                    string fileName = trackMovieContainer.Pictures[xx];
                    // replace '~' with the path to home (for Linux environment
                    if (fileName.StartsWith("~")) {
                        fileName = strHome + fileName.Substring(1);
                    }
                    trackMovieContainer.Pictures[xx] = Path.GetFileName(fileName);
                }
                
                string destFolder = Application.streamingAssetsPath;
                destFolder = Path.Combine(destFolder, strFootageName).Replace("\\", "/");
                Directory.CreateDirectory(destFolder); //make sure the directory exists
                trackMovieContainer.Folder = AssetEditorUtility.NormalizeAssetPath(destFolder);

                for (int i=0;i<numImages;++i) {
                    string destFilePath = Path.Combine(destFolder, trackMovieContainer.Pictures[i]);
                    if (File.Exists(destFilePath)) {
                        File.Delete(destFilePath);
                    }
                    
                    string srcFilePath = Path.GetFullPath(Path.Combine(assetFolder, originalImagePaths[i])).Replace("\\", "/");
                    FileUtil.CopyFileOrDirectory(srcFilePath, destFilePath);
                }

            }


            StreamingImageSequencePlayableAsset sisAsset = ScriptableObject.CreateInstance<StreamingImageSequencePlayableAsset>();
            sisAsset.SetParam(trackMovieContainer);

            string playableAssetPath = Path.Combine(timelineFolder, strFootageName + "_StreamingImageSequence.playable");
            AssetEditorUtility.OverwriteAsset(sisAsset, playableAssetPath);

            StreamingImageSequenceTrack movieTrack = asset.CreateTrack<StreamingImageSequenceTrack>(null, strFootageName);
            TimelineClip clip = movieTrack.CreateDefaultClip();
            clip.asset = sisAsset;
            clip.start = track.Start;
            clip.duration = track.Duration;
            sisAsset.Setup(clip);
            sisAsset.ValidateAnimationCurve();


            if (Object.FindObjectOfType(typeof(UnityEngine.EventSystems.EventSystem)) == null)
            {
                var es = new GameObject();
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                es.name = "EventSystem";
            }
            GameObject canvasObj = null;
            Canvas canvas = Object.FindObjectOfType(typeof(Canvas)) as Canvas;
            if (canvas != null) {
                canvasObj = canvas.gameObject;
            } else {
                canvasObj = new GameObject();
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                canvasObj.name = "Canvas";

            }

            Transform directorT = director.gameObject.transform;
            directorT.SetParent(canvasObj.transform);
            directorT.localPosition = new Vector3(0.0f, 0.0f, 0.0f);

            GameObject imageGo = null;
            Transform imageT = directorT.Find(strFootageName);
            if (null == imageT) {
                imageGo = new GameObject(strFootageName);
                imageT = imageGo.transform;
            } else {
                imageGo = imageT.gameObject;
            }

            Image image = imageGo.GetOrAddComponent<Image>();
            StreamingImageSequenceNativeRenderer renderer = imageGo.GetOrAddComponent<StreamingImageSequenceNativeRenderer>();

            RectTransform rectTransform = imageGo.GetComponent<RectTransform>();
            rectTransform.SetParent(directorT);
            rectTransform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            rectTransform.sizeDelta = new Vector2(trackMovieContainer.Resolution.Width,
                                                  trackMovieContainer.Resolution.Height);

            director.SetGenericBinding(movieTrack, renderer);
            EditorUtility.SetDirty(director);
        }

        //cause crash if this is called inside of OnImportAsset()
        UnityEditor.EditorApplication.delayCall += () => {
            AssetDatabase.Refresh();
            if (null != director) {
                Selection.activeGameObject = director.gameObject;
            }
        };
    }

//---------------------------------------------------------------------------------------------------------------------
    static PlayableDirector RemovePlayableFromDirectorsInScene(string timelinePath) {
        PlayableDirector[] directors = Object.FindObjectsOfType<PlayableDirector>();
        PlayableDirector ret = null;
        foreach (PlayableDirector director in directors) {
            string playableAssetPath = AssetDatabase.GetAssetPath(director.playableAsset);
            if (playableAssetPath != timelinePath) {
                continue;
            }

            director.playableAsset = null;
            ret = director;
        }

        return ret;
    }


//---------------------------------------------------------------------------------------------------------------------
//---------------------------------------------------------------------------------------------------------------------


    [System.Serializable]
    public class TimelineParam
    {
        public float Version;
        public string assetFolder;
        public TrackParam[] Tracks;
    }

    [System.Serializable]
    public class TrackParam
    {
        public float[] Position;
        public float Start;
        public float Duration;
        public string Footage;
    }
}


} //end namespace

