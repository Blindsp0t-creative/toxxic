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

        sceneLoader t = (sceneLoader)target;

        EditorGUILayout.Space(10);

        if (GUILayout.Button("stripClub", GUILayout.Height(40)))
        {
            Scene sceneP = SceneManager.GetSceneByName("pelleteuseNew");
            if (sceneP.isLoaded)
                SceneManager.UnloadSceneAsync("pelleteuseNew");

            SceneManager.sceneLoaded += OnStripClubLoaded;
            SceneManager.LoadScene("stripClubNew", LoadSceneMode.Additive);

            t._debugObjects.SetActive(false);
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("pelleteuse", GUILayout.Height(40)))
        {
            Scene sceneS = SceneManager.GetSceneByName("stripClubNew");
            if (sceneS.isLoaded)
                SceneManager.UnloadSceneAsync("stripClubNew");

            SceneManager.sceneLoaded += OnPelleteuseLoaded;
            SceneManager.LoadScene("pelleteuseNew", LoadSceneMode.Additive);

            t._debugObjects.SetActive(false);
        }
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
