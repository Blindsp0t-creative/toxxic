using UnityEngine;

/// <summary>
/// Rend un avatar anime "jetable" par un coup de pied, sur un top de conduite.
///
/// Le Rigidbody et le collider vivent sur le ROOT de l'avatar — celui qui porte
/// l'Animator — et pas sur le SkinnedMeshRenderer : un mesh skinne se dessine a
/// partir de ses bones, deplacer le GameObject du renderer ne bouge rien a
/// l'ecran. La boucle d'animation continue donc de tourner pendant que le corps
/// entier part en vol : il reste allonge et il tourne sur lui-meme.
///
/// ARMEMENT — tant que la physique n'est pas armee le corps est kinematic : il
/// ne derive pas, il ne tombe pas a travers le decor, et un pied qui passe
/// dessus pendant le reste du spectacle ne le lance pas. C'est la conduite qui
/// ouvre la fenetre au top, via SetPhysics() — voir le champ
/// 'physiqueLayingAvatar' de conduite_script03_Silk1, assignable a un bouton ou
/// a un toggle Kontrols.
///
/// A poser sur "Laying BreathlessAvatar". Voir FootLauncher pour le pied qui
/// frappe, et le menu TOXXIC > Coup de pied pour le cablage automatique.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class LaunchableBody : MonoBehaviour
{
    [Header("Armement")]
    [SerializeField]
    [Tooltip("Physique armee. Desarme, le corps est kinematic et ignore les " +
             "coups de pied. Cochable a la main en Play pour essayer, mais en " +
             "spectacle c'est la conduite qui le fait au top.")]
    bool armed = false;

    [Tooltip("En desarmant, remettre le corps a sa pose de depart. Decoche : il " +
             "reste la ou il est tombe et se contente de se figer.")]
    public bool resetOnDisarm = true;

    [Header("Animation")]
    [Tooltip("L'Animator de l'avatar. Laisser vide : cherche sur cet objet. " +
             "Apply Root Motion est force a false, sinon l'Animator reecrit la " +
             "position du root a chaque frame et le Rigidbody ne peut rien faire.")]
    public Animator animator;

    [Tooltip("Couper l'Animator pendant le vol. L'avatar se fige dans sa pose " +
             "courante et part en vrille comme une planche. Decoche : la boucle " +
             "continue de jouer en l'air.")]
    public bool freezeAnimationInFlight = false;

    [Header("Retour au repos")]
    [Tooltip("Retour automatique a la pose de depart quand le corps s'est " +
             "immobilise. 0 = jamais, il reste ou il est tombe.")]
    public float autoResetDelay = 4f;

    [Tooltip("Vitesse en dessous de laquelle on considere le corps immobilise, " +
             "en m/s.")]
    public float settleSpeed = 0.15f;

    [Tooltip("Desarmer apres le retour a la pose de depart : il faut un nouveau " +
             "top de conduite pour relancer. C'est ce qu'on veut en spectacle.")]
    public bool disarmAfterReset = true;

    [Header("Securite")]
    [Tooltip("Plafond de la vitesse de depenetration. Evite que le corps soit " +
             "catapulte quand un collider de pied demarre deja a l'interieur.")]
    public float maxDepenetrationSpeed = 3f;

    [Tooltip("Sous cette altitude monde, le corps est remis a sa pose de depart. " +
             "Filet de securite quand il n'y a pas de sol de collision.")]
    public float killY = -100f;

    Rigidbody  _rb;
    Vector3    _restPos;
    Quaternion _restRot;
    bool       _poseStored;
    bool       _appliedArmed;
    bool       _inFlight;
    float      _settleTimer;

    // La conduite tourne en ExecuteInEditMode et peut donc appeler
    // SetPhysics() hors Play, avant que Awake() ait eu lieu.
    Rigidbody Rb
    {
        get
        {
            if (_rb == null) _rb = GetComponent<Rigidbody>();
            return _rb;
        }
    }

    /// <summary>La physique est-elle armee ?</summary>
    public bool Armed { get { return armed; } }

    /// <summary>Le corps est-il en vol (ou en train de retomber) ?</summary>
    public bool InFlight { get { return _inFlight; } }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponent<Animator>();

        _restPos    = transform.position;
        _restRot    = transform.rotation;
        _poseStored = true;

        _rb.maxDepenetrationVelocity = maxDepenetrationSpeed;
        _rb.collisionDetectionMode   = CollisionDetectionMode.ContinuousSpeculative;
        _rb.interpolation            = RigidbodyInterpolation.Interpolate;

        if (animator != null && animator.applyRootMotion)
        {
            // Root motion + Rigidbody sur le meme objet : l'Animator gagne et
            // le corps ne decolle jamais.
            animator.applyRootMotion = false;
        }

        ApplyArmed();
    }

    // --------------------------------------------------------------- armement

    /// <summary>
    /// Ouvre ou ferme la fenetre physique. C'est le point d'entree de la
    /// conduite : arme = le corps est dynamique et peut se faire jeter en
    /// l'air, desarme = il est kinematic et plus rien ne le touche.
    /// </summary>
    public void SetPhysics(bool enable)
    {
        armed = enable;
        ApplyArmed();
    }

    /// <summary>Arme la physique. Pour un bouton.</summary>
    public void ArmPhysics()
    {
        SetPhysics(true);
    }

    /// <summary>Desarme la physique. Pour un bouton.</summary>
    public void DisarmPhysics()
    {
        SetPhysics(false);
    }

    /// <summary>Bascule l'etat. Pour un bouton unique.</summary>
    public void TogglePhysics()
    {
        SetPhysics(!armed);
    }

    void ApplyArmed()
    {
        _appliedArmed = armed;

        if (armed)
        {
            Rb.isKinematic = false;
            Rb.WakeUp();
            _settleTimer = 0f;
            return;
        }

        if (resetOnDisarm)
        {
            ResetToRest();
            return;
        }

        StopMotion();
        Rb.isKinematic = true;

        if (animator != null) animator.enabled = true;

        _inFlight    = false;
        _settleTimer = 0f;
    }

    void StopMotion()
    {
        // Inutile sur un corps kinematic, et Unity n'aime pas qu'on lui ecrive
        // une vitesse dans cet etat.
        if (Rb.isKinematic) return;

        Rb.linearVelocity  = Vector3.zero;
        Rb.angularVelocity = Vector3.zero;
    }

    // ------------------------------------------------------------------- vol

    /// <summary>
    /// Envoie le corps en l'air. 'impulse' est une impulsion en N.s appliquee
    /// au point 'worldPoint' — hors du centre de masse, ce qui met aussi le
    /// corps en rotation. Sans armement, l'appel est ignore.
    /// </summary>
    public void Launch(Vector3 impulse, Vector3 worldPoint)
    {
        if (!armed) return;

        if (Rb.isKinematic) Rb.isKinematic = false;
        Rb.WakeUp();
        Rb.AddForceAtPosition(impulse, worldPoint, ForceMode.Impulse);

        if (freezeAnimationInFlight && animator != null) animator.enabled = false;

        _inFlight    = true;
        _settleTimer = 0f;
    }

    /// <summary>Remet le corps a la pose qu'il avait au demarrage de la scene.</summary>
    public void ResetToRest()
    {
        StopMotion();

        // Hors Play, Awake() n'a pas tourne et la pose de repos n'est pas
        // connue : surtout ne pas teleporter l'avatar a l'origine.
        if (_poseStored) transform.SetPositionAndRotation(_restPos, _restRot);

        if (animator != null) animator.enabled = true;

        _inFlight    = false;
        _settleTimer = 0f;

        if (disarmAfterReset) armed = false;

        _appliedArmed  = armed;
        Rb.isKinematic = !armed;
    }

    /// <summary>Memorise la pose actuelle comme nouvelle pose de repos.</summary>
    public void StoreRestPose()
    {
        _restPos    = transform.position;
        _restRot    = transform.rotation;
        _poseStored = true;
    }

    void FixedUpdate()
    {
        // La case a cocher a pu etre basculee a la main dans l'Inspector en
        // Play : on reconcilie ici, par le meme chemin que la conduite.
        if (_appliedArmed != armed) ApplyArmed();

        if (!armed || !_inFlight) return;

        if (transform.position.y < killY)
        {
            ResetToRest();
            return;
        }

        if (autoResetDelay <= 0f) return;

        bool still = _rb.linearVelocity.magnitude  < settleSpeed
                  && _rb.angularVelocity.magnitude < settleSpeed * 4f;

        _settleTimer = still ? _settleTimer + Time.fixedDeltaTime : 0f;
        if (_settleTimer >= autoResetDelay) ResetToRest();
    }

    [ContextMenu("Test : armer et donner un coup de pied")]
    void TestKick()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[LaunchableBody] Le test ne marche qu'en Play.", this);
            return;
        }
        if (_rb == null) _rb = GetComponent<Rigidbody>();

        SetPhysics(true);
        Launch(new Vector3(0f, 8f, 2f) * _rb.mass, transform.position);
    }
}
