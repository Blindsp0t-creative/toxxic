using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class CinemachineCameraSwitcher : MonoBehaviour
{
    [Header("Cameras renseignées manuellement (15-20)")]
    public List<CinemachineVirtualCamera> manualCameras = new List<CinemachineVirtualCamera>();

    [Header("Contrôles (Inspector)")]
    [Range(0, 30)]
    public int currentIndex = 0;

    [Header("OBSOLETE — placements pilotes par les presets")]
    [Tooltip("Le placement de Jen et du marqueur VR ne vient plus d'un tableau " +
             "indexe par la camera, mais d'une ancre (PresetAnchor) suivie par un " +
             "AnchorFollower pose sur chaque cible, dont la pose est capturee par " +
             "le preset. Ces champs sont conserves le temps de la transition et ne " +
             "sont plus lus par ce script.")]
    public GameObject _avatarJen;
    public GameObject _vrMarker;

    public GameObject[] _placementsJen = new GameObject[20];
    public GameObject[] _placementsDenis = new GameObject[20];

    public float[] _durations = new float[20];

    public VideoPlayer _player;

    private CinemachineVirtualCamera _vrCloseUpCamera;
    private List<CinemachineVirtualCamera> _allCameras = new List<CinemachineVirtualCamera>();

    // Dernier index reellement applique aux Priority Cinemachine. Sert au
    // poll de Update() : un preset (ou un script tiers) ecrit currentIndex
    // directement dans le champ serialise, sans passer par ActivateCamera —
    // sans ce suivi la camera ne changerait pas au rappel du preset.
    private int _appliedIndex = -1;

    private const int PRIORITY_ACTIVE = 20;
    private const int PRIORITY_INACTIVE = 0;

    public GameObject blackOutSphere;

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        FindVRCloseUpCameraInAllScenes();
        RebuildCameraList();
        ActivateCamera(currentIndex, animate: false);

        //video player
        _player.Stop();
        _player.Pause();

        blackOutSphere.SetActive(false);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (currentIndex != _appliedIndex)
            ActivateCamera(currentIndex, animate: true);
    }

    // ─────────────────────────────────────────────
    //  SCENE ASYNC : détection de la caméra taguée
    // ─────────────────────────────────────────────

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Additive) return;
        StartCoroutine(FindVRCameraInScene(scene));
    }

    IEnumerator FindVRCameraInScene(Scene scene)
    {
        yield return null;

        foreach (var root in scene.GetRootGameObjects())
        {
            var cam = FindCameraWithTagInChildren(root, "GROS_PLAN_VR");
            if (cam != null)
            {
                _vrCloseUpCamera = cam;
                Debug.Log($"[CameraSwitcher] Camera VR trouvée dans la scène '{scene.name}' : {cam.name}");
                RebuildCameraList();
                ActivateCamera(currentIndex, animate: false);
                yield break;
            }
        }

        Debug.LogWarning($"[CameraSwitcher] Aucun objet tagué 'GROS_PLAN_VR' trouvé dans '{scene.name}'.");
    }

    void FindVRCloseUpCameraInAllScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            foreach (var root in scene.GetRootGameObjects())
            {
                var cam = FindCameraWithTagInChildren(root, "GROS_PLAN_VR");
                if (cam != null)
                {
                    _vrCloseUpCamera = cam;
                    return;
                }
            }
        }
    }

    CinemachineVirtualCamera FindCameraWithTagInChildren(GameObject root, string tag)
    {
        if (root.CompareTag(tag))
        {
            var cam = root.GetComponent<CinemachineVirtualCamera>();
            if (cam != null) return cam;
        }
        foreach (Transform child in root.transform)
        {
            var result = FindCameraWithTagInChildren(child.gameObject, tag);
            if (result != null) return result;
        }
        return null;
    }

    // ─────────────────────────────────────────────
    //  CONSTRUCTION DE LA LISTE COMPLÈTE
    // ─────────────────────────────────────────────

    void RebuildCameraList()
    {
        _allCameras.Clear();
        _allCameras.AddRange(manualCameras);

        if (_vrCloseUpCamera != null && !_allCameras.Contains(_vrCloseUpCamera))
            _allCameras.Add(_vrCloseUpCamera);

        currentIndex  = Mathf.Clamp(currentIndex, 0, Mathf.Max(0, _allCameras.Count - 1));
        _appliedIndex = -1;   // la liste a change : forcer une re-application
    }

    // ─────────────────────────────────────────────
    //  ACTIVATION
    // ─────────────────────────────────────────────

    // animate : false pour un etat initial (Start, scene additive fraichement
    // chargee) ou l'on veut l'etat final tout de suite ; true pour un top, un
    // bouton ou un rappel de preset, ou l'on veut la transition.
    void ActivateCamera(int index, bool animate)
    {
        if (_allCameras.Count == 0) return;

        currentIndex  = Mathf.Clamp(index, 0, _allCameras.Count - 1);
        _appliedIndex = currentIndex;

        // Le placement de Jen et du marqueur VR ne suit plus la camera : il vient
        // des ancres, capturees par le preset. Un preset peut donc associer
        // n'importe quelle pose a n'importe quelle camera.

        for (int i = 0; i < _allCameras.Count; i++)
        {
            if (_allCameras[i] == null) continue;
            _allCameras[i].Priority = (i == currentIndex) ? PRIORITY_ACTIVE : PRIORITY_INACTIVE;
        }

        // Le slot peut etre vide (aucune vcam assignee) : on log sans deferencer,
        // sinon l'exception coupe Start() avant _player.Stop() / blackOutSphere.
        var active = _allCameras[currentIndex];
        Debug.Log($"[CameraSwitcher] Camera active : [{currentIndex}] {(active != null ? active.name : "<slot vide>")}");
    }

    // ─────────────────────────────────────────────
    //  API PUBLIQUE
    // ─────────────────────────────────────────────

    /// <summary>Appelé par le Slider UI. Valeur : 0 → (count-1)</summary>
    public void OnSliderChanged(float value)
    {
        ActivateCamera(Mathf.RoundToInt(value), animate: true);
    }

    /// <summary>Bouton "Next" — aussi accessible via clic droit sur le composant</summary>
    [ContextMenu("Next Camera")]
    public void NextCamera()
    {
        if (_allCameras.Count == 0) return;
        ActivateCamera((currentIndex + 1) % _allCameras.Count, animate: true);
    }

    // Conservees parce qu'un UnityEvent de scene peut encore les referencer :
    // les supprimer casserait le binding en silence. Le placement est desormais
    // gere par AnchorFollower, pose sur Jen et sur le marqueur.
    public void moveAvatar()    => WarnPlacementMoved();
    public void moveVR_marker() => WarnPlacementMoved();

    bool _placementWarned;
    void WarnPlacementMoved()
    {
        if (_placementWarned) return;
        _placementWarned = true;
        Debug.LogWarning("[CameraSwitcher] moveAvatar/moveVR_marker ne font plus rien : " +
                         "le placement est pilote par les ancres (PresetAnchor + AnchorFollower). " +
                         "Retire cet appel du UnityEvent qui le declenche.");
    }


    public void toggleVideo()
    {
        if (_player.isPlaying)
        {
            Debug.Log("is playing");
            _player.Stop();
            _player.Pause();
        }
        else if (_player.isPlaying == false)
        {
            _player.Play();
        }
    }


    public void blackOut(bool value)
    {
        if (value == true)
        {
            blackOutSphere.SetActive(true);
        }
        else if (value == false)
        {
            blackOutSphere.SetActive(false);
        }
    }


    /// <summary>Bouton "Previous" — aussi accessible via clic droit sur le composant</summary>
    [ContextMenu("Previous Camera")]
    public void PreviousCamera()
    {
        if (_allCameras.Count == 0) return;
        ActivateCamera((currentIndex - 1 + _allCameras.Count) % _allCameras.Count, animate: true);
    }

    /// <summary>Nombre total de cameras disponibles</summary>
    public int CameraCount => _allCameras.Count;

    /// <summary>Nom de la camera actuellement active</summary>
    public string CurrentCameraName =>
        (_allCameras.Count > 0 && _allCameras[currentIndex] != null)
            ? _allCameras[currentIndex].name : "—";
}