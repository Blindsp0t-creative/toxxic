using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CinemachineCameraSwitcher : MonoBehaviour
{
    [Header("Cameras renseignées manuellement (15-20)")]
    public List<CinemachineVirtualCamera> manualCameras = new List<CinemachineVirtualCamera>();

    [Header("Contrôles (Inspector)")]
    [Range(0, 20)]
    public int currentIndex = 0;

    public GameObject _avatarJen;
    public GameObject _vrMarker;

    public GameObject[] _placementsJen = new GameObject[20];
    public GameObject[] _placementsDenis = new GameObject[20];

    public float[] _durations = new float[20];

    private CinemachineVirtualCamera _vrCloseUpCamera;
    private List<CinemachineVirtualCamera> _allCameras = new List<CinemachineVirtualCamera>();

    private const int PRIORITY_ACTIVE = 20;
    private const int PRIORITY_INACTIVE = 0;

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        FindVRCloseUpCameraInAllScenes();
        RebuildCameraList();
        ActivateCamera(currentIndex);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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
                ActivateCamera(currentIndex);
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

        currentIndex = Mathf.Clamp(currentIndex, 0, Mathf.Max(0, _allCameras.Count - 1));
    }

    // ─────────────────────────────────────────────
    //  ACTIVATION
    // ─────────────────────────────────────────────

    void ActivateCamera(int index)
    {
        if (_allCameras.Count == 0) return;

        currentIndex = Mathf.Clamp(index, 0, _allCameras.Count - 1);

        for (int i = 0; i < _allCameras.Count; i++)
        {
            if (_allCameras[i] == null) continue;
            _allCameras[i].Priority = (i == currentIndex) ? PRIORITY_ACTIVE : PRIORITY_INACTIVE;
        }

        Debug.Log($"[CameraSwitcher] Camera active : [{currentIndex}] {_allCameras[currentIndex].name}");
    }

    // ─────────────────────────────────────────────
    //  API PUBLIQUE
    // ─────────────────────────────────────────────

    /// <summary>Appelé par le Slider UI. Valeur : 0 → (count-1)</summary>
    public void OnSliderChanged(float value)
    {
        ActivateCamera(Mathf.RoundToInt(value));
    }

    /// <summary>Bouton "Next" — aussi accessible via clic droit sur le composant</summary>
    [ContextMenu("Next Camera")]
    public void NextCamera()
    {
        if (_allCameras.Count == 0) return;
        ActivateCamera((currentIndex + 1) % _allCameras.Count);

        moveAvatar();
        moveVR_marker();
    }

    public void moveAvatar()
    {
        StartCoroutine(LerpPosition(_avatarJen, _avatarJen.transform.position, _placementsJen[currentIndex].transform.position, _durations[currentIndex]));
    }

    public void moveVR_marker()
    {
        StartCoroutine(LerpPosition(_vrMarker, _vrMarker.transform.position, _placementsDenis[currentIndex].transform.position, _durations[currentIndex]));
    }

    IEnumerator LerpPosition(GameObject _object, Vector3 startPos, Vector3 endPos, float duration)
    {
        float elapsedTime = 0;
        while (elapsedTime < duration) {
            float t = elapsedTime / duration;
            _object.transform.position = Vector3.Lerp(startPos, endPos, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _object.transform.position = endPos;
    }

    /// <summary>Bouton "Previous" — aussi accessible via clic droit sur le composant</summary>
    [ContextMenu("Previous Camera")]
    public void PreviousCamera()
    {
        if (_allCameras.Count == 0) return;
        ActivateCamera((currentIndex - 1 + _allCameras.Count) % _allCameras.Count);

        moveAvatar();
        moveVR_marker();
    }

    /// <summary>Nombre total de cameras disponibles</summary>
    public int CameraCount => _allCameras.Count;

    /// <summary>Nom de la camera actuellement active</summary>
    public string CurrentCameraName =>
        (_allCameras.Count > 0 && _allCameras[currentIndex] != null)
            ? _allCameras[currentIndex].name : "—";
}