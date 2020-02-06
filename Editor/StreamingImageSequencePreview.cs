using System;
using System.Collections.Generic;
using NUnit.Framework.Constraints;

namespace UnityEngine.StreamingImageSequence {

internal class StreamingImageSequencePreview : IDisposable {

    public StreamingImageSequencePreview(StreamingImageSequencePlayableAsset playableAsset) {
        m_playableAsset = playableAsset;
    }

//----------------------------------------------------------------------------------------------------------------------
    public void Dispose() {
        if (m_disposed) return;

        m_disposed= true;
    }

//----------------------------------------------------------------------------------------------------------------------
    internal void SetLocalTime(double startTime, double endTime) {
        m_localStartTime = startTime;
        m_localEndTime = endTime;
    }

//----------------------------------------------------------------------------------------------------------------------
    internal void Render(Rect rect) {

        IList<string> imagePaths = m_playableAsset.GetImagePaths();

        //TODO-sin: 2020-1-17 Set the size of textures according to the used size in the Timeline instead of full size
        ImageDimensionInt resolution = m_playableAsset.GetResolution();

        //Calculate rect for one image.
        float dimensionRatio = m_playableAsset.GetDimensionRatio();
        int widthPerPreviewImage = (int) (dimensionRatio * rect.height);
        int heightPerPreviewImage = (int)rect.height;

        //Initialize variables to display the preview images correctly.
        int numPreviewImages = Mathf.FloorToInt(rect.width / widthPerPreviewImage);
        double usedWidthRatio = (numPreviewImages * widthPerPreviewImage) / rect.width;
        double endPreviewTime = (m_localEndTime - m_localStartTime) * usedWidthRatio + m_localStartTime;
        double localTimeCounter = (endPreviewTime - m_localStartTime) / numPreviewImages;
        Rect drawRect = new Rect(rect) {
            width = widthPerPreviewImage,
            height = heightPerPreviewImage
        };

        //Each preview should show the image used in the time in the middle of the span, instead of the left start point
        double localTime = m_localStartTime + (localTimeCounter * 0.5f);

        for (int i = 0; i < numPreviewImages; ++i) {

            int imageIndex = m_playableAsset.LocalTimeToImageIndex(localTime);

            //Load
            string fullPath = m_playableAsset.GetCompleteFilePath(imagePaths[imageIndex]);
            StreamingImageSequencePlugin.GetNativeTextureInfo(fullPath, out ReadResult readResult, 
                StreamingImageSequenceConstants.TEXTURE_TYPE_PREVIEW);
            switch (readResult.ReadStatus) {
                case StreamingImageSequenceConstants.READ_RESULT_NONE: {
                    PreviewImageLoadBGTask.Queue(fullPath, widthPerPreviewImage, heightPerPreviewImage);
                    break;
                }
                case StreamingImageSequenceConstants.READ_RESULT_SUCCESS: {
                    Texture2D tex = PreviewTextureFactory.GetOrCreate(fullPath, ref readResult);
                    if (null != tex) {
                        Graphics.DrawTexture(drawRect, tex);
                    }
                    break;
                }
                default: {
                    break;
                }

            }

            drawRect.x += widthPerPreviewImage;
            localTime += localTimeCounter;
        }

    }

//----------------------------------------------------------------------------------------------------------------------
    private bool m_disposed;
    private readonly StreamingImageSequencePlayableAsset m_playableAsset = null;
    private const int MIN_PREVIEW_IMAGE_WIDTH = 60;
    private double m_localStartTime;
    private double m_localEndTime;
}

} //end namespace

