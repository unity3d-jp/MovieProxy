using System;
using System.IO;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor.Timeline;
using UnityEditor;
#endif

namespace UnityEngine.StreamingImageSequence {

    //ITimelineClipAsset interface is used to define the clip capabilities (ClipCaps) 
    [System.Serializable]
    public class StreamingImageSequencePlayableAsset : PlayableAsset, ITimelineClipAsset, IPlayableBehaviour {
        
//----------------------------------------------------------------------------------------------------------------------
        public virtual void OnBehaviourDelay(Playable playable, FrameData info) {

        }
        public virtual void OnBehaviourPause(Playable playable, FrameData info){

        }
        public virtual void OnBehaviourPlay(Playable playable, FrameData info){

        }
        public virtual void OnGraphStart(Playable playable){

        }
        public virtual void OnGraphStop(Playable playable){

        }
        public virtual void OnPlayableCreate(Playable playable){

        }
        public virtual void OnPlayableDestroy(Playable playable){

        }
        public virtual void PrepareData(Playable playable, FrameData info){

        }
        public virtual void PrepareFrame(Playable playable, FrameData info){

        }

        public virtual void ProcessFrame(Playable playable, FrameData info, object playerData) {
        }

//----------------------------------------------------------------------------------------------------------------------

        public StreamingImageSequencePlayableAsset() {
            m_loadingIndex = -1;
            m_lastIndex = -1;
#if UNITY_EDITOR            
            m_timelineEditorCurveBinding  = new EditorCurveBinding() {
                path = "",
                type = typeof(StreamingImageSequencePlayableAsset),
                propertyName = "m_time"
            };
#endif            
        }

        ~StreamingImageSequencePlayableAsset() {
            Reset();
        }
//----------------------------------------------------------------------------------------------------------------------        
        //Calculate Image Sequence Time, which is normalized [0..1] 
        private double GlobalTimeToCurveTime(double globalTime) {
            double localTime = m_timelineClip.ToLocalTime(globalTime);
            return LocalTimeToCurveTime(localTime);
        }

//----------------------------------------------------------------------------------------------------------------------
        private double LocalTimeToCurveTime(double localTime) {
            AnimationCurve curve = GetAndValidateAnimationCurve();
            return curve.Evaluate((float)localTime);
        }
//----------------------------------------------------------------------------------------------------------------------

        //Calculate the used image index for the passed globalTime
        internal int GlobalTimeToImageIndex(double globalTime) {
            double imageSequenceTime = GlobalTimeToCurveTime(globalTime);
            int count = m_imagePaths.Count;
            int index = (int)(count * imageSequenceTime);
            index = Mathf.Clamp(index, 0, count - 1);
            return index;
        }

//----------------------------------------------------------------------------------------------------------------------

        //Calculate the used image index for the passed localTime
        internal int LocalTimeToImageIndex(double localTime) {
            double imageSequenceTime = LocalTimeToCurveTime(localTime);
            int count = m_imagePaths.Count;
            int index = (int)(count * imageSequenceTime);
            index = Mathf.Clamp(index, 0, count - 1);
            return index;
        }

//----------------------------------------------------------------------------------------------------------------------

        internal int GetVersion() { return m_version; }
        internal IList<string> GetImagePaths() { return m_imagePaths; }
        internal ImageDimensionInt GetResolution() { return m_resolution; }
        internal System.Collections.IList GetImagePathsNonGeneric() { return m_imagePaths; }
        internal Texture2D GetTexture() { return m_texture; }

//----------------------------------------------------------------------------------------------------------------------        

        public float GetDimensionRatio() {
            if (Mathf.Approximately(m_dimensionRatio, 0f)) {
                m_dimensionRatio = m_resolution.CalculateRatio();
            }

            return m_dimensionRatio;
        }
//----------------------------------------------------------------------------------------------------------------------        

        public string GetImagePath(int index) {
            if (null == m_imagePaths || index >= m_imagePaths.Count)
                return null;

            return m_imagePaths[index];
        }

        public string GetFolder() { return m_folder; }
        public UnityEditor.DefaultAsset GetTimelineDefaultAsset() { return m_timelineDefaultAsset; }

//----------------------------------------------------------------------------------------------------------------------        
        internal void Reset() {
            m_loadingIndex = -1;
            m_lastIndex = -1;
            m_loadRequested = null;
            ResetTexture();
            m_resolution = new ImageDimensionInt();
        }
        
        
//----------------------------------------------------------------------------------------------------------------------        

        public ClipCaps clipCaps {
            get { return ClipCaps.ClipIn | ClipCaps.SpeedMultiplier; }
        }
        
//----------------------------------------------------------------------------------------------------------------------        

        public bool Verified
        {
            get
            {
                if (!m_verified)
                {
                    m_verified = !string.IsNullOrEmpty(m_folder) && 
                                 m_folder.StartsWith("Assets/StreamingAssets") &&
                                 Directory.Exists(m_folder) && 
                                 m_imagePaths != null && 
                                 m_imagePaths.Count > 0;
                }
                
                return m_verified;
            }
        }

//----------------------------------------------------------------------------------------------------------------------        

         
        public void SetParam(StreamingImageSequencePlayableAssetParam param) {
            m_resolution = param.Resolution;
            m_dimensionRatio = m_resolution.CalculateRatio();
            m_imagePaths = param.Pictures;
            m_folder = param.Folder;
            if (m_folder.StartsWith("Assets")) {
                m_timelineDefaultAsset = AssetDatabase.LoadAssetAtPath<UnityEditor.DefaultAsset>(m_folder);
            } else {
                m_timelineDefaultAsset = null;
            }
            m_texture = null;
            EditorUtility.SetDirty(this);
        }

//---------------------------------------------------------------------------------------------------------------------

        public override Playable CreatePlayable(PlayableGraph graph, GameObject go) {
            //Dummy. We just need to implement this from PlayableAsset because folder D&D support. See notes below
            return Playable.Null;
        }
        
        public override double duration {  get {  return (null!=m_timelineClip) ? m_timelineClip.duration : 0;  }  }
        
//---------------------------------------------------------------------------------------------------------------------

        internal void LoadRequest(bool isDirectorIdle) {
            if (null == m_imagePaths)
                return;

            int numPictures = m_imagePaths.Count;
            if (m_loadRequested == null && numPictures > 0) {
                m_loadRequested = new bool[numPictures];
            }

            // request loading while editor is idle.
            if (isDirectorIdle)
            {
                LoadStep(4);
            }
            else
            {
                LoadStep(2);
            }
        }

        private void LoadStep(int step)
        {
            if (UpdateManager.IsPluginResetting())
            {
                return;
            }
            int loadRequestMax = m_loadingIndex + step;
            if (loadRequestMax > m_imagePaths.Count)
            {
                loadRequestMax = m_imagePaths.Count;
            }
            for (int ii = m_loadingIndex; ii <= loadRequestMax - 1; ii++)
            {
                if (ii == -1)
                {
                    continue;
                }

                LoadRequest(ii, false, out ReadResult readResult);

            }
            m_loadingIndex = loadRequestMax;

            
        }

        internal bool IsLoadRequested(int index)
        {
            string filename = m_imagePaths[index];
            filename = GetCompleteFilePath(filename);
            StreamingImageSequencePlugin.GetNativeTextureInfo(filename, out ReadResult readResult, 
                StreamingImageSequenceConstants.TEXTURE_TYPE_FULL
            );
            return (readResult.ReadStatus != 0);

        }

//----------------------------------------------------------------------------------------------------------------------        
        internal string LoadRequest(int index, bool isBlocking, out ReadResult readResult) {
            const int texType = StreamingImageSequenceConstants.TEXTURE_TYPE_FULL;
            string filename = m_imagePaths[index];
            filename = GetCompleteFilePath(filename);
            if (m_loadRequested == null) {
                m_loadRequested = new bool[m_imagePaths.Count];
            }

            //test
            AnimationCurve curve = GetAndValidateAnimationCurve();
            Debug.Log(curve.keys.Length);

            StreamingImageSequencePlugin.GetNativeTextureInfo(filename, out readResult, texType);
            //Debug.Log("readResult.readStatus " + readResult.readStatus + "Loading " + filename);
            if (readResult.ReadStatus == StreamingImageSequenceConstants.READ_RESULT_NONE) {
                ImageLoadBGTask.Queue(filename);
            }
            if ( isBlocking ) {
                while (readResult.ReadStatus != StreamingImageSequenceConstants.READ_RESULT_SUCCESS) {
                    StreamingImageSequencePlugin.GetNativeTextureInfo(filename, out readResult, texType);
                }
            }
#if false //UNITY_EDITOR
            if ( readResult.readStatus == 1 )
            {
                Util.Log("Already requestd:" + filename);
            }
#endif
            return filename;
        }
//----------------------------------------------------------------------------------------------------------------------        
        

        internal bool RequestLoadImage(int index, bool isBlocking)
        {
            if (null == m_imagePaths || index < 0 || index >= m_imagePaths.Count || string.IsNullOrEmpty(m_imagePaths[index])) {
                return false;
            }

          
            string filename = LoadRequest(index,isBlocking, out ReadResult readResult);

            if (null == m_texture &&  readResult.ReadStatus == StreamingImageSequenceConstants.READ_RESULT_SUCCESS) {

                m_texture = StreamingImageSequencePlugin.CreateTexture(ref readResult);

                IntPtr ptr =  m_texture.GetNativeTexturePtr();
                int texInstanceID = m_texture.GetInstanceID();
                
                UpdateResolution(ref readResult);
                StreamingImageSequencePlugin.SetNativeTexturePtr(ptr, (uint)readResult.Width, (uint)readResult.Height, texInstanceID);
            }

            //Update the texture
			if (readResult.ReadStatus == StreamingImageSequenceConstants.READ_RESULT_SUCCESS && m_lastIndex != index) {
                int texInstanceID = m_texture.GetInstanceID();
                StreamingImageSequencePlugin.SetLoadedTexture (filename, texInstanceID);
                GL.IssuePluginEvent(StreamingImageSequencePlugin.GetRenderEventFunc(), texInstanceID);
			}

			m_lastIndex = index;
            return null!=m_texture;
        }
//----------------------------------------------------------------------------------------------------------------------        

        public string GetCompleteFilePath(string filePath)
        {
            string strOverridePath = m_folder;

            if (!string.IsNullOrEmpty(strOverridePath)) {
                filePath = Path.Combine(strOverridePath, filePath).Replace("\\", "/");
            }

            if (Path.IsPathRooted(filePath)) {
                filePath = Path.Combine(UpdateManager.GetProjectFolder(), filePath).Replace("\\", "/");
            }
            return filePath;
        }

//---------------------------------------------------------------------------------------------------------------------
        void ResetTexture() {
            if (null != m_texture) {
                StreamingImageSequencePlugin.ResetLoadedTexture(m_texture.GetInstanceID());
                m_texture = null;
            }
        }

//---------------------------------------------------------------------------------------------------------------------
        void UpdateResolution(ref ReadResult readResult) {
            m_resolution.Width  = readResult.Width;
            m_resolution.Height = readResult.Height;
            m_dimensionRatio = m_resolution.CalculateRatio();
        }
//---------------------------------------------------------------------------------------------------------------------
        
        public void Setup(TimelineClip clip) {
            if (null == clip.curves) {
                clip.CreateCurves("Curves: " + clip.displayName);
            }
            m_timelineClip = clip;
            m_clipStart = clip.start;
        }

//----------------------------------------------------------------------------------------------------------------------
        public void OnAfterTrackDeserialize(TimelineClip clip) {
            Setup(clip);
        }

//----------------------------------------------------------------------------------------------------------------------
        public void ResetAnimationCurve() {
            AnimationCurve animationCurve = new AnimationCurve();
            ValidateAnimationCurve(ref animationCurve);
            RefreshAnimationCurveInTimelineClip(animationCurve);
            m_timelineClip.clipIn = 0;
            m_timelineClip.timeScale = 1.0;
        }

//----------------------------------------------------------------------------------------------------------------------
        public void ValidateAnimationCurve() {
            Debug.Log("ValidateAnimationCurve");
            AnimationCurve curve = GetAndValidateAnimationCurve();
            RefreshAnimationCurveInTimelineClip(curve);
        }

//----------------------------------------------------------------------------------------------------------------------
        //Get the animation curve from the TimelineClip.  
        private AnimationCurve GetAndValidateAnimationCurve() {
            AnimationCurve animationCurve = null;
#if UNITY_EDITOR
            animationCurve = AnimationUtility.GetEditorCurve(m_timelineClip.curves, m_timelineEditorCurveBinding);
#endif
            if (null == animationCurve)
                animationCurve = new AnimationCurve();
            
            ValidateAnimationCurve(ref animationCurve);
            return animationCurve;
        }

//----------------------------------------------------------------------------------------------------------------------
        //Validate: make sure we have at least two keys
        private void ValidateAnimationCurve(ref AnimationCurve animationCurve) {
            int numKeys = animationCurve.keys.Length;
            switch (numKeys) {
                case 0: {
                    animationCurve = AnimationCurve.Linear(0, 0, (float) m_timelineClip.duration,1 );
                    break;
                }
                case 1: {
                    animationCurve.keys[0] = new Keyframe(0.0f,0.0f);
                    animationCurve.AddKey((float)m_timelineClip.duration, 1.0f);
                    break;
                }
                default: break;
            }
        }
        
//----------------------------------------------------------------------------------------------------------------------

        private void  RefreshAnimationCurveInTimelineClip(AnimationCurve curve) {
            m_timelineClip.curves.SetCurve("", typeof(StreamingImageSequencePlayableAsset), "m_time", curve);
#if UNITY_EDITOR            
            //[TODO-sin: 2019-12-25] Is there a way to make this smoother ?
            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved );
#endif            
        }

        
//----------------------------------------------------------------------------------------------------------------------

        [SerializeField] private string m_folder;
        [SerializeField] List<string> m_imagePaths;
        [SerializeField] private int m_version = STREAMING_IMAGE_SEQUENCE_PLAYABLE_ASSET_VERSION;        
        [SerializeField] private ImageDimensionInt  m_resolution;        
        [SerializeField] double m_time;

        [SerializeField] [HideInInspector] float m_dimensionRatio;

        double m_clipStart;     //In global space. In seconds


#if UNITY_EDITOR
        [SerializeField] private UnityEditor.DefaultAsset m_timelineDefaultAsset = null; //Folder D&D. See notes below
        private EditorCurveBinding m_timelineEditorCurveBinding;
#endif
        private bool[] m_loadRequested;
        //[TODO-sin: 2019-12-25] Is there a way we can just serialize this without affecting folder D&D
        TimelineClip m_timelineClip  = null; 

        //[TODO-sin: 2020-1-30] Don't serialize this
        public int m_loadingIndex;
		private int m_lastIndex;
        private bool m_verified;

        Texture2D m_texture = null;

        private const int STREAMING_IMAGE_SEQUENCE_PLAYABLE_ASSET_VERSION = 1;

    }
}

//---------------------------------------------------------------------------------------------------------------------
//[Note-Sin: 2019-12-23] We need two things, in order to enable folder drag/drop to the timeline Window
//1. Derive this class from PlayableAsset
//2. Declare UnityEditor.DefaultAsset variable 



