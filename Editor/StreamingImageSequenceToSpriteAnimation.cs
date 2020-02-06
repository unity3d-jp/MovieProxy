using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEditor.Animations;
using UnityEngine.Assertions;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine.StreamingImageSequence;

namespace UnityEditor.StreamingImageSequence {

    public class StreamingImageSequenceToSpriteAnimation  {

        static List<Object> orgList;
        static TimelineAsset newAsset;
        static PlayableDirector newPlayableDirector; 

        static internal void ConvertIt(GameObject orgGo)
        {
            orgList = new List<Object>();

            var orgPlayableDirector = orgGo.GetComponent<PlayableDirector>() as PlayableDirector;
            Assert.IsTrue(orgPlayableDirector != null, "PlayableDirector component is not attached to this GameObject." + orgGo);
            if (orgPlayableDirector == null)
            {
                return;
            }
            var orgAsset = orgPlayableDirector.playableAsset;
            if (orgAsset == null)
            {
                return;
            }
            if (orgAsset.GetType() != typeof(TimelineAsset))
            {
                return;
            }
            Assert.IsTrue(AssetDatabase.Contains(orgAsset));
            orgList.Add(orgAsset);



            // store gameobjects

            var orgBindings = orgAsset.outputs;
            List<Object> goList = new List<Object>();

            foreach (PlayableBinding binding in orgBindings)
            {
                Object tmpObject = orgPlayableDirector.GetGenericBinding(binding.sourceObject) as Object;
                goList.Add(tmpObject);
            }


            // create new asset.
            var path = AssetDatabase.GetAssetPath(orgAsset);
            var uniquePath = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CopyAsset(path, uniquePath);
            AssetDatabase.Refresh();
            newAsset = AssetDatabase.LoadAssetAtPath(uniquePath, typeof(Object)) as TimelineAsset;

            if (newAsset == null)
            {
                return;
            }

            // improtant: clear all bindings to avoid leaving garbage.

            orgPlayableDirector.playableAsset = newAsset;
            orgBindings = newAsset.outputs;

            foreach (PlayableBinding binding in orgBindings)
            {
                var source = binding.sourceObject;
                orgPlayableDirector.SetGenericBinding(source, null);

            }

            orgPlayableDirector.playableAsset = orgAsset;
            // done cleaning.


            var cloneGo = GameObject.Instantiate(orgGo);
            var parent = orgGo.transform.parent;
            cloneGo.transform.SetParent(parent);
            cloneGo.transform.SetSiblingIndex(parent == null ? 0 : parent.transform.childCount - 1);
            cloneGo.transform.localPosition = orgGo.transform.localPosition;
            cloneGo.transform.localRotation = orgGo.transform.localRotation;
            cloneGo.transform.localScale = orgGo.transform.localScale;
            cloneGo.name = orgGo.name;
            cloneGo.name = GameObjectUtility.GetUniqueNameForSibling(parent, cloneGo.name);

            newPlayableDirector = cloneGo.GetComponent<PlayableDirector>() as PlayableDirector;
            if (newPlayableDirector == null)
            {
                return;
            }
            newPlayableDirector.playableAsset = newAsset;

            var newBindings = newAsset.outputs;

            int index = 0;
            foreach (PlayableBinding binding in newBindings)
            {
                newPlayableDirector.SetGenericBinding(binding.sourceObject, goList[index++]);
            }

            // start to modify tracks in the new playable asset.
            var rootTracks = UpdateManager.GetTrackList(newAsset);
            ProcessTracks(null, rootTracks,goList,0 );

            newPlayableDirector = null;
            newAsset = null;
            Debug.Log("Done duplicate " + orgGo );

        }

        static private void ProcessTracks(TrackAsset parentTrack, List<TrackAsset> tracks, List<Object> goList, int currentIndex)
        {
            List<TrackAsset> newList = new List<TrackAsset>();

            foreach (var track in tracks)
            {
                var trackType = track.GetType();
                if (trackType == typeof(GroupTrack))
                {
                    var childTracks = UpdateManager.GetTrackList(track as GroupTrack);
                    ProcessTracks(track,childTracks, goList, currentIndex);
                }
                else if (trackType == typeof(StreamingImageSequenceTrack))
                {
                    newList.Add(track);
                }

            }

            List<TrackAsset> toBeDelated = new List<TrackAsset>();

            foreach ( var track in newList )
            {
                var tmp = newPlayableDirector.GetGenericBinding(track) as StreamingImageSequenceNativeRenderer;
                GameObject go = null;
                if ( tmp != null )
                {
                    go = tmp.gameObject;
                }
                if ( go == null )
                {
                    continue;
                }

                toBeDelated.Add(track);

                var srcClips = track.GetClips();
                int counter = 0;
                foreach (var srcClip in srcClips)
                {

                    var activationTrack = newAsset.CreateTrack<ActivationTrack>(parentTrack, track.name);
                    if (counter == 0)
                    {
                        newPlayableDirector.SetGenericBinding(activationTrack, go);
                    }
                    else
                    {
                        GameObject goDup = DuplicateGameObject(go);
                        newPlayableDirector.SetGenericBinding(activationTrack, goDup);
                        go = goDup;
                    }
                    var dstClip = activationTrack.CreateDefaultClip();
                    dstClip.start = srcClip.start;
                    dstClip.duration = srcClip.duration;

                    var animator = go.GetComponent<Animator>();
                    if ( animator == null )
                    {
                        animator = go.AddComponent<Animator>();
                    }
                    var movieProxyAsset = srcClip.asset as StreamingImageSequencePlayableAsset;


                    int imageCount = movieProxyAsset.GetImagePaths().Count;
                    Sprite[] sprites = new Sprite[imageCount];

                    string strSrcFolder = Path.Combine(UpdateManager.GetProjectFolder(), movieProxyAsset.GetFolder()).Replace("\\", "/");
                    string strDistFolder = GetDistinationFolder(movieProxyAsset.GetImagePath(0));
                    for (int ii = 0; ii < imageCount; ii++)
                    {
                        string strAssetPath =  Path.Combine(strDistFolder, movieProxyAsset.GetImagePath(ii)).Replace("\\", "/");
                        string strSrcPath = Path.Combine( strSrcFolder, movieProxyAsset.GetImagePath(ii)).Replace("\\", "/");
                        if (!File.Exists(strAssetPath))
                        {
                            FileUtil.CopyFileOrDirectory(strSrcPath, strAssetPath);
                        }
                    }

                    for (int ii = 0; ii < imageCount; ii++)
                    {
                        string strAssetPath = Path.Combine(strDistFolder, movieProxyAsset.GetImagePath(ii)).Replace("\\", "/");
                        strAssetPath = UpdateManager.ToRelativePath(strAssetPath);

                        AssetDatabase.ImportAsset(strAssetPath);

                        AssetImporter tmpImporter = AssetImporter.GetAtPath(strAssetPath);
                        TextureImporter importer = tmpImporter as TextureImporter;

                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.anisoLevel = 16;
                        AssetDatabase.WriteImportSettingsIfDirty(strAssetPath);



                    }

                    for (int ii = 0; ii < imageCount; ii++)
                    {
                        string strAssetPath = Path.Combine(strDistFolder, movieProxyAsset.GetImagePath(ii)).Replace("\\", "/");
                        strAssetPath = UpdateManager.ToRelativePath(strAssetPath);
                        AssetDatabase.ImportAsset(strAssetPath);

                        Sprite sp = (Sprite)AssetDatabase.LoadAssetAtPath(strAssetPath, typeof(Sprite));
                        sprites[ii] = sp;

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
                    ObjectReferenceKeyframe[] Keyframes = new ObjectReferenceKeyframe[imageCount];
                    EditorCurveBinding curveBinding = new EditorCurveBinding();
                    if (go.GetComponent<Image>() != null)
                    {
                        curveBinding.type = typeof(Image);
                        curveBinding.path = string.Empty;
                        curveBinding.propertyName = "m_Sprite";
                    }
                    else if (go.GetComponent<SpriteRenderer>() != null )
                    {
                        curveBinding.type = typeof(SpriteRenderer);
                        curveBinding.path = string.Empty;
                        curveBinding.propertyName = "m_Sprite";
                    }

                    float delta = (float)srcClip.duration / (float)(imageCount - 1);
                    
                    for (int ii = 0; ii < imageCount; ii++)
                    {
                        Keyframes[ii] = new ObjectReferenceKeyframe();
                        Keyframes[ii].time = delta * ii;
                        Keyframes[ii].value = sprites[ii];
                    }
                    counter++;

                    AnimationUtility.SetObjectReferenceCurve(newClip, curveBinding, Keyframes);
                    strDistFolder = UpdateManager.ToRelativePath(strDistFolder);
                    var uniquePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(strDistFolder, "Animation.anim").Replace("\\", "/"));
                    AssetDatabase.CreateAsset(newClip, uniquePath );

                    uniquePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(strDistFolder, "Animator.controller").Replace("\\", "/"));

                    var controller = AnimatorController.CreateAnimatorControllerAtPath(uniquePath);
                    controller.AddParameter("Start", AnimatorControllerParameterType.Trigger);
                    var rootStateMachine = controller.layers[0].stateMachine;
                    var stateA1 = rootStateMachine.AddState("stateA1");
                    //var resetTransition = rootStateMachine.AddAnyStateTransition(stateA1);
                    controller.SetStateEffectiveMotion(stateA1, newClip);

                    animator.runtimeAnimatorController = controller;
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            } // foreach.

            foreach( var track in toBeDelated)
            {
                newAsset.DeleteTrack(track);
            }
            AssetDatabase.Refresh();
        }

       
        static string GetDistinationFolder(string strPath)
        {
            var strFileneWithoutExtention = Path.GetFileNameWithoutExtension(strPath);

            var regNumbers = new Regex(@"\d+$");
            var matches = regNumbers.Matches(strFileneWithoutExtention);
            Assert.IsTrue(matches.Count > 0);

            Match match = null;
            foreach (Match m in matches)
            {
                match = m;
            }

            Assert.IsTrue(match != null);

            int periodIndex = strFileneWithoutExtention.Length;
            int digits = match.Value.Length;
            var strBaseName = strFileneWithoutExtention.Substring(0, match.Index);

            var strDistFolder = Application.dataPath;

            if (!Directory.Exists(strDistFolder))
            {
                Directory.CreateDirectory(strDistFolder);
            }

            var strAssetName = strBaseName;
            if (strAssetName.EndsWith("_") || strAssetName.EndsWith("-"))
            {
                strAssetName = strAssetName.Substring(0, strAssetName.Length - 1);
            }

            strDistFolder = Path.Combine(strDistFolder, "Conv").Replace("\\", "/");
            if (!Directory.Exists(strDistFolder))
            {
                Directory.CreateDirectory(strDistFolder);
            }

            strDistFolder = Path.Combine(strDistFolder, strAssetName).Replace("\\", "/");
            if (!Directory.Exists(strDistFolder))
            {
                Directory.CreateDirectory(strDistFolder);
            }
            return strDistFolder;
        }
        static GameObject DuplicateGameObject(GameObject go)
        {
            Transform parent = go.transform.parent;
            GameObject retGo = Object.Instantiate(go, parent);
            return retGo;
        }
    }

}