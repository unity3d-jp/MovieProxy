using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace UnityEngine.StreamingImageSequence {

internal static class PreviewTextureFactory {

    [InitializeOnLoadMethod]
    static void OnLoad() {
        m_previewTextures = new Dictionary<string, PreviewTexture>();
        m_obsoleteTextures = new List<string>();
        m_removeObsoleteTextures = false;
        EditorApplication.update += Update;
    }
    
//----------------------------------------------------------------------------------------------------------------------    

    public static void Reset() {
        m_previewTextures.Clear();
    }
    
//----------------------------------------------------------------------------------------------------------------------    

    public static Texture2D GetOrCreate(string fullPath, ref ReadResult readResult) {
        Assert.IsTrue(StreamingImageSequenceConstants.READ_RESULT_SUCCESS == readResult.ReadStatus);
        
        //We only remove obsolete textures if there is any access to PreviewTextures in a particular frame. 
        //For example, if Unity is not in focus, then we don't want to remove them.
        m_removeObsoleteTextures = true;

        if (m_previewTextures.ContainsKey(fullPath)) {
            Assert.IsNotNull(m_previewTextures[fullPath].GetTexture());
            return m_previewTextures[fullPath].GetTexture();
        }

        Texture2D newTex = StreamingImageSequencePlugin.CreateTexture(ref readResult);
        newTex.name = fullPath;
        m_previewTextures[fullPath] = new PreviewTexture(newTex);
        newTex.hideFlags = HideFlags.HideAndDontSave; //This is static. Don't destroy the tex if a new scene is loaded
        
        return newTex;
    }

//----------------------------------------------------------------------------------------------------------------------    
    private static void Update() {
        double curTime = EditorApplication.timeSinceStartup;
        if (!m_removeObsoleteTextures)
            return;

        if (StreamingImageSequencePlugin.IsResetting())
            return;

        //Remove obsolete textures
        m_obsoleteTextures.Clear();
        foreach (KeyValuePair<string, PreviewTexture> keyValue in m_previewTextures) {
            if (curTime - keyValue.Value.GetLastAccessTime() > OBSOLETE_TIME) {
                m_obsoleteTextures.Add(keyValue.Key);
            }
        }
        foreach (string texFullPath in m_obsoleteTextures) {
            m_previewTextures.Remove(texFullPath);
        }

        m_removeObsoleteTextures = false;
    }
    
//----------------------------------------------------------------------------------------------------------------------    
    
    private static Dictionary<string, PreviewTexture> m_previewTextures = null;
    private static List<string> m_obsoleteTextures = null;
    
    private const double OBSOLETE_TIME = 10; //seconds 
    static bool m_removeObsoleteTextures;


}

} //end namespace