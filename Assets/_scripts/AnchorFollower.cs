using System.Collections;
using UnityEngine;

/// <summary>
/// Colle cet objet a son ancre (voir PresetAnchor). A poser sur l'avatar Rokoko
/// et sur le marqueur VR.
///
/// Deux regimes, distingues par l'amplitude du deplacement de l'ancre :
///   • petits deplacements — tu deplaces l'ancre a la main dans la Scene view :
///     suivi direct, image par image, pour placer a vue ;
///   • saut — un rappel de preset reecrit la pose d'un coup : transition avec
///     ease-in/out sur 'transitionDuration'.
///
/// Remplace les tableaux de placements indexes par la camera : la pose ne vient
/// plus de placements[currentIndex] mais du preset, via l'ancre.
/// </summary>
public class AnchorFollower : MonoBehaviour
{
    [Tooltip("L'ancre a suivre. En general un GameObject vide portant PresetAnchor.")]
    public Transform anchor;

    [Tooltip("Suivre aussi la rotation de l'ancre. Decocher pour un simple point " +
             "de repere au sol, comme le marqueur VR.")]
    public bool followRotation = true;

    [Tooltip("Duree de la transition quand l'ancre saute, en secondes. 0 = instantane.")]
    public float transitionDuration = 2f;

    [Header("Detection du saut")]
    [Tooltip("Deplacement de l'ancre sur une frame, en metres, au-dela duquel on " +
             "considere un saut (rappel de preset) plutot qu'un drag a la main.")]
    public float jumpThreshold = 0.05f;

    [Tooltip("Idem pour la rotation, en degres.")]
    public float jumpAngleThreshold = 5f;

    [Header("Accrochage dynamique")]
    [Tooltip("Non nul : on suit CET objet image par image, en ignorant l'ancre et " +
             "les presets. C'est ce qu'il faut pour accrocher l'avatar a un objet " +
             "qui bouge — la plateforme qui monte, par exemple : un marqueur enfant " +
             "de l'Elevator, et l'avatar monte avec. Pose et retire par la conduite. " +
             "Au detachement, on revient a l'ancre en transition, pas en teleportation.")]
    public Transform attachTo;

    private Vector3    _lastAnchorPos;
    private Quaternion _lastAnchorRot;
    private bool       _init;
    private bool       _wasAttached;
    private Coroutine  _co;

    /// <summary>Accrocher l'avatar a un objet mobile (suivi image par image).</summary>
    public void Attach(Transform target) => attachTo = target;

    /// <summary>Relacher : retour a l'ancre, en transition.</summary>
    public void Detach() => attachTo = null;

    void Update()
    {
        // L'accrochage dynamique est prioritaire sur l'ancre et sur les presets :
        // tant qu'il est pose, l'avatar est solidaire de l'objet, sans lissage,
        // exactement comme l'ancien placeTrendmillAvatar recopiait la position
        // du marqueur a chaque frame.
        if (attachTo != null)
        {
            if (_co != null) { StopCoroutine(_co); _co = null; }
            Snap(attachTo.position, attachTo.rotation);
            _wasAttached = true;

            // On garde l'ancre a l'oeil pour ne pas prendre son immobilite pour
            // une absence de saut au moment du detachement.
            if (anchor != null) { _lastAnchorPos = anchor.position; _lastAnchorRot = anchor.rotation; }
            return;
        }

        if (anchor == null) { _wasAttached = false; return; }

        Vector3    aPos = anchor.position;
        Quaternion aRot = anchor.rotation;

        // Premiere frame : on se pose sur l'ancre sans transition.
        if (!_init)
        {
            _init = true;
            _wasAttached   = false;
            _lastAnchorPos = aPos;
            _lastAnchorRot = aRot;
            Snap(aPos, aRot);
            return;
        }

        // On vient d'etre relache : l'ancre n'a pas bouge, mais l'avatar est reste
        // sur la plateforme. On force la transition, sinon il se teleporterait.
        bool justDetached = _wasAttached;
        _wasAttached = false;

        bool jumped = justDetached
                   || Vector3.Distance(aPos, _lastAnchorPos) > jumpThreshold
                   || (followRotation && Quaternion.Angle(aRot, _lastAnchorRot) > jumpAngleThreshold);

        _lastAnchorPos = aPos;
        _lastAnchorRot = aRot;

        if (jumped && transitionDuration > 0f && Application.isPlaying)
        {
            // Couper la transition en cours : LerpPose relit la pose courante au
            // demarrage, donc on repart d'ou on en est.
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(LerpPose(aPos, aRot, transitionDuration));
            return;
        }

        // Transition en cours : c'est la coroutine qui ecrit le transform.
        if (_co != null) return;

        Snap(aPos, aRot);
    }

    /// <summary>Se poser immediatement sur l'ancre, sans transition.</summary>
    public void SnapToAnchor()
    {
        if (anchor == null) return;
        if (_co != null) { StopCoroutine(_co); _co = null; }
        Snap(anchor.position, anchor.rotation);
    }

    void Snap(Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        if (followRotation) transform.rotation = rot;
    }

    IEnumerator LerpPose(Vector3 endPos, Quaternion endRot, float duration)
    {
        Vector3    startPos = transform.position;
        Quaternion startRot = transform.rotation;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // SmoothStep : ease-in/out, depart et arrivee amortis.
            float e = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            transform.position = Vector3.Lerp(startPos, endPos, e);
            if (followRotation) transform.rotation = Quaternion.Slerp(startRot, endRot, e);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Snap(endPos, endRot);
        _co = null;
    }
}
