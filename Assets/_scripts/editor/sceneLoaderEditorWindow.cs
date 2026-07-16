using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(sceneLoader))]
public class sceneLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

    }

    void OnStripClubLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnStripClubLoaded;
        sceneLoader t = (sceneLoader)target;
       
    }

    void OnPelleteuseLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnPelleteuseLoaded;
        sceneLoader t = (sceneLoader)target;
      
    }
}
