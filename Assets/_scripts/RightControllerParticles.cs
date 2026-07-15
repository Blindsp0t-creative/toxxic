using UnityEngine;

/// <summary>
/// Attache un ParticleSystem au contrôleur droit du Quest 3 (Meta XR SDK)
/// et déclenche l'émission lors de l'appui sur le bouton A.
/// </summary>
public class RightControllerParticles : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Le ParticleSystem déjà configuré à émettre.")]
    [SerializeField] private ParticleSystem particles;

    [Tooltip("Ancre du contrôleur droit (RightControllerAnchor de l'OVRCameraRig). " +
             "Laisser vide pour la retrouver automatiquement.")]
    [SerializeField] private Transform rightControllerAnchor;

    [Header("Placement")]
    [Tooltip("Décalage local par rapport au contrôleur (ex: (0, 0, 0.05) pour sortir " +
             "les particules légèrement vers l'avant).")]
    [SerializeField] private Vector3 localPositionOffset = Vector3.zero;

    [SerializeField] private Vector3 localEulerRotation = Vector3.zero;

    [Header("Comportement d'émission")]
    [Tooltip("Vrai : émet tant que A est maintenu. Faux : un seul burst à l'appui.")]
    [SerializeField] private bool emitWhileHeld = true;

    [Tooltip("Nombre de particules par burst (mode 'un seul burst').")]
    [SerializeField] private int burstCount = 30;

    private void Awake()
    {
        // Retrouve automatiquement l'ancre du contrôleur droit si non assignée.
        // (API Unity 6 : FindFirstObjectByType remplace FindObjectOfType, déprécié.)
        if (rightControllerAnchor == null)
        {
            OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
            if (rig != null)
                rightControllerAnchor = rig.rightControllerAnchor;
        }

        // Attache le système de particules au contrôleur droit.
        if (rightControllerAnchor != null && particles != null)
        {
            Transform t = particles.transform;
            t.SetParent(rightControllerAnchor, worldPositionStays: false);
            t.localPosition = localPositionOffset;
            t.localRotation = Quaternion.Euler(localEulerRotation);
        }
        else
        {
            Debug.LogWarning("[RightControllerParticles] ParticleSystem ou ancre du contrôleur droit manquant.");
        }

        // On s'assure que rien n'est émis au démarrage
        // (utile si 'Play On Awake' est coché sur le ParticleSystem).
        if (particles != null)
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void Update()
    {
        if (particles == null) return;

        // Bouton A du contrôleur droit.
        if (OVRInput.GetDown(OVRInput.RawButton.A))
        {
            if (emitWhileHeld)
                particles.Play();
            else
                particles.Emit(burstCount);
        }

        // Arrêt de l'émission au relâchement (uniquement en mode maintenu).
        if (emitWhileHeld && OVRInput.GetUp(OVRInput.RawButton.A))
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}