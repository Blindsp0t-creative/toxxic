using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// OBSOLETE — remplace par AnchorFollower, qui suit une PresetAnchor dont la pose
/// est capturee par le systeme de presets. Conserve le temps de la transition,
/// pour ne pas perdre les placements deja renseignes dans l'Inspector.
///
/// Se met automatiquement en retrait si un AnchorFollower est pose sur le meme
/// GameObject : sans ca les deux composants se disputeraient le transform a
/// chaque frame, avec un resultat dependant de l'ordre d'execution.
/// </summary>
public class placeTrendmillAvatar : MonoBehaviour
{
    [Header("Placements (position ET rotation)")]
    public GameObject[] places;

    [Tooltip("Duree de transition vers places[i], en secondes. 0 = instantane. " +
             "Tableau parallele a 'places' ; au-dela de sa longueur, 0.")]
    public float[] durations;

    [Tooltip("Index du placement courant. Ecrit par la conduite (sceneNB) ou " +
             "par un rappel de preset.")]
    public int activePlace;

    // Dernier index reellement applique. Sert au poll de Update() : un preset
    // ecrit activePlace directement dans le champ serialise, sans passer par
    // une methode — sans ce suivi l'avatar ne bougerait pas au rappel.
    // -1 = rien d'applique encore, la premiere application se fait sans
    // transition (etat initial de la scene).
    private int _appliedPlace = -1;

    private Coroutine _co;

    // Non nul = un AnchorFollower pilote deja ce transform, on ne fait rien.
    private AnchorFollower _follower;

    void Awake()
    {
        _follower = GetComponent<AnchorFollower>();
        if (_follower != null)
            Debug.Log($"[placeTrendmillAvatar] AnchorFollower present sur '{name}' : " +
                      "placement laisse a l'ancre, ce composant reste inactif.");
    }

    void Update()
    {
        if (_follower != null) return;

        // Changement d'index (conduite ou rappel de preset) : on lance la transition.
        if (activePlace != _appliedPlace)
        {
            bool animate = _appliedPlace >= 0;   // la toute premiere pose est un snap
            _appliedPlace = activePlace;
            MoveToPlace(animate);
            return;
        }

        // Transition en cours : c'est la coroutine qui ecrit le transform.
        if (_co != null) return;

        // Au repos, on colle au marker a chaque frame, comme avant. Indispensable :
        // la conduite deplace le marker en live (slider d'elevation), et l'avatar
        // doit suivre sans attendre un changement d'index.
        var placement = PlacementAt(places, activePlace);
        if (placement == null) return;

        transform.position = placement.transform.position;
        transform.rotation = placement.transform.rotation;
    }

    /// <summary>Rejoue le placement courant, avec transition.</summary>
    public void MoveToPlace() => MoveToPlace(true);

    void MoveToPlace(bool animate)
    {
        var placement = PlacementAt(places, activePlace);

        // Index hors bornes ou slot vide : on ne deplace rien plutot que de
        // lever une IndexOutOfRange ou une NullReference en plein show.
        if (placement == null)
        {
            Debug.LogWarning($"[placeTrendmillAvatar] Placement [{activePlace}] absent sur '{name}' — deplacement ignore.");
            return;
        }

        if (_co != null) StopCoroutine(_co);

        Vector3    endPos = placement.transform.position;
        Quaternion endRot = placement.transform.rotation;

        // Duree nulle, ou hors Play ou les coroutines ne tournent pas : on pose
        // directement l'etat final.
        if (!animate || DurationAt(activePlace) <= 0f || !Application.isPlaying)
        {
            transform.position = endPos;
            transform.rotation = endRot;
            _co = null;
            return;
        }

        _co = StartCoroutine(LerpPose(endPos, endRot, DurationAt(activePlace)));
    }

    static GameObject PlacementAt(GameObject[] arr, int i)
        => (arr != null && i >= 0 && i < arr.Length) ? arr[i] : null;

    float DurationAt(int i)
        => (durations != null && i >= 0 && i < durations.Length) ? durations[i] : 0f;

    IEnumerator LerpPose(Vector3 endPos, Quaternion endRot, float duration)
    {
        // Relu au demarrage : un nouveau top repart d'ou l'avatar en est,
        // pas de la position qu'il avait quand la transition precedente a commence.
        Vector3    startPos = transform.position;
        Quaternion startRot = transform.rotation;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // SmoothStep : ease-in/out, depart et arrivee amortis.
            float e = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            transform.position = Vector3.Lerp(startPos, endPos, e);
            transform.rotation = Quaternion.Slerp(startRot, endRot, e);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        transform.rotation = endRot;
        _co = null;
    }
}
