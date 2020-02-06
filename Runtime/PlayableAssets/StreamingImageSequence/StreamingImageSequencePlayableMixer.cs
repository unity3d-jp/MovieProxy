using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Playables;
using System.Text.RegularExpressions;
using UnityEngine.Assertions;
using System.Threading;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using UnityEngine.Timeline;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.StreamingImageSequence
{

    // A behaviour that is attached to a playable

    public class StreamingImageSequencePlayableMixer : PlayableBehaviour
    {

        internal PlayableDirector m_PlayableDirector;
        internal IEnumerable<TimelineClip> m_clips;
        internal TrackAsset m_track;
        private double m_loadStartOffsetTime = -1.0;
#if UNITY_EDITOR
        EditorWindow m_gameView;
#endif
        private int[] m_nextInadvanceLoadingFrameArray;
        public StreamingImageSequencePlayableMixer()
        {

#if UNITY_EDITOR
            System.Reflection.Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
            Type type = assembly.GetType("UnityEditor.GameView");
            m_gameView = EditorWindow.GetWindow(type);

#endif
            m_loadStartOffsetTime = -1.0;
        }

        public override void OnPlayableCreate(Playable playable)
        {
            UpdateManager.m_ResetDelegate += Reset;
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            UpdateManager.m_ResetDelegate -= Reset;
        }


        void Reset()
        {
            m_loadStartOffsetTime = -1.0;
            m_boundGameObject   = null;
            m_spriteRenderer    = null;
            m_image             = null;
            m_meshRenderer      = null;
        }

//---------------------------------------------------------------------------------------------------------------------
        public override void PrepareFrame(Playable playable, FrameData info) {
            base.PrepareFrame(playable, info);
            if (null == m_boundGameObject)
                return;

            m_boundGameObject.SetActive(false); //Always hide first, and show it later 
        }

//---------------------------------------------------------------------------------------------------------------------
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            int inputCount = playable.GetInputCount<Playable>();
            if (inputCount == 0) {
                return; // it doesn't work as mixer.
            }

            if (m_nextInadvanceLoadingFrameArray == null)
            {
                m_nextInadvanceLoadingFrameArray = new int[inputCount];
                for ( int ii = 0;ii< inputCount;ii++)
                {
                    m_nextInadvanceLoadingFrameArray[ii] = 0;
                }
            }

            if (!TryBindGameObjectFromFrame(playerData)) {
                //Debug.LogError("Can't bind GameObject for track: " + m_track.name);
                return;
            }


            double directorTime = m_PlayableDirector.time;
            TimelineClip activeClip = null;

            int i = 0;
            foreach (TimelineClip clip in m_clips) {

                StreamingImageSequencePlayableAsset asset = clip.asset as StreamingImageSequencePlayableAsset;
                if (null == asset)
                    continue;

                IList<string> imagePaths = asset.GetImagePaths();
                if (null == imagePaths)
                    continue;

                int count = imagePaths.Count;
                double clipDuration = clip.duration;
                double startTime = clip.start;
                double endTime = clip.end;

                if ( m_loadStartOffsetTime < 0.0) {
                    m_loadStartOffsetTime = 1.0f + count * 0.1f;

                }

                //Load clips that might still be inactive, in advance
				if ( directorTime>= startTime - m_loadStartOffsetTime && directorTime < endTime) {
                    ProcessInAdvanceLoading(directorTime, clip, i );
                }

                if (directorTime >= startTime && directorTime < endTime) {
                    activeClip = clip;
                    int index = asset.GlobalTimeToImageIndex(directorTime);

                    bool texReady = asset.RequestLoadImage(index, false);
                    if (texReady) {
                        UpdateRendererTexture(asset);

#if UNITY_EDITOR
                        if (!EditorApplication.isPlaying) {
                            m_gameView.Repaint();
                        }
#endif
                    }

                } 

                ++i;
            }

            //Hide/Show
            if (null != activeClip) {
                m_boundGameObject.SetActive(true);
            }
        }

        private void ProcessInAdvanceLoading(double time, TimelineClip clip, int index)
        {
            var asset = clip.asset as StreamingImageSequencePlayableAsset;
            if (null == asset)
                return;

            int count = asset.GetImagePaths().Count;

            if (m_nextInadvanceLoadingFrameArray[index] < count)
            {
                for (int check = 0; check < 4; check++)
                {

                    if (m_nextInadvanceLoadingFrameArray[index] >= 0 && m_nextInadvanceLoadingFrameArray[index] <= count)
                    {
                        if (!asset.IsLoadRequested(m_nextInadvanceLoadingFrameArray[index]))
                        {
                            ReadResult result = new ReadResult();
                            asset.LoadRequest(m_nextInadvanceLoadingFrameArray[index], false, out result);
                        }
                    }
                    m_nextInadvanceLoadingFrameArray[index]++;
                    if (m_nextInadvanceLoadingFrameArray[index] >= count)
                    {
                        break;
                    }

                }
            }

        }

//---------------------------------------------------------------------------------------------------------------------
        public bool BindGameObject(GameObject go) {
            m_boundGameObject = go;
            bool ret = InitRenderers();
            if (!ret) {
                Reset();
                return false;
            }

            return true;
        }


//---------------------------------------------------------------------------------------------------------------------
        private bool TryBindGameObjectFromFrame(object playerData) {
            if (null != m_boundGameObject)
                return true;

            //There might be two sources: playerData, and the Object bound to the track
            StreamingImageSequenceNativeRenderer renderer = playerData as StreamingImageSequenceNativeRenderer;
            if (null != renderer) {
                m_boundGameObject = renderer.gameObject;
            } else  {
                Object binding = m_PlayableDirector.GetGenericBinding(m_track);
                GameObject go = binding as GameObject;
                m_boundGameObject = go;
            }

            if (null == m_boundGameObject) {
                return false;
            }

            bool ret = InitRenderers();
            if (!ret) {
                Reset();
                return false;
            }

            return true;
        }

//---------------------------------------------------------------------------------------------------------------------
        private bool InitRenderers() {
            m_spriteRenderer= m_boundGameObject.GetComponent<SpriteRenderer>();
            m_meshRenderer  = m_boundGameObject.GetComponent<MeshRenderer>();
            m_image         = m_boundGameObject.GetComponent<Image>();
            return (null!= m_meshRenderer || null!= m_image || null!=m_spriteRenderer);
        }

//---------------------------------------------------------------------------------------------------------------------

        void UpdateRendererTexture(StreamingImageSequencePlayableAsset asset) {
            Texture2D tex = asset.GetTexture();
            GameObject go = m_boundGameObject;
            if (null!=m_spriteRenderer ) {
                Sprite sprite = m_spriteRenderer.sprite;
                if (sprite.texture != tex) {
                    m_spriteRenderer.sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f, 2, SpriteMeshType.FullRect);
                }
            } else if (null!=m_meshRenderer) {
                Material mat = m_meshRenderer.sharedMaterial;
                mat.mainTexture = tex; 
            } else if (null!= m_image) {
                Sprite sprite = m_image.sprite;
                if (null==sprite || sprite.texture != tex) {
                    m_image.sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f, 1, SpriteMeshType.FullRect);
                }
            }

        }

//---------------------------------------------------------------------------------------------------------------------
        
        /*
        static public void ResetAllTexturePtr()
        {
            StreamingImageSequencePlugin.ResetAllLoadedTexture();
        }
        */

        private GameObject      m_boundGameObject;
        private SpriteRenderer  m_spriteRenderer = null;
        private MeshRenderer    m_meshRenderer = null;
        private Image           m_image = null;

    }



}