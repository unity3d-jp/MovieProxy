namespace UnityEngine.StreamingImageSequence {

    internal class PreviewImageLoadBGTask : BackGroundTask {

    internal static void Queue(string strFileName, int width, int height) {
        PreviewImageLoadBGTask task = new PreviewImageLoadBGTask(strFileName, width, height);
        UpdateManager.QueueBackGroundTask(task);
        
    }

//----------------------------------------------------------------------------------------------------------------------
    private PreviewImageLoadBGTask( string fileName, int width, int height) {
        m_fileName = fileName;
        m_width = width;
        m_height = height;
    }

//----------------------------------------------------------------------------------------------------------------------

    public override void Execute() {
        const int TEX_TYPE = StreamingImageSequenceConstants.TEXTURE_TYPE_PREVIEW;
        StreamingImageSequencePlugin.GetNativeTextureInfo(m_fileName, out ReadResult tResult, TEX_TYPE);
        switch (tResult.ReadStatus) {
            case StreamingImageSequenceConstants.READ_RESULT_NONE: {
                //Debug.Log("Loading: " + m_fileName);
                StreamingImageSequencePlugin.LoadAndAllocPreviewTexture(m_fileName, m_width, m_height);
                break;
            }
            case StreamingImageSequenceConstants.READ_RESULT_REQUESTED: {
#if UNITY_EDITOR
                LogUtility.LogDebug("Already requested:" + m_fileName);
#endif
                break;
            }
            default: break;
        }

    }

//----------------------------------------------------------------------------------------------------------------------

    string m_fileName;
    private int m_width;
    private int m_height;

}
} //end namespace
