using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;
using System.IO;
using UnityEngine.StreamingImageSequence;

namespace UnityEditor.StreamingImageSequence
{


    public class BGJobCacheChecker : BackGroundTask
    {

        BGJobCacheParam m_param;

        public BGJobCacheChecker( BGJobCacheParam param )
        {
            m_param = param;
            UpdateManager.QueueBackGroundTask(this);
            UpdateManager.m_ResetDelegate += Reset;
        }
        public override void Execute()
        {
            ReadResult tResult = new ReadResult();

            int loaded = 0;
            for ( int ii = 0; ii < m_param.m_collorArray.Length; ii++ )
            {
                string fileName = m_param.m_asset.GetImagePath(ii);
                //m_collorArray[ii] = notYet;
                if ( StreamingImageSequencePlugin.GetNativeTextureInfo(m_param.m_asset.GetCompleteFilePath(fileName), 
                    out tResult, StreamingImageSequenceConstants.TEXTURE_TYPE_FULL) )
                {
                    if ( tResult.ReadStatus == StreamingImageSequenceConstants.READ_RESULT_SUCCESS )
                    {
                        m_param.m_collorArray[ii] = 0xffffffff;
                        loaded ++;
                    }
                }

            }

            int length = m_param.m_collorArray.Length;
            if (loaded == length)
            {
                m_param.m_allLoaded = true;
            }
            else if ( m_param.m_asset.m_loadingIndex == length)
            {
                // this is neccessary if script files are rebuilt while thread is execuiting in editor.
                m_param.m_asset.m_loadingIndex = -1;
#if UNITY_EDITOR
                //                Util.Log("Fetched the situation");
#endif
            }

        }

        private void Reset()
        {
            m_param.Reinitialize();
        }

    }

    public class BGJobCacheParam
    {
        public StreamingImageSequencePlayableAsset m_asset;
        public UInt32[] m_collorArray;
        public Texture2D m_tex2D;
        public bool m_allLoaded;

 //       public GUIStyle m_style;
        public BGJobCacheParam(StreamingImageSequencePlayableAsset asset)
        {
           m_asset = asset;
           int imageCount = m_asset.GetImagePaths().Count;
           m_collorArray = new UInt32[imageCount];

           m_tex2D = new Texture2D(imageCount, 1);
           m_allLoaded = false;
 //           m_style = new GUIStyle(GUI.skin.box); 
        }

        public void Reinitialize()
        {
            int imageCount = m_asset.GetImagePaths().Count;
            m_collorArray = new UInt32[imageCount];

            m_tex2D = new Texture2D(imageCount, 1);
            m_allLoaded = false;
        }
    }

}
