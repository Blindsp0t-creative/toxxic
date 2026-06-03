using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;

public class CinemachineCameraSwitcher : MonoBehaviour
{
    [Header("Cameras renseignées manuellement (15-20)")]
    public List<CinemachineVirtualCamera> manualCameras = new List<CinemachineVirtualCamera>();

    [Header("Camera chargée depuis une scène Async (tag GROS_PLAN_VR)")]
    private CinemachineVirtualCamera _vrCloseUpCamera;

    // Liste complète = manualCameras + _vrCloseUpCamera
    private List<CinemachineVirtualCamera> _allCameras = new List<CinemachineVirtualCamera>();
    private int _currentIndex = 0;

    // Priorité haute pour la camera active, basse pour les autres
    private const int PRIORITY_ACTIVE   = 20;
    private const int PRIORITY_INACTIVE = 0;

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────

    void Start()
    {
        // Écouter le chargement des scènes pour détecter la camera VR
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Chercher si la scène est déjà chargée (cas de rechargement)
        FindVRCloseUpCameraInAllScenes();

        RebuildCameraList();
        ActivateCamera(_currentIndex);
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
        // Ne chercher que dans les scènes chargées en Additive (Async)
        if (mode != LoadSceneMode.Additive) return;

        StartCoroutine(FindVRCameraInScene(scene));
    }

    IEnumerator FindVRCameraInScene(Scene scene)
    {
        // Attendre une frame que les objets soient initialisés
        yield return null;

        foreach (var root in scene.GetRootGameObjects())
        {
            var cam = FindCameraWithTagInChildren(root, "GROS_PLAN_VR");
            if (cam != null)
            {
                _vrCloseUpCamera = cam;
                Debug.Log($"[CameraSwitcher] Camera VR trouvée dans la scène '{scene.name}' : {cam.name}");
                RebuildCameraList();

                // Si c'était la dernière camera sélectionnée, la réactiver
                ActivateCamera(_currentIndex);
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

        // Clamp l'index au cas où la liste a changé de taille
        _currentIndex = Mathf.Clamp(_currentIndex, 0, _allCameras.Count - 1);
    }

    // ─────────────────────────────────────────────
    //  ACTIVATION
    // ─────────────────────────────────────────────

    void ActivateCamera(int index)
    {
        if (_allCameras.Count == 0) return;

        _currentIndex = Mathf.Clamp(index, 0, _allCameras.Count - 1);

        for (int i = 0; i < _allCameras.Count; i++)
        {
            if (_allCameras[i] == null) continue;
            var priorityComponent = _allCameras[i].GetComponent<CinemachineVirtualCamera>();
            _allCameras[i].Priority = (i == _currentIndex) ? PRIORITY_ACTIVE : PRIORITY_INACTIVE;
        }

        Debug.Log($"[CameraSwitcher] Camera active : [{_currentIndex}] {_allCameras[_currentIndex].name}");
    }

    // ─────────────────────────────────────────────
    //  API PUBLIQUE — appelée par Slider et Boutons
    // ─────────────────────────────────────────────

    /// <summary>Appelé par le Slider UI. Valeur : 0 → (count-1)</summary>
    public void OnSliderChanged(float value)
    {
        int index = Mathf.RoundToInt(value);
        ActivateCamera(index);
    }

    /// <summary>Appelé par le bouton "Next"</summary>
    public void NextCamera()
    {
        int next = (_currentIndex + 1) % _allCameras.Count;
        ActivateCamera(next);
    }

    /// <summary>Appelé par le bouton "Previous"</summary>
    public void PreviousCamera()
    {
        int prev = (_currentIndex - 1 + _allCameras.Count) % _allCameras.Count;
        ActivateCamera(prev);
    }

    /// <summary>Index actuel (pour synchroniser le Slider depuis l'UI)</summary>
    public int CurrentIndex => _currentIndex;

    /// <summary>Nombre total de cameras disponibles</summary>
    public int CameraCount => _allCameras.Count;

    /// <summary>Nom de la camera actuellement active</summary>
    public string CurrentCameraName =>
        (_allCameras.Count > 0 && _allCameras[_currentIndex] != null)
            ? _allCameras[_currentIndex].name
            : "—";
}