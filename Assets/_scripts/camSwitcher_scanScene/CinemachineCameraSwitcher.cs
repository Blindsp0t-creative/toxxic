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

    [Tooltip("Volume de la piste audio de la video. N'agit qu'en mode de sortie " +
             "Direct, le seul ou le VideoPlayer gere lui-meme le volume.")]
    [Range(0f, 1f)]
    public float _videoVolume = 1f;

    [Tooltip("Au stop, revenir a la premiere image pour que le top suivant " +
             "reparte du debut — c'est le comportement de l'ancien Stop(). " +
             "Decoche pour reprendre la lecture la ou elle s'est arretee.")]
    public bool _videoRewindOnStop = true;

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
        SetupVideo();

        blackOutSphere.SetActive(false);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (_player != null)
        {
            _player.prepareCompleted -= OnVideoPrepared;
            _player.loopPointReached -= OnVideoEnded;
        }
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
        // sinon l'exception coupe Start() avant SetupVideo() / blackOutSphere.
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


    // ─────────────────────────────────────────────
    //  VIDEO
    // ─────────────────────────────────────────────
    //
    // Le son de la video manquait un lancement sur deux. Cause : l'ancien
    // toggleVideo enchainait Stop() puis Play(). Stop() remet le player a
    // l'etat non prepare et libere le fournisseur d'echantillons audio ; le
    // Play() suivant relance une preparation asynchrone et, waitForFirstFrame
    // etant a false, l'image part des que le decodeur est pret — parfois avant
    // que la piste audio soit rearmee. Unity exige que audioOutputMode,
    // controlledAudioTrackCount et EnableAudioTrack soient en place AVANT la
    // fin de la preparation : reglees apres, elles sont ignorees jusqu'a la
    // preparation suivante.
    //
    // Donc : on prepare une fois au Start en armant l'audio avant le Prepare,
    // et le top ne fait plus que Play / Pause. Le decodeur et la piste audio
    // restent vivants, la course disparait.

    // Un top peut arriver pendant la preparation, ou isPlaying est encore
    // false : ce drapeau evite d'empiler deux preparations.
    private bool _videoPlayPending;

    void SetupVideo()
    {
        if (_player == null)
        {
            Debug.LogWarning("[CameraSwitcher] _player non assigne : pas de video.");
            return;
        }

        // L'ancien Start() faisait Stop(), qui avait pour effet de bord de
        // vider la RenderTexture — d'ou le quad noir avant le premier top.
        // Sans Stop(), la RT garde son contenu non initialise (quad blanc) :
        // on l'efface nous-memes, ce qui est de toute facon plus deterministe
        // que de dependre d'un effet de bord.
        clearVideoTexture();

        ArmVideoAudio();

        // Un rechargement additif de la scene rejoue Start() : on se desabonne
        // avant de se reabonner pour ne pas empiler les handlers.
        _player.prepareCompleted -= OnVideoPrepared;
        _player.prepareCompleted += OnVideoPrepared;
        _player.loopPointReached -= OnVideoEnded;
        _player.loopPointReached += OnVideoEnded;

        _videoPlayPending = false;
        _player.Prepare();
    }

    /// <summary>
    /// Remet a noir opaque la RenderTexture affichee par le quad de la TV.
    /// Utilisable aussi depuis un UnityEvent ou un binding Kontrols si tu veux
    /// un noir franc au stop plutot que la premiere image du clip.
    /// </summary>
    [ContextMenu("Clear Video Texture")]
    public void clearVideoTexture()
    {
        if (_player == null) return;

        var rt = _player.targetTexture;
        if (rt == null) return;   // renderMode != RenderTexture : rien a effacer

        // RenderTexture.active cree la RT si elle ne l'est pas encore.
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.black);   // alpha a 1 : la RT a un canal alpha
        RenderTexture.active = previous;
    }

    // A appeler avant chaque Prepare(), jamais apres.
    void ArmVideoAudio()
    {
        // audioTrackCount n'est fiable qu'une fois le player prepare ; le clip,
        // lui, porte l'info depuis l'import.
        ushort tracks = _player.clip != null ? _player.clip.audioTrackCount : (ushort)0;
        if (tracks == 0)
        {
            Debug.LogWarning("[CameraSwitcher] Le clip video '" +
                             (_player.clip != null ? _player.clip.name : "<aucun>") +
                             "' n'a aucune piste audio : la video sera muette.");
            return;
        }

        _player.controlledAudioTrackCount = 1;   // avant tout acces a la piste 0
        _player.EnableAudioTrack(0, true);

        switch (_player.audioOutputMode)
        {
            case VideoAudioOutputMode.Direct:
                _player.SetDirectAudioMute(0, false);
                _player.SetDirectAudioVolume(0, _videoVolume);
                break;

            case VideoAudioOutputMode.AudioSource:
                if (_player.GetTargetAudioSource(0) == null)
                    Debug.LogWarning("[CameraSwitcher] audioOutputMode = AudioSource " +
                                     "mais aucun AudioSource n'est assigne sur le " +
                                     "VideoPlayer : la video sera muette.");
                break;

            case VideoAudioOutputMode.None:
                Debug.LogWarning("[CameraSwitcher] audioOutputMode = None sur le " +
                                 "VideoPlayer : la video sera muette.");
                break;
        }
    }

    void OnVideoPrepared(VideoPlayer source)
    {
        // La preparation du Start ne doit pas lancer la lecture : seule celle
        // declenchee par un top (donc avec _videoPlayPending) enchaine un Play.
        if (!_videoPlayPending) return;

        _videoPlayPending = false;
        source.Play();
        Debug.Log("[CameraSwitcher] Video : play (apres preparation)");
    }

    // Sans looping, le player s'arrete sur la derniere image en restant pret :
    // un Play() y repartirait de la fin et se terminerait aussitot. On rembobine
    // pour que le top suivant reparte du debut.
    void OnVideoEnded(VideoPlayer source)
    {
        if (_videoRewindOnStop)
            source.frame = 0;
    }

    /// <summary>
    /// Top video. Cible du bouton "Top Video TV" de la KontrolSurface
    /// (binding Kontrols id "toggleVideoTV" → cette methode : ne pas renommer).
    /// </summary>
    public void toggleVideo()
    {
        if (_player == null)
        {
            Debug.LogWarning("[CameraSwitcher] _player non assigne : top video ignore.");
            return;
        }

        if (_player.isPlaying)
        {
            // Pause et pas Stop : garde le decodeur et la piste audio en vie.
            // Le rembobinage reproduit le comportement visible du Stop (le top
            // suivant repart du debut) sans la re-preparation qui coutait le son.
            _player.Pause();
            if (_videoRewindOnStop)
                _player.frame = 0;

            Debug.Log("[CameraSwitcher] Video : stop");
            return;
        }

        if (_videoPlayPending)
        {
            Debug.Log("[CameraSwitcher] Video : preparation en cours, top ignore.");
            return;
        }

        if (_player.isPrepared)
        {
            _player.Play();
            Debug.Log("[CameraSwitcher] Video : play");
            return;
        }

        // Pas encore pret : top tres tot apres le chargement, ou clip change
        // entre-temps. OnVideoPrepared enchainera le Play.
        _videoPlayPending = true;
        ArmVideoAudio();
        _player.Prepare();
        Debug.Log("[CameraSwitcher] Video : preparation...");
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