using System;
using JetBrains.Annotations;
using System.IO;
using UnityEditor;
using UnityEditor.StreamingImageSequence;
using UnityEditor.Timeline;
using UnityEngine.Timeline;
using System.Collections.Generic;

namespace UnityEngine.StreamingImageSequence {
    [CustomTimelineEditor(typeof(StreamingImageSequencePlayableAsset)), UsedImplicitly]
    internal class StreamingImageSequencePlayableAssetEditor : ClipEditor
    {
        private const string kNoFolderAssignedError = "No Folder assigned";
        private const string kNotStreamingAssetsFolderAssignedError = "Loading folder must be under Assets/StreamingAssets";
        private const string kFolderMissingError = "Assigned folder does not exist.";
        private const string kNoPicturesAssignedError = "No Pictures assigned";

        /// <inheritdoc/>
        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            var clipOptions = base.GetClipOptions(clip);
            StreamingImageSequencePlayableAsset asset = clip.asset as StreamingImageSequencePlayableAsset;
            if (null == asset) {
                Debug.LogError("Asset is not a StreamingImageSequencePlayableAsset: " + clip.asset);
                return clipOptions;
            }

            string folder = asset.GetFolder();
            if (string.IsNullOrEmpty(folder)) {

                UnityEditor.DefaultAsset timelineDefaultAsset = asset.GetTimelineDefaultAsset();
                if (null!=timelineDefaultAsset) {
                    if (!InitializeAssetFromDefaultAsset(clip, null, asset)) {
                        clipOptions.errorText = kNotStreamingAssetsFolderAssignedError;
                    }
                } else {
                    clipOptions.errorText = kNoFolderAssignedError;
                }
            }  else if (!Directory.Exists(folder)) {
                clipOptions.errorText = kFolderMissingError;
            } else if (asset.GetImagePaths() == null) {
                clipOptions.errorText = kNoPicturesAssignedError;
            }
            clipOptions.tooltip = folder;
            
            return clipOptions;
        }

        /// <inheritdoc/>
        public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
        {
            StreamingImageSequencePlayableAsset asset = clip.asset as StreamingImageSequencePlayableAsset;
            if (null == asset) {
                Debug.LogError("Asset is not a StreamingImageSequencePlayableAsset: " + clip.asset);
                return;
            }

            InitializeAssetFromDefaultAsset(clip, clonedFrom, asset);
        }

//----------------------------------------------------------------------------------------------------------------------

        private static bool InitializeAssetFromDefaultAsset(TimelineClip clip, TimelineClip clonedFrom, 
            StreamingImageSequencePlayableAsset asset) 
        {
            if (null == clonedFrom) { //Not cloning, which means this is created by user interaction, such as Folder D&D
                UnityEditor.DefaultAsset timelineDefaultAsset = asset.GetTimelineDefaultAsset();
                if (null == timelineDefaultAsset)
                    return false;

                string path = AssetDatabase.GetAssetPath(timelineDefaultAsset).Replace("\\","/");
                if (!path.StartsWith("Assets/StreamingAssets/")) {
                    return false;
                }
                ImageSequenceImporter.ImportPictureFiles(PictureFileImporterParam.Mode.StreamingAssets, path, asset);
            }

            //If the clip already has curves (because of cloning, etc), then we don't set anything
            if (null == clip.curves) {
                clip.duration = asset.GetImagePaths().Count * 0.125; // 8fps (standard limited animation)
                clip.displayName = Path.GetFileName(asset.GetFolder());
                clip.CreateCurves("Curves: " + clip.displayName);
            }

            asset.Setup(clip);
            asset.ValidateAnimationCurve();
            return true;
        }

        /// <inheritdoc/>
        public override void OnClipChanged(TimelineClip clip)
        {
            base.OnClipChanged(clip);
        }

//----------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region) {
            base.DrawBackground(clip, region);
            
            var rect = region.position;
            if (rect.width <= 0)
                return;

            StreamingImageSequencePlayableAsset curAsset = clip.asset as StreamingImageSequencePlayableAsset;
            if (null == curAsset)
                return;

            Rect quantizedRect = new Rect(Mathf.Ceil(rect.x), Mathf.Ceil(rect.y), Mathf.Ceil(rect.width), Mathf.Ceil(rect.height));

            if (QualitySettings.activeColorSpace != m_colorSpace) {
                m_colorSpace = QualitySettings.activeColorSpace;
                m_persistentPreviews.Clear();
            }

            if (!m_persistentPreviews.TryGetValue(clip, out StreamingImageSequencePreview preview)) {
                preview = m_persistentPreviews[clip] = new StreamingImageSequencePreview(curAsset);
            }

            if (Event.current.type == EventType.Repaint) {
                preview.SetLocalTime(region.startTime, region.endTime);
                preview.Render(quantizedRect);
            }
        }

//----------------------------------------------------------------------------------------------------------------------
        readonly Dictionary<TimelineClip, StreamingImageSequencePreview> m_persistentPreviews 
            = new Dictionary<TimelineClip, StreamingImageSequencePreview>();

        ColorSpace m_colorSpace = ColorSpace.Uninitialized;

    }
}
