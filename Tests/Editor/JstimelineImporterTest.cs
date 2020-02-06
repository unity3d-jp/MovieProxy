using NUnit.Framework;
using System.IO;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.StreamingImageSequence;
using UnityEngine.Timeline;

namespace UnityEditor.StreamingImageSequence.Tests {
    public class JstimelineImporterTest {
    [Test]
    public void Import() {
        string fullPath = "Packages/com.unity.streaming-image-sequence/Tests/Data/AeConvert.jstimeline";
        Assert.IsTrue(File.Exists(fullPath));

        JstimelineImporter.ImportTimeline(fullPath);

        //Check if the generated director is valid
        PlayableDirector[] directors = Object.FindObjectsOfType<PlayableDirector>();
        Assert.AreEqual(directors.Length,1);

        PlayableDirector pd = directors[0];
        TimelineAsset timelineAsset = pd.playableAsset as TimelineAsset;
        Assert.IsNotNull(timelineAsset);

        Assert.AreEqual(timelineAsset.outputTrackCount,1);
        foreach (TrackAsset trackAsset in timelineAsset.GetOutputTracks()) {
            StreamingImageSequenceTrack imageSequenceTrack = trackAsset as StreamingImageSequenceTrack;
            Assert.IsNotNull(imageSequenceTrack);

            foreach (TimelineClip clip in imageSequenceTrack.GetClips()) {
                Assert.IsNotNull(clip.asset);
                StreamingImageSequencePlayableAsset playableAsset = clip.asset as StreamingImageSequencePlayableAsset;
                Assert.IsNotNull(playableAsset);
                
                Assert.AreEqual(playableAsset.GetImagePaths().Count,10);
            }

            //Make sure a StreamingImageSequenceNativeRenderer is bound to the trackAsset
            StreamingImageSequenceNativeRenderer r = pd.GetGenericBinding(trackAsset) as StreamingImageSequenceNativeRenderer;
            Assert.IsNotNull(r);
        }

    }

}

} //end namespace
