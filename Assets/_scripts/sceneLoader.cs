using UnityEngine;
using UnityEngine.SceneManagement;

public class sceneLoader : MonoBehaviour
{
    public OSC _osc;

    public GameObject avatarStripClub;
    public GameObject avatarPelleteuse;
    public GameObject avatarRainbowRoad;

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

        unloadToxxicScene("PELLETEUSE_V1");
        unloadToxxicScene("SCAN_V1");

        loadToxxicScene("CLUB_V1");

        disableAvatars();
        if(avatarStripClub != null )
            avatarStripClub.SetActive(true);
    }

    public void loadPelleteuse()
    {

        unloadToxxicScene("CLUB_V1");
        unloadToxxicScene("SCAN_V1");

        loadToxxicScene("PELLETEUSE_V1");

        disableAvatars();
        if(avatarPelleteuse != null )
            avatarPelleteuse.SetActive(true);

    }

    public void loadRainbowRoad()
    {

        unloadToxxicScene("CLUB_V1");
        unloadToxxicScene("PELLETEUSE_V1");

        loadToxxicScene("SCAN_V1");

        disableAvatars();
        if(avatarRainbowRoad != null )
            avatarRainbowRoad.SetActive(true);

    }

    void OnStripClubLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnStripClubLoaded;
        /*
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Conduite"))
        {
            if (go.scene == scene)
            {
                go.GetComponent<conduite_script03>()._handler = _osc;
                break;
            }
        }
        */
    }

    void OnPelleteuseLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnPelleteuseLoaded;

        /*
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Conduite"))
        {
            if (go.scene == scene)
            {
                go.GetComponent<pelleteuse_conduite_script03>()._handler = _osc;
                break;
            }
        }
        */
    }

    public void unloadToxxicScene(string _name)
    {
        Scene sceneP = SceneManager.GetSceneByName(_name);
        if (sceneP.isLoaded)
            SceneManager.UnloadSceneAsync(_name);
    }

    public void loadToxxicScene(string _name)
    {
        Scene current = SceneManager.GetSceneByName(_name);
        if (current.isLoaded)
            return;
        else
        {
            SceneManager.sceneLoaded += OnStripClubLoaded;
            SceneManager.LoadScene(_name, LoadSceneMode.Additive);
        }
    }

    public void disableAvatars()
    {
        if(avatarStripClub != null)
            avatarStripClub.SetActive(false);

        if(avatarPelleteuse != null)
            avatarPelleteuse.SetActive(false);

        if(avatarRainbowRoad != null)
            avatarRainbowRoad.SetActive(false);
    }

}
