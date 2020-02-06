using System;
using UnityEditor;

namespace UnityEngine.StreamingImageSequence {
    
internal class PreviewTexture : IDisposable {

    public PreviewTexture(Texture2D tex) {
        m_texture = tex;
        m_lastAccessTime = EditorApplication.timeSinceStartup;
    }

//----------------------------------------------------------------------------------------------------------------------    
    ~PreviewTexture() {
        Dispose();
    }
    
//----------------------------------------------------------------------------------------------------------------------    
    public void Dispose() {
        if (null == m_texture)
            return;
        
        if (Application.isPlaying)
            UnityEngine.Object.Destroy(m_texture);
        else
            UnityEngine.Object.DestroyImmediate(m_texture);


        m_texture = null;
    }
    
//----------------------------------------------------------------------------------------------------------------------    
    public Texture2D GetTexture() {
        m_lastAccessTime = EditorApplication.timeSinceStartup;
        return m_texture;
    }

//----------------------------------------------------------------------------------------------------------------------    

    public double GetLastAccessTime() { return m_lastAccessTime; }


//----------------------------------------------------------------------------------------------------------------------    
    private Texture2D m_texture = null;
    private double m_lastAccessTime;
    
}

} //end namespace
