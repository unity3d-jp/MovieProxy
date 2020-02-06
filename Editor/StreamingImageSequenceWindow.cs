using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEngine.Assertions;
using System;
using UnityEngine.StreamingImageSequence;

namespace UnityEditor.StreamingImageSequence
{ 
    public class StreamingImageSequenceWindow : EditorWindow
    {
        private GUIStyle m_windowStyle = null;
        private GUIStyle m_boxStyle = null;
        private GUIStyle m_clipStyle = null;
        private GUIStyle m_headerBgStyle = null;
        private GUIStyle m_contentBgStyle = null;
        private GUIStyle m_trackGroupHeaderStyle = null;
        private GUIStyle m_trackGroupBodyStyle = null;
    
        private GUIStyle m_trackHeaderStyle = null;
        private GUIStyle m_trackBGStyle = null;

        private GUIStyle m_ActivationTrackLineStyle = null;
        private GUIStyle m_AnimationTrackLineStyle = null;
        private GUIStyle m_AudioTrackLineStyle = null;
        private GUIStyle m_ControlTrackLineStyle = null;
        private GUIStyle m_PlayableTrackLineStyle = null;
        private GUIStyle m_MovieProxyTrackLineStyle = null;

    //    private GUIStyle m_IndicatorStyle = null;

        static float kLineWidth = 3.0f;
        static float kHeaderWidth = 306.0f;
        static float kHeaderTrackHeight = 16.0f;
        static float kLeftMerginOfHeader = 16.0f;
        static float kHederWidthGroupTrack = kHeaderWidth - kLeftMerginOfHeader;
        static float m_fIndent = 0.0f;
        static float m_fWholeHeight = 0.0f;
        static float m_fRightAreaWidth = 0.0f;

        static PlayableDirector m_currentDirector;
        static Dictionary<StreamingImageSequencePlayableAsset, BGJobCacheParam> m_MovieProxyPlayableAssetToColorArray = new Dictionary<StreamingImageSequencePlayableAsset, BGJobCacheParam>();
        public Vector2 m_scrollPosition = Vector2.zero;

 
        /*
        [MenuItem("Window/Streaming Image Sequence")]

        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(MovieProxyWindow));
        }
        */



        void OnGUI()
        {
#if false
            {
                GUILayout.Label("Editor window with Popup example", EditorStyles.boldLabel);
                buttonRect = new Rect(100, 100, 100, 200);
              if (GUILayout.Button("Popup Options", GUILayout.Width(200)))
                {
                    PopupWindow.Show(buttonRect, new PopupExample());
                }
               // if (Event.current.type == EventType.Repaint) buttonRect = GUILayoutUtility.GetLastRect();
            }
            return;
#endif
            m_currentDirector = UpdateManager.GetCurrentDirector();
            if (m_currentDirector == null)
            {
                return;
            }

            //ShowOverwrapWindows();
            List<TrackAsset> trackList = UpdateManager.GetTrackList(m_currentDirector);
            if (trackList == null)
            {
                return;
            }

            float nextStartY = 0.0f;

            Color backColor = GUI.backgroundColor;

            InitStyles();
        
            float kTopMerginHeight = 16.0f;
            Rect tTopLeftHeaderRect = new Rect(0, nextStartY, kHeaderWidth, kTopMerginHeight);
            GUI.Box(tTopLeftHeaderRect, GUIContent.none, m_trackBGStyle);

            m_fRightAreaWidth = Screen.width - (kHeaderWidth + kLineWidth);
            Rect tTopRightBodyRect = new Rect(kHeaderWidth + kLineWidth, nextStartY, m_fRightAreaWidth, kTopMerginHeight);
            GUI.Box(tTopRightBodyRect, GUIContent.none, m_trackBGStyle);

            //        var height = Screen.height < m_fWholeHeight ? Screen.height : m_fWholeHeight;

            var height = Screen.height; // m_fWholeHeight; // Screen.height; // - kTopMerginHeight;

            m_scrollPosition = GUI.BeginScrollView(
                new Rect(0, kTopMerginHeight, Screen.width, Screen.height - 24.0f - kTopMerginHeight),
                m_scrollPosition,
                new Rect(0, 0, Screen.width - 16.0f, m_fWholeHeight), // - 4.0f), //Screen.height),
                false,
                false);



            // ------------------------------------------------------
            // Draw leftside of the background 
            // ------------------------------------------------------


            Rect tLeftHeaderRect = new Rect(0, nextStartY, kHeaderWidth, Screen.height);
            GUI.Box(tLeftHeaderRect, GUIContent.none, m_headerBgStyle);

            // ------------------------------------------------------
            // Draw rightside of the background 
            // ------------------------------------------------------
            Rect tRightBodyRect = new Rect(kHeaderWidth + kLineWidth, nextStartY, m_fRightAreaWidth, Screen.height);
            GUI.Box(tRightBodyRect, GUIContent.none, m_contentBgStyle);

            nextStartY = DrawTracks(trackList, nextStartY);
            m_fWholeHeight = nextStartY;

           DrawIndicator();    

            GUI.backgroundColor = backColor;
            GUI.EndScrollView();

 //           ImageLoadBGTask.m_sUpdated = false;


        }

 

        public void Update()
        {
            Repaint();
        }

        private float DrawTracks(List<TrackAsset> trackList, float nextStartY)
        {
            foreach (var track in trackList)
            {
                if (track.GetType() == typeof(GroupTrack))
                {
                    // Draw TrackGroupLeftSide
                    nextStartY = DrawTrackGroup(nextStartY, track as GroupTrack);
                }
                else if (track.GetType() == typeof(ActivationTrack))
                {
                    // Activation Track 
                    nextStartY = DrawTrack(nextStartY, m_ActivationTrackLineStyle, track);

                }
                else if (track.GetType() == typeof(AnimationTrack))
                {
                    // Animation Track 
                    nextStartY = DrawTrack(nextStartY, m_AnimationTrackLineStyle, track);

                }
                else if (track.GetType() == typeof(AudioTrack))
                {
                    // Audio Track
                    nextStartY = DrawTrack(nextStartY, m_AudioTrackLineStyle, track);
                }
                else if (track.GetType() == typeof(ControlTrack))
                {

                    // Control Track
                    nextStartY = DrawTrack(nextStartY, m_ControlTrackLineStyle, track);
                }
                else if (track.GetType() == typeof(PlayableTrack))
                {
                    // Playable Track
                    nextStartY = DrawTrack(nextStartY, m_PlayableTrackLineStyle, track);
                }
                else if (track.GetType() == typeof(StreamingImageSequenceTrack))
                {
                    // MovieProxy Track
                    nextStartY = DrawTrack(nextStartY, m_MovieProxyTrackLineStyle, track);
                }
            }
            return nextStartY;
        }



        private float DrawTrackGroup(float nextStartY, GroupTrack trackGroup)
        {
 
            List<TrackAsset> list = UpdateManager.GetTrackList(trackGroup);

            GUIStyle trackGroupStyleLeft  = m_trackGroupHeaderStyle;
            GUIStyle trackGroupStyleRight = m_trackGroupBodyStyle;
            if (list.Count >= 1)
            {
                var wholeHeight = (kHeaderTrackHeight + 2) * (list.Count + 1) + 4;
                Rect tGroupTracktHeaderRect = new Rect(kLeftMerginOfHeader, nextStartY, kHederWidthGroupTrack, wholeHeight);
                GUI.Box(tGroupTracktHeaderRect, GUIContent.none, trackGroupStyleLeft);

                Rect tRightBodyRect = new Rect(kHeaderWidth + kLineWidth, nextStartY, m_fRightAreaWidth, wholeHeight);
                GUI.Box(tRightBodyRect, GUIContent.none, trackGroupStyleRight);
                nextStartY += kHeaderTrackHeight + 4;
                m_fIndent += 16.0f;
                DrawTracks(list, nextStartY);
                m_fIndent -= 16.0f;
                nextStartY += wholeHeight - (kHeaderTrackHeight + 4) + 1;
            }
            else
            {
                Rect tGroupTracktHeaderRect = new Rect(kLeftMerginOfHeader, nextStartY, kHederWidthGroupTrack, kHeaderTrackHeight + 4);
                GUI.Box(tGroupTracktHeaderRect, GUIContent.none, trackGroupStyleLeft);

                Rect tRightBodyRect = new Rect(kHeaderWidth + kLineWidth, nextStartY, m_fRightAreaWidth, kHeaderTrackHeight + 4);
                GUI.Box(tRightBodyRect, GUIContent.none, trackGroupStyleRight);
                nextStartY += kHeaderTrackHeight + 4 + 1;
             
            }

            return nextStartY;
        }

        private float DrawTrack(float nextStartY, GUIStyle trackLineStyle, TrackAsset track)
        {
            float kHeaderLineWidth = 3.0f;

            Rect tRect = new Rect(kLeftMerginOfHeader + m_fIndent, nextStartY, kHederWidthGroupTrack- m_fIndent - 1.0f, kHeaderTrackHeight);
            GUI.Box(tRect, GUIContent.none, m_trackHeaderStyle);
            tRect.width = kHeaderLineWidth;
            GUI.Box(tRect, GUIContent.none, trackLineStyle);


            Rect tRightBodyRect = new Rect(kHeaderWidth + kLineWidth, nextStartY, Screen.width - (kHeaderWidth + kLineWidth), kHeaderTrackHeight);
            GUI.Box(tRightBodyRect, GUIContent.none, m_trackBGStyle);
        
            foreach (var clip in track.GetClips())
            {
                DrawClip(nextStartY, trackLineStyle, clip);
            }
            nextStartY += kHeaderTrackHeight + 2;
            return nextStartY;
        }

        private void DrawIndicator()
        {
 
            float wholeWidth = m_fRightAreaWidth - 16.0f;
            double duration = m_currentDirector.duration;
            double time = m_currentDirector.time;
            float point = (float)(time / duration);
            point *= wholeWidth;
            Color colIndicator = new Color(0.7f, 0.7f, 0.7f, 1.0f);
            EditorGUI.DrawRect(
                new Rect(kHeaderWidth + kLineWidth + 8.0f + point, 0.0f, 1.0f, Screen.height),
                colIndicator);

        }
        private void DrawClip(float nextStartY, GUIStyle trackLineStyle, TimelineClip clip)
        {
 
            float wholeWidth = m_fRightAreaWidth - 16.0f;
            double start = clip.start;
            double end = clip.end;
            double duration = m_currentDirector.duration;

            start = start / duration;
            end = end / duration;

            double startPoint = wholeWidth * start;
            double endPoint = wholeWidth * end;

            float fRectStartX = kHeaderWidth + kLineWidth + 8.0f + (float)startPoint;
            float fRectWidth = (float)(endPoint - startPoint);
            Rect tRightBodyRect = new Rect(fRectStartX, nextStartY, fRectWidth, kHeaderTrackHeight);
            GUI.Box(tRightBodyRect, clip.asset.name, m_clipStyle);
            tRightBodyRect = new Rect(fRectStartX, nextStartY + 14.0f, fRectWidth, 2.0f);
            GUI.Box(tRightBodyRect, GUIContent.none, trackLineStyle);
            Color colLight = new Color(1.0f, 1.0f, 1.0f, 0.2f);
            EditorGUI.DrawRect(
                new Rect(fRectStartX, nextStartY, fRectWidth, 2.0f),
                colLight);
            EditorGUI.DrawRect(
                new Rect(fRectStartX, nextStartY, 2.0f, kHeaderTrackHeight),
                colLight);

            Color colDark = new Color(0.2f, 0.2f, 0.2f, 0.2f);
            EditorGUI.DrawRect(
                new Rect(fRectStartX, nextStartY + kHeaderTrackHeight - 2.0f, fRectWidth-1.0f, 2.0f),
                colDark);
            EditorGUI.DrawRect(
                new Rect(fRectStartX + fRectWidth -2.0f, nextStartY , 2.0f, kHeaderTrackHeight),
                colDark);
            if (clip.asset.GetType() == typeof(StreamingImageSequencePlayableAsset))
            {
                DrawCacheStatus(nextStartY, clip);
            }
        }

        private void DrawCacheStatus(float nextStartY, TimelineClip clip)
        {
            var asset = clip.asset as StreamingImageSequencePlayableAsset;
            float wholeWidth = m_fRightAreaWidth - 16.0f;
            double start = clip.start;
            double end = clip.end;
            double duration = m_currentDirector.duration;

            start = start / duration;
            end = end / duration;

            double startPoint = wholeWidth * start;
            double endPoint = wholeWidth * end;
 




            int length = asset.GetImagePaths().Count;
            if (m_MovieProxyPlayableAssetToColorArray.ContainsKey(asset))
            {


            }
            else
            {
                m_MovieProxyPlayableAssetToColorArray.Add(asset, new BGJobCacheParam(asset));
            }
            /*
                        if (ImageLoadBGTask.m_sUpdated  )
                            new BGJobCacheChecker( m_MovieProxyPlayableAssetToColorArray[asset]);
            
            UInt32[] colorArray = m_MovieProxyPlayableAssetToColorArray[asset].m_collorArray;
            if (colorArray == null)
            {
                return;

            }
         */
         /*
            var parm = m_MovieProxyPlayableAssetToColorArray[asset];

             StreamingImageSequencePlugin.SetOverwrapWindowData(asset.GetInstanceID(), colorArray, colorArray.Length);
             */
            /*
            //        if (parm.m_NeedUpdate)
                    {
                        Texture2D result = parm.m_tex2D;
                        if ( result != null )
                        {
                            result.SetPixels(colorArray);
                            result.filterMode = FilterMode.Point;
                            result.Apply();
                            parm.m_style.normal.background = result;
                            //    Graphics.DrawTexture(new Rect(-20, -20, 40, 40), result);
                            parm.m_NeedUpdate = false;

                        }
                    }
                    */


            float fRectStartX = kHeaderWidth + kLineWidth + 8.0f + (float)startPoint;
            float fRectWidth = (float)(endPoint - startPoint);
   //         Rect tRightBodyRect = new Rect(fRectStartX, nextStartY + kHeaderTrackHeight -3.0f, fRectWidth, 1.0f);



        }

        private void InitGUIStyle( ref GUIStyle style, Color col )
        {
            if (style == null || style.normal.background == null)
            {
                style = new GUIStyle(GUI.skin.box);
                style.normal.background = MakeTex(2, 2, col);
            }
        }
        private void InitStyles()
        {
            InitGUIStyle(ref m_windowStyle, new Color(1f, 0f, 0f, 0.5f));
            InitGUIStyle(ref m_boxStyle, new Color(0f, 1f, 0f, 0.5f));
            InitGUIStyle(ref m_clipStyle, new Color(93.0f / 255.0f, 97.0f / 255.0f, 102.0f / 255.0f));
            InitGUIStyle(ref m_headerBgStyle, new Color(40.0f / 255.0f, 40.0f / 255.0f, 40.0f / 255.0f, 1.0f));
            InitGUIStyle(ref m_contentBgStyle, new Color(40.0f / 255.0f, 40.0f / 255.0f, 40.0f / 255.0f, 1.0f));
            InitGUIStyle(ref m_trackGroupHeaderStyle, new Color(39.0f / 255.0f, 55.0f / 255.0f, 57.0f / 255.0f, 1.0f));
            InitGUIStyle(ref m_trackGroupBodyStyle, new Color(36.0f / 255.0f, 47.0f / 255.0f, 48.0f / 255.0f, 1.0f));
            InitGUIStyle(ref m_trackHeaderStyle, new Color(65.0f / 255.0f, 65.0f / 255.0f, 65.0f / 255.0f, 1.0f));
            InitGUIStyle(ref m_trackBGStyle, new Color(49 / 255.0f, 49.0f / 255.0f, 49.0f / 255.0f, 1.0f));
            InitGUIStyle(ref m_ActivationTrackLineStyle, new Color(0.0f, 152.0f / 255.0f, 33.0f / 255.0f, 0.9f));
            InitGUIStyle(ref m_AnimationTrackLineStyle, new Color(36.0f / 255.0f, 85.0f / 255.0f, 137.0f / 255.0f, 1.0f));
            InitGUIStyle(ref m_AudioTrackLineStyle, new Color(1f, 162.0f / 255.0f, 0f, 1.0f));
            InitGUIStyle(ref m_ControlTrackLineStyle, new Color(59.0f / 255.0f, 162.0f / 255.0f, 149.0f / 255.0f, 1.0f));
            InitGUIStyle(ref m_PlayableTrackLineStyle, new Color(1.0f, 1.0f, 1.0f, 1.0f));
            InitGUIStyle(ref m_MovieProxyTrackLineStyle, new Color(0.776f, 0.263f, 0.09f, 1.0f));


        }



        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }



 
        private Rect GetWindowPosition(EditorWindow eidtorWindow)
        {
            var bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField;
            var type = eidtorWindow.GetType().BaseType;
            //        var info = type.GetField("m_Pos", bf);
            var info = type.GetProperty("position", bf);
            Rect rect = (Rect)info.GetValue(eidtorWindow, null) ;
            return rect;
        }




        public class PopupExample : PopupWindowContent
        {
            bool toggle1 = true;
            bool toggle2 = true;
            bool toggle3 = true;

            public override Vector2 GetWindowSize()
            {
                return new Vector2(200, 150);
            }

            public override void OnGUI(Rect rect)
            {
                GUILayout.Label("Popup Options Example", EditorStyles.boldLabel);
                toggle1 = EditorGUILayout.Toggle("Toggle 1", toggle1);
                toggle2 = EditorGUILayout.Toggle("Toggle 2", toggle2);
                toggle3 = EditorGUILayout.Toggle("Toggle 3", toggle3);
            }

            public override void OnOpen()
            {
                LogUtility.LogDebug("Popup opened: " + this);
            }

            public override void OnClose()
            {
                LogUtility.LogDebug("Popup closed: " + this);
            }
        }

    }
}

