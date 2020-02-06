using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
namespace UnityEngine.StreamingImageSequence
{ 
    [TrackClipType(typeof(StreamingImageSequencePlayableAsset))]
    [TrackBindingType(typeof(StreamingImageSequenceNativeRenderer))]
    [TrackColor(0.776f, 0.263f, 0.09f)]
    public class StreamingImageSequenceTrack : TrackAsset
    {
        LoaderPeriodicJob m_LoaderPeriodicJob;
        public StreamingImageSequenceTrack()
        {
            LogUtility.LogDebug("StreamingImageSequenceTrack creating ObserverPeriodicJob");
            m_LoaderPeriodicJob = new LoaderPeriodicJob(this);
            m_LoaderPeriodicJob.AddToUpdateManger();
            // above job is removed when finished to load by calling RemoveIfFinished();
        }
#if false
        protected override Playable CreatePlayable(PlayableGraph graph, GameObject go, TimelineClip clip)
        {
            var playable = (ScriptPlayable<StreamingImageSequencePlayableBehaviour>)base.CreatePlayable(graph, go, clip);
            var myBehaviour = playable.GetBehaviour() as StreamingImageSequencePlayableBehaviour;
            myBehaviour.m_clip = clip;
            return playable;
        }
#endif

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var mixer = ScriptPlayable<StreamingImageSequencePlayableMixer>.Create(graph, inputCount);
            
            var director = go.GetComponent<PlayableDirector>();
            if (director != null)
            {
                var boundGo = director.GetGenericBinding(this);
                var outputGo = boundGo as StreamingImageSequenceNativeRenderer;
                StreamingImageSequencePlayableMixer bh = mixer.GetBehaviour();
                bh.m_track = this;
                bh.m_clips = GetClips();
                if (outputGo != null)
                {
                    bh.BindGameObject(outputGo.gameObject);
                }
                bh.m_PlayableDirector = director;

            }
            return mixer;
        }

//---------------------------------------------------------------------------------------------------------------------

        protected override  void OnAfterTrackDeserialize() {
            //Re-setup the PlayableAsset
            foreach (TimelineClip clip in m_Clips) {
                StreamingImageSequencePlayableAsset playableAsset = clip.asset as StreamingImageSequencePlayableAsset;
                if (null == playableAsset)
                    continue;
                
                playableAsset.OnAfterTrackDeserialize(clip);
            }
        }

//---------------------------------------------------------------------------------------------------------------------

    }

}