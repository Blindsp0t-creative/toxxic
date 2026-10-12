using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Met les pieds d'un avatar en etat d'envoyer un LaunchableBody en l'air.
/// A poser sur le root de l'avatar qui frappe — ici "Onyx_New_Unity".
///
/// Pourquoi un script et pas juste des colliders : les pieds d'Onyx sont pilotes
/// par Rokoko, qui ecrit les transforms des bones a la main. Pour PhysX ce sont
/// des colliders qui se teleportent, pas des corps en mouvement : ils peuvent
/// repousser un Rigidbody de quelques centimetres pour le desenchevetrer, mais
/// ils ne lui transmettent aucune vitesse. D'ou l'avatar qui ne bouge pas.
///
/// Ici on mesure nous-memes la vitesse monde de chaque pied entre deux pas de
/// physique, et on applique l'impulsion correspondante au corps touche. Le
/// rayon de detection est en metres monde, ce qui evite de se battre avec
/// l'echelle de l'armature (x100 dans le FBX, x1.8 sur le root : un collider de
/// 0.001 en local fait 18 cm a l'ecran).
/// </summary>
public class FootLauncher : MonoBehaviour
{
    [Header("Pieds")]
    [Tooltip("Les bones qui frappent. Laisser vide : les pieds du rig humanoide " +
             "sont trouves tout seuls via l'Animator.")]
    public Transform[] feet;

    [Tooltip("Rayon de detection autour de chaque pied, en metres monde.")]
    public float footRadius = 0.18f;

    [Tooltip("Decalage du centre de detection par rapport au bone, en metres, " +
             "dans le repere du pied. Le bone est a la cheville : dans ce rig le " +
             "+Y du bone pointe vers les orteils, donc 0.12 en Y vise le milieu " +
             "de la semelle. Active les gizmos pour verifier a l'oeil.")]
    public Vector3 footOffset = new Vector3(0f, 0.12f, 0f);

    [Header("Cibles")]
    [Tooltip("Layers fouilles. Par defaut tout.")]
    public LayerMask targetLayers = ~0;

    [Tooltip("Ne frapper que les objets qui portent LaunchableBody. Decoche pour " +
             "envoyer valser n'importe quel Rigidbody de la scene.")]
    public bool onlyLaunchableBodies = true;

    [Header("Coup de pied")]
    [Tooltip("Vitesse minimale du pied pour declencher, en m/s. En dessous on " +
             "ignore : un pied pose ne doit pas lancer le corps.")]
    public float minSpeed = 1.5f;

    [Tooltip("Plafond de la vitesse du pied prise en compte, en m/s. Evite qu'un " +
             "glitch de mocap envoie le corps en orbite.")]
    public float maxSpeed = 10f;

    [Tooltip("Multiplicateur de transfert. 1 = le corps part a la vitesse du " +
             "pied. Au-dela, coup de pied de cartoon.")]
    public float transfer = 1.8f;

    [Range(0f, 1f)]
    [Tooltip("Redressement de la direction vers le haut. 0 = on suit exactement " +
             "la direction du pied, 1 = tout droit en l'air.")]
    public float upwardBias = 0.45f;

    [Tooltip("Vitesse verticale ajoutee d'office, en m/s. C'est ce qui garantit " +
             "que le corps decolle meme sur un coup de pied rasant.")]
    public float extraLift = 2.5f;

    [Tooltip("Rotation ajoutee au decollage. 0 = seul le bras de levier du point " +
             "d'impact fait tourner le corps.")]
    public float spin = 0.35f;

    [Tooltip("Delai avant de pouvoir re-frapper la meme cible, en secondes. " +
             "Evite que les deux pieds, ou deux pas de physique de suite, " +
             "empilent les impulsions.")]
    public float cooldown = 0.6f;

    [Tooltip("Compenser la masse de la cible : le corps part a la meme vitesse " +
             "quelle que soit sa masse. Decoche pour une vraie impulsion en N.s.")]
    public bool massIndependent = true;

    [Header("Avance")]
    [Tooltip("Physics > Auto Sync Transforms est desactive dans ce projet, donc " +
             "les requetes de physique travaillent sur l'etat du pas precedent. " +
             "Coche : on resynchronise avant de chercher, le contact est franc.")]
    public bool syncTransformsBeforeQuery = true;

    [Range(0f, 0.9f)]
    [Tooltip("Lissage de la vitesse mesuree, 0 = brut. Un peu de lissage " +
             "absorbe le bruit de la mocap.")]
    public float velocitySmoothing = 0.3f;

    [Tooltip("Passer l'Animator en Update Mode = Fixed. Inutile sur un avatar " +
             "pilote par Rokoko (la pose ne vient pas de l'Animator), utile sur " +
             "un avatar anime par un clip.")]
    public bool forceFixedAnimator = false;

    [Header("Debug")]
    public bool drawGizmos = true;
    public bool logKicks   = false;

    readonly Collider[]             _hits     = new Collider[16];
    readonly Dictionary<int, float> _lastKick = new Dictionary<int, float>();
    Vector3[] _lastPos;
    Vector3[] _vel;
    bool      _primed;

    void Awake()
    {
        if (feet == null || feet.Length == 0) feet = FindHumanoidFeet();

        if (forceFixedAnimator)
        {
            var anim = GetComponent<Animator>();
            if (anim != null) anim.updateMode = AnimatorUpdateMode.Fixed;
        }

        if (feet == null || feet.Length == 0)
            Debug.LogWarning("[FootLauncher] Aucun pied trouve : renseigne 'feet' a la main.", this);
    }

    /// <summary>Pieds gauche et droit du rig humanoide, s'il y en a un.</summary>
    public Transform[] FindHumanoidFeet()
    {
        var anim = GetComponent<Animator>();
        if (anim == null || anim.avatar == null || !anim.avatar.isHuman) return new Transform[0];

        var found = new List<Transform>(2);
        var l = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        var r = anim.GetBoneTransform(HumanBodyBones.RightFoot);
        if (l != null) found.Add(l);
        if (r != null) found.Add(r);
        return found.ToArray();
    }

    Vector3 FootCenter(Transform foot)
    {
        return foot.position + foot.rotation * footOffset;
    }

    void FixedUpdate()
    {
        if (feet == null || feet.Length == 0) return;

        if (_lastPos == null || _lastPos.Length != feet.Length)
        {
            _lastPos = new Vector3[feet.Length];
            _vel     = new Vector3[feet.Length];
            _primed  = false;
        }

        float dt = Time.fixedDeltaTime;

        // Premier pas : pas de position precedente connue. Une vitesse calculee
        // la-dessus serait enorme et lancerait tout ce qui traine.
        if (!_primed)
        {
            for (int i = 0; i < feet.Length; i++)
                if (feet[i] != null) _lastPos[i] = FootCenter(feet[i]);
            _primed = true;
            return;
        }

        if (syncTransformsBeforeQuery) Physics.SyncTransforms();

        for (int i = 0; i < feet.Length; i++)
        {
            var foot = feet[i];
            if (foot == null) continue;

            Vector3 pos = FootCenter(foot);
            Vector3 raw = (pos - _lastPos[i]) / dt;
            _lastPos[i] = pos;
            _vel[i]     = Vector3.Lerp(raw, _vel[i], velocitySmoothing);

            float speed = _vel[i].magnitude;
            if (speed < minSpeed) continue;

            int count = Physics.OverlapSphereNonAlloc(
                pos, footRadius, _hits, targetLayers, QueryTriggerInteraction.Ignore);

            for (int h = 0; h < count; h++)
            {
                var col = _hits[h];
                if (col == null) continue;

                var rb = col.attachedRigidbody;
                if (rb == null) continue;

                // Ne pas se frapper soi-meme : les colliders de pieds et de
                // corps du kicker sont dans la sphere en permanence.
                if (rb.transform == transform || rb.transform.IsChildOf(transform)) continue;

                var body = rb.GetComponent<LaunchableBody>();
                if (onlyLaunchableBodies && body == null) continue;

                // Physique non armee : la conduite n'a pas encore donne le top.
                // On sort avant le cooldown, sinon le premier pied qui passe
                // dessus avant le top grillerait le coup suivant.
                if (body != null && !body.Armed) continue;

                int key = rb.GetInstanceID();
                float last;
                if (_lastKick.TryGetValue(key, out last) && Time.time - last < cooldown) continue;
                _lastKick[key] = Time.time;

                Kick(rb, body, col, pos, _vel[i], speed);
            }
        }
    }

    void Kick(Rigidbody rb, LaunchableBody body, Collider col, Vector3 footPos, Vector3 vel, float speed)
    {
        Vector3 dir = Vector3.Slerp(vel / speed, Vector3.up, upwardBias).normalized;
        float   mag = Mathf.Min(speed, maxSpeed) * transfer;

        Vector3 impulse = dir * mag + Vector3.up * extraLift;
        if (massIndependent) impulse *= rb.mass;

        // Point d'impact hors centre de masse : c'est ce bras de levier qui fait
        // tourner le corps pendant le vol.
        Vector3 point = col.ClosestPoint(footPos);

        if (body != null) body.Launch(impulse, point);
        else              rb.AddForceAtPosition(impulse, point, ForceMode.Impulse);

        if (spin > 0f)
        {
            Vector3 torque = Random.onUnitSphere * (spin * mag);
            if (massIndependent) torque *= rb.mass;
            rb.AddTorque(torque, ForceMode.Impulse);
        }

        if (logKicks)
            Debug.Log("[FootLauncher] " + rb.name + " frappe a " + speed.ToString("0.0")
                      + " m/s, impulsion " + impulse.magnitude.ToString("0.0") + " N.s", rb);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        var f = (feet != null && feet.Length > 0) ? feet : FindHumanoidFeet();
        if (f == null) return;

        Gizmos.color = new Color(1f, 0.4f, 0f, 0.8f);
        foreach (var foot in f)
        {
            if (foot == null) continue;
            Gizmos.DrawWireSphere(FootCenter(foot), footRadius);
        }
    }
}
