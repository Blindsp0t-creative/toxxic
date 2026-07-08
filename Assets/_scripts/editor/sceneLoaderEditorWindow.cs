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
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Conduite"))
        {
            if (go.scene == scene)
            {
                go.GetComponent<conduite_script03>()._handler = t._osc;
                break;
            }
        }
    }

    void OnPelleteuseLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnPelleteuseLoaded;

        sceneLoader t = (sceneLoader)target;
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Conduite"))
        {
            if (go.scene == scene)
            {
                go.GetComponent<pelleteuse_conduite_script03>()._handler = t._osc;
                break;
            }
        }
    }
}
