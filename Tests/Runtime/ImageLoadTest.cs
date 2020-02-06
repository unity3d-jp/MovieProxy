using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.StreamingImageSequence;
using UnityEngine.TestTools;

namespace UnityEditor.StreamingImageSequence.Tests {
    public class ImageLoadTest {


        [UnityTest]
        public IEnumerator QueueFullImageLoadTask() {
            const string PKG_PATH = "Packages/com.unity.streaming-image-sequence/Tests/Data/png/A_00000.png";
            string fullPath = Path.GetFullPath(PKG_PATH);
            Assert.IsTrue(File.Exists(fullPath));

            const int TEX_TYPE = StreamingImageSequenceConstants.TEXTURE_TYPE_FULL;

            StreamingImageSequencePlugin.GetNativeTextureInfo(fullPath, out ReadResult readResult, TEX_TYPE);
            Assert.AreEqual(StreamingImageSequenceConstants.READ_RESULT_NONE, readResult.ReadStatus, 
                "Texture is already or currently being loaded"
            );

            ImageLoadBGTask.Queue(fullPath);
            yield return new WaitForSeconds(LOAD_TIMEOUT);

            StreamingImageSequencePlugin.GetNativeTextureInfo(fullPath, out readResult, TEX_TYPE);
            Assert.AreEqual(StreamingImageSequenceConstants.READ_RESULT_SUCCESS, readResult.ReadStatus,
                "Loading texture is not successful."
            );

            AssertUnloaded(fullPath, TEX_TYPE);
            ResetAndAssert(fullPath, TEX_TYPE);
        }

//----------------------------------------------------------------------------------------------------------------------
        [UnityTest]
        public IEnumerator QueuePreviewImageLoadTask() {
            const string PKG_PATH = "Packages/com.unity.streaming-image-sequence/Tests/Data/png/A_00000.png";
            string fullPath = Path.GetFullPath(PKG_PATH);
            Assert.IsTrue(File.Exists(fullPath));

            const int TEX_TYPE = StreamingImageSequenceConstants.TEXTURE_TYPE_PREVIEW;

            StreamingImageSequencePlugin.GetNativeTextureInfo(fullPath, out ReadResult readResult, TEX_TYPE);
            Assert.AreEqual(StreamingImageSequenceConstants.READ_RESULT_NONE, readResult.ReadStatus, 
                "Texture is already or currently being loaded"
            );

            const int WIDTH = 256;
            const int HEIGHT= 128;
            PreviewImageLoadBGTask.Queue(fullPath, WIDTH, HEIGHT);
            yield return new WaitForSeconds(LOAD_TIMEOUT);

            StreamingImageSequencePlugin.GetNativeTextureInfo(fullPath, out readResult, TEX_TYPE);
            Assert.AreEqual(StreamingImageSequenceConstants.READ_RESULT_SUCCESS, readResult.ReadStatus, 
                "Loading texture is not successful."
            );
            Assert.AreEqual(WIDTH, readResult.Width );
            Assert.AreEqual(HEIGHT, readResult.Height);

            AssertUnloaded(fullPath, TEX_TYPE);
            ResetAndAssert(fullPath, TEX_TYPE);
        }

//----------------------------------------------------------------------------------------------------------------------

        //Check that this image is not loaded yet, except for a particular texture type
        void AssertUnloaded(string fullPath, int exceptionTexType) {
            //Check that we are not affecting the other image types
            for (int texType = 0; texType < StreamingImageSequenceConstants.MAX_TEXTURE_TYPES;++texType) {
                if (texType == exceptionTexType)
                    continue;
                StreamingImageSequencePlugin.GetNativeTextureInfo(fullPath, out ReadResult otherReadResult, texType);
                Assert.AreEqual(StreamingImageSequenceConstants.READ_RESULT_NONE, otherReadResult.ReadStatus, 
                    "AssertUnloaded()"
                );
            }
        }

//----------------------------------------------------------------------------------------------------------------------

        void ResetAndAssert(string fullPath, int texType) {
            StreamingImageSequencePlugin.ResetNativeTexture(fullPath);
            StreamingImageSequencePlugin.GetNativeTextureInfo(fullPath, out ReadResult readResult, texType);
            Assert.AreEqual(StreamingImageSequenceConstants.READ_RESULT_NONE, readResult.ReadStatus, "ResetAndAssert");
        }

//----------------------------------------------------------------------------------------------------------------------

        private const float LOAD_TIMEOUT = 0.1f; //in seconds
    }

} //end namespace
