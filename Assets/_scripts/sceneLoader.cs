using UnityEngine;
using UnityEngine.SceneManagement;

public class sceneLoader : MonoBehaviour
{
    public OSC _osc;

    public void LoadSceneAdditive(string sceneName)
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void UnloadScene(string sceneName)
    {
        SceneManager.UnloadSceneAsync(sceneName);
    }



    public void loadStripClub()
    {
        Scene sceneP = SceneManager.GetSceneByName("PELLETEUSE_V1");
        if (sceneP.isLoaded)
            SceneManager.UnloadSceneAsync("PELLETEUSE_V1");

        Scene sceneR = SceneManager.GetSceneByName("SCAN_V1");
        if (sceneR.isLoaded)
            SceneManager.UnloadSceneAsync("SCAN_V1");

        Scene current = SceneManager.GetSceneByName("CLUB_V1");
        if(current.isLoaded)
            return;
        else
        {
            SceneManager.sceneLoaded += OnStripClubLoaded;
            SceneManager.LoadScene("CLUB_V1", LoadSceneMode.Additive);
        }


    }

    public void loadPelleteuse()
    {
        Scene sceneS = SceneManager.GetSceneByName("CLUB_V1");
        if (sceneS.isLoaded)
            SceneManager.UnloadSceneAsync("CLUB_V1");

        Scene sceneR = SceneManager.GetSceneByName("SCAN_V1");
        if (sceneR.isLoaded)
            SceneManager.UnloadSceneAsync("SCAN_V1");

        Scene current = SceneManager.GetSceneByName("PELLETEUSE_V1");
        if (current.isLoaded)
            return;
        else
        {
            SceneManager.sceneLoaded += OnPelleteuseLoaded;
            SceneManager.LoadScene("PELLETEUSE_V1", LoadSceneMode.Additive);
        }
    }

    public void loadRainbowRoad()
    {
        Scene sceneS = SceneManager.GetSceneByName("CLUB_V1");
        if (sceneS.isLoaded)
            SceneManager.UnloadSceneAsync("CLUB_V1");

        Scene sceneP = SceneManager.GetSceneByName("PELLETEUSE_V1");
        if (sceneP.isLoaded)
            SceneManager.UnloadSceneAsync("PELLETEUSE_V1");


        Scene current = SceneManager.GetSceneByName("SCAN_V1");
        if (current.isLoaded)
            return;
        else
        {
            SceneManager.sceneLoaded += OnPelleteuseLoaded;
            SceneManager.LoadScene("SCAN_V1", LoadSceneMode.Additive);
        }
    }

    void OnStripClubLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnStripClubLoaded;

        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Conduite"))
        {
            if (go.scene == scene)
            {
                go.GetComponent<conduite_script03>()._handler = _osc;
                break;
            }
        }
    }

    void OnPelleteuseLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnPelleteuseLoaded;

        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Conduite"))
        {
            if (go.scene == scene)
            {
                go.GetComponent<pelleteuse_conduite_script03>()._handler = _osc;
                break;
            }
        }
    }
}
