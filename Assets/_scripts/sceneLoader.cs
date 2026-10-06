using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class sceneLoader : MonoBehaviour
{
    public OSC _osc;

    public GameObject avatarStripClub;
    public GameObject avatarPelleteuse;
    public GameObject avatarRainbowRoad;

    public string sceneStripClub = "";
    public string scenePeleteuse = "";
    public string sceneRainbow = "";

    [Tooltip("Si la scene demandee est deja chargee : la decharger puis la recharger (repart a zero). Sinon le bouton ne fait rien.")]
    public bool reloadIfAlreadyLoaded = true;

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

        unloadToxxicScene(scenePeleteuse);
        unloadToxxicScene(sceneRainbow);

        loadToxxicScene(sceneStripClub);

        disableAvatars();
        if(avatarStripClub != null )
            avatarStripClub.SetActive(true);
    }

    public void loadPelleteuse()
    {

        unloadToxxicScene(sceneStripClub);
        unloadToxxicScene(sceneRainbow);

        loadToxxicScene(scenePeleteuse);

        disableAvatars();
        if(avatarPelleteuse != null )
            avatarPelleteuse.SetActive(true);

    }

    public void loadRainbowRoad()
    {

        unloadToxxicScene(sceneStripClub);
        unloadToxxicScene(scenePeleteuse);

        loadToxxicScene(sceneRainbow);

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
        if (string.IsNullOrEmpty(_name))
            return;

        Scene current = SceneManager.GetSceneByName(_name);

        if (current.isLoaded)
        {
            // deja chargee : soit on ne fait rien (ancien comportement),
            // soit on la recharge pour repartir d'un etat propre.
            if (!reloadIfAlreadyLoaded)
                return;

            StartCoroutine(reloadToxxicScene(_name));
            return;
        }

        SceneManager.sceneLoaded += OnStripClubLoaded;
        SceneManager.LoadScene(_name, LoadSceneMode.Additive);
    }

    // Decharge puis recharge la scene additive. L'unload est asynchrone :
    // on attend sa fin, sinon le LoadScene tombe sur une scene en cours
    // de destruction et on finit avec deux instances.
    private IEnumerator reloadToxxicScene(string _name)
    {
        AsyncOperation unload = SceneManager.UnloadSceneAsync(_name);

        while (unload != null && !unload.isDone)
            yield return null;

        SceneManager.sceneLoaded += OnStripClubLoaded;
        SceneManager.LoadScene(_name, LoadSceneMode.Additive);
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
