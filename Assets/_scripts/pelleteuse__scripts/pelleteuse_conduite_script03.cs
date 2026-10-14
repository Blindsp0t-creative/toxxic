using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.Locomotion;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.SceneManagement;

[ExecuteInEditMode]
public class pelleteuse_conduite_script03 : MonoBehaviour
{
    [Header("---------REFERENCES ---------")]

    public pelleteuse_cameraSelector camSelector;
    public pelleteuse_placeTrendmillAvatar avatarPlaces;
    public GameObject photoQuad;
    //public GameObject[] lookAtTargets;

    [Range(1, 10)]
    public int sceneNB;

    [Header("--------- OVERRIDES PRESETS ---------")]
    [Tooltip("-1 = la camera suit sceneNB. >= 0 : fige cet index de camera, " +
             "independamment de l'etat dramaturgique. Permet d'ajouter une camera " +
             "sans toucher a la machine a etats : il suffit de l'ajouter a " +
             "camSelector.cameras et de capturer un preset qui pointe dessus. " +
             "Remis a -1 par tout bouton de conduite.")]
    public int cameraOverride = -1;
    public Rokoko.CommandAPI.StudioCommandAPI rokoko;
    public GameObject canvasBlackOut;

    [Range(-1.0f, 4.0f)]
    public float avatarHeight;

    public GameObject pellePosition;

    public GameObject LOCOMOTOR;
    public GameObject INTERACTION;
    public GameObject AVATAR;


    public FirstPersonLocomotor locomotor;
    public GameObject vrRoot;
    public OVRCameraRig cameraRigXR;
    public GameObject locomotorRoot;

    public GameObject cow;

    public Material materialQuad;

    //pelleteuse_OSC
    public OSC _handler;

    private bool done = false;

    GameObject rigFromMasterScene;

    [Header("--------- ACCROCHE PELLETEUSE ---------")]
    [Tooltip("Decalage ajoute a la position de pellePosition pendant l'accroche. " +
             "Zero = le performeur est pile sur 'Cube (4)'. Permet de le remonter " +
             "ou de le reculer dans le godet sans toucher a la scene.")]
    public Vector3 accrocheOffset;

    [Tooltip("Le point qui doit se retrouver exactement sur 'Cube (4)'. Vide = " +
             "CenterEyeAnchor, la tete du performeur. Pour poser les PIEDS de " +
             "l'avatar sur le godet, viser sa racine. S'il vit dans 00_MASTER_, " +
             "l'Inspector refusera le glisser-deposer : passer par " +
             "accrocheReferenceNom juste en dessous.")]
    public Transform accrocheReference;

    [Tooltip("Nom du GameObject a prendre comme accrocheReference, resolu au " +
             "demarrage. Unity interdit les references inter-scenes dans " +
             "l'Inspector : c'est le seul moyen de viser un objet de 00_MASTER_ " +
             "depuis 02_PELLETEUSE chargee en additif. Accepte un chemin complet " +
             "facon '/Racine/Enfant' si le nom seul n'est pas unique. Ignore si " +
             "accrocheReference est deja renseigne.")]
    public string accrocheReferenceNom;

    [Tooltip("L'offset tourne avec le godet. Le vecteur reste exprime en axes " +
             "MONDE au moment du top (+Y = vers le haut, facile a regler), puis " +
             "il suit la pelle : le performeur garde la meme place a l'interieur " +
             "du godet quand celui-ci bascule. Decocher pour un offset monde fige.")]
    public bool accrocheOffsetSuitLeGodet = true;

    // Rotation du godet au moment de l'accroche. Sert de repere pour faire
    // tourner accrocheOffset avec la pelle sans toucher a la valeur reglee.
    private Quaternion _rotGodetAccroche = Quaternion.identity;

    // Ecart entre la reference et la tete du rig, releve au moment de l'accroche.
    //
    // Pourquoi ne pas simplement lire reference.position a chaque frame : une
    // reference EXTERIEURE au rig (l'avatar de 00_MASTER_) ne suit pas le rig
    // dans la meme phase que nous. HeadBodyRig la repositionne dans FixedUpdate,
    // qui ne tourne pas a toutes les frames. Les frames sans FixedUpdate, on
    // relirait une position perimee et on redeplacerait le rig du meme delta :
    // depassement, puis correction en sens inverse — ca retremble.
    //
    // On passe donc par la tete, qui est enfant du rig et donc toujours
    // coherente avec lui, et on y ajoute cet ecart fige. Valide tant que la
    // reference garde un decalage constant par rapport a la tete, ce qui est
    // exactement ce que garantit HeadBodyRig.
    private Vector3 _ecartRefTete;
    private bool    _refHorsDuRig;

    // La vraie locomotion du rig de 00_MASTER_ : OVRLocomotion pilote un
    // CharacterController pose sur l'OVRCameraRig lui-meme. Rien a voir avec
    // LOCOMOTOR / INTERACTION, qui pointent sur le groupe "VR (1)" desactive de
    // 02_PELLETEUSE et ne coupent donc rien en pratique.
    // Qualifie : le 'using Oculus.Interaction.Locomotion' en tete de fichier
    // amene un autre CharacterController, et les deux noms entrent en collision.
    private OVRLocomotion                   _ovrLoco;
    private UnityEngine.CharacterController _ccRig;
    private bool _ovrLocoEtait, _ccEtait;

    // Accroche en cours. Lu par LateUpdate, ecrit par AccrochePelleteuse seule.
    private bool _accrochee;

    // Etat de la locomotion juste avant l'accroche. On le restitue tel quel au
    // decrochage : forcer SetActive(true) rallumerait peut-etre quelque chose qui
    // avait ete eteint volontairement avant le top.
    private bool _locomotorEtait, _interactionEtait;

    // Dernier sceneNB vu par Update, pour distinguer l'entree dans un etat de sa
    // repetition frame par frame.
    private int _lastSceneNB = -1;

    void Start()
    {
        sceneNB = 1;
        canvasBlackOut.SetActive(false);
        camSelector.activeCamera = 0;
        photoQuad.SetActive(false);

        //camSelector.cameras[7].GetComponent<pelleteuse_cinemachine_followTrack01>().enabled = false;

        //materialQuad.color = new Color(255,255,255, 255);


        //INIT pelleteuse_OSC
        /*
        _handler.SetAllMessageHandler(allMessages);
        _handler.SetAddressHandler("/osc/next", onMessageNext);
        _handler.SetAddressHandler("/osc/back", onMessageBack);
        _handler.SetAddressHandler("/osc/calib", onMessageCalib);
        */

        cow.GetComponent<Rigidbody>().isKinematic = true; //disable physics


        //FIND VRRig from master scene
        GameObject[] gos;
        gos = GameObject.FindGameObjectsWithTag("VRRig");
        rigFromMasterScene = gos[0];

        ResoudreAccrocheReference();
    }

    /// <summary>
    /// Resout accrocheReference par son nom. Unity interdit les references
    /// inter-scenes dans l'Inspector : un objet de 00_MASTER_ ne peut pas etre
    /// glisse dans un champ de 02_PELLETEUSE, qui est chargee en additif. Meme
    /// contournement que pour rigFromMasterScene juste au-dessus, au nom plutot
    /// qu'au tag pour ne pas avoir a taguer l'avatar.
    ///
    /// Rappele a l'accroche : au premier Start la scene visee n'est pas
    /// forcement encore chargee.
    /// </summary>
    void ResoudreAccrocheReference()
    {
        if (accrocheReference != null) return;
        if (string.IsNullOrEmpty(accrocheReferenceNom)) return;

        Transform t = TrouverParNom(accrocheReferenceNom);

        if (t == null)
        {
            Debug.LogWarning($"[AccrochePelleteuse] '{accrocheReferenceNom}' introuvable dans les " +
                             "scenes chargees. Verifier l'orthographe exacte, et que 00_MASTER_ " +
                             "est bien la.");
            return;
        }

        accrocheReference = t;
        Debug.Log($"[AccrochePelleteuse] accrocheReference resolue sur '{t.name}'.");
    }

    /// <summary>
    /// Cherche un Transform par nom dans toutes les scenes chargees, objets
    /// DESACTIVES compris.
    ///
    /// GameObject.Find seul ne suffit pas : dans 00_MASTER_ les avatars sont
    /// sauves inactifs et allumes par la conduite au moment voulu. Au Start de
    /// cette scene-ci ils sont donc invisibles pour Find(), qui ignore les
    /// objets inactifs.
    /// </summary>
    Transform TrouverParNom(string nom)
    {
        // D'abord Find : le plus rapide, et lui seul gere la forme chemin
        // "/Racine/Enfant" qui leve les homonymies.
        GameObject direct = GameObject.Find(nom);
        if (direct != null) return direct.transform;

        // Sinon parcours complet. On ne garde que le dernier segment : un chemin
        // ne sert qu'a Find, la recherche large compare sur le nom seul.
        string cible = nom.Substring(nom.LastIndexOf('/') + 1);

        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            UnityEngine.SceneManagement.Scene scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;

            foreach (GameObject racine in scene.GetRootGameObjects())
                foreach (Transform t in racine.GetComponentsInChildren<Transform>(true))
                    if (t.name == cible) return t;
        }

        return null;
    }

    IEnumerator avatarsReveal()
    {
        yield return new WaitForSeconds(2);

    }

    public void showPhoto(bool val)
    {
        if (val == true)
        {
            photoQuad.SetActive(true);
        }
        else
        {
            photoQuad.SetActive(false);
        }
    }

    public void allMessages(OscMessage message)
    {
        Debug.Log(message.address);
    }

    public void onMessageNext(OscMessage message)
    {
        if (message.GetFloat(0) > 0.5)
            ButtonNEXT();

        Debug.Log(message.address);
    }

    public void onMessageBack(OscMessage message)
    {
        if (message.GetFloat(0) > 0.5)
            ButtonBACK();
        Debug.Log(message.address);
    }
    public void onMessageCalib(OscMessage message)
    {
        if (message.GetFloat(0) > 0.5)
            rokoko.CalibrateAll();
    }



    void Update()
    {
        locomotor.HeightOffset = avatarHeight;

        // Entree dans un etat. La machine a etats tourne a chaque frame, mais
        // l'accroche ne doit tirer qu'une fois : sinon elle se reinstallerait
        // derriere chaque decrochage du toggle de la surface Kontrols.
        bool entreeDansEtat = sceneNB != _lastSceneNB;
        _lastSceneNB = sceneNB;


        if (sceneNB == 1) //soleil
        {
            camSelector.activeCamera = 0;
        }

        if (sceneNB == 2) //accueil Miku
        {
            camSelector.activeCamera = 1;
        }

        if (sceneNB == 3) //découverte pelleteuse
        {
            camSelector.activeCamera = 2;
        }

        if (sceneNB == 4) //carresse vache
        {
            camSelector.activeCamera = 3;

        }
        if (sceneNB == 5) //pelleteuse carresse
        {
            camSelector.activeCamera = 4;
            cow.GetComponent<Rigidbody>().isKinematic = false; //enable physics

        }

        if (sceneNB == 6) //photo
        {

            photoQuad.SetActive(true);
            //photoQuad.GetComponent<Renderer>().material.color = new Color(255, 255, 255, 255);
            //materialQuad.color = new Color(255, 255, 255, 255);
            camSelector.activeCamera = 5;
        }

        if (sceneNB == 7)  //monte pelle
        {

            //DISPARITION EN FADE DU QUAD
            //StartCoroutine(hideQuad());


            camSelector.activeCamera = 6;

            // Remplace le placement en dur d'avant (RigBuilder coupe, position
            // recopiee a chaque frame) : un seul chemin de code pour l'accroche,
            // et le toggle 'accrochePelle' peut decrocher derriere.
            //if (entreeDansEtat) AccrochePelleteuse(true);
        }


        if (sceneNB == 8)  //danse + dolly
        {

            //cache la photo
            StartCoroutine(CachePhoto(4));

            camSelector.cameras[7].GetComponent<pelleteuse_cinemachine_followTrack01>().enabled = true;

            camSelector.activeCamera = 7;

            //AVATAR.GetComponent<RigBuilder>().enabled = false;
            //LOCOMOTOR.SetActive(false);
            //INTERACTION.SetActive(false);

            ///AVATAR.transform.position = pellePosition.transform.position;
            //rigFromMasterScene.transform.position = pellePosition.transform.position;
        }

        if (sceneNB == 9)  //aurevoir
        {
            //canvasBlackOut.SetActive(true);
            camSelector.activeCamera = 8;
        }

        if (sceneNB == 10) //noir
        {
            canvasBlackOut.SetActive(true);
        }

        // Override de preset : applique apres la chaine d'etats, qui vient de
        // reecrire activeCamera depuis sceneNB. -1 = pas d'override, la conduite
        // garde la main.
        if (cameraOverride >= 0) camSelector.activeCamera = cameraOverride;
    }

    public void setCamPublicLookAt(Transform subject)
    {
        camSelector.cameras[5].GetComponent<Cinemachine.CinemachineVirtualCamera>().LookAt = subject;
    }

    // Toute entree manuelle dans un etat — bouton de la surface Kontrols, OSC —
    // passe par ici, et relache l'override de preset. Sans ca, une fois un preset
    // rappele la camera resterait figee et les tops auraient l'air morts.
    void SetScene(int n)
    {
        sceneNB = n;
        cameraOverride = -1;
    }

    public void ButtonNEXT()
    {
        if (sceneNB + 1 <= 10)
            SetScene(sceneNB + 1);
    }

    public void ButtonBACK()
    {
        if (sceneNB - 1 > 0)
            SetScene(sceneNB - 1);
    }

    IEnumerator hideQuad()
    {
        while (materialQuad.color.a > 0)
        {
            float alpha = materialQuad.color.a;
            alpha -= 0.00001f;
            materialQuad.color = new Color(materialQuad.color.r, materialQuad.color.g, materialQuad.color.b, alpha);
            /*
            float delta = rate * Time.deltaTime;
            if (delta > damage)
            {
                currentHP -= damage;
                break;
            }
            currentHP -= delta;
            damage -= delta;
            */
            yield return null;
        }
    }

    public void blackOut(bool value)
    {
        if (value == true)
        {
            canvasBlackOut.SetActive(true);
        }
        else if (value == false)
        {
            canvasBlackOut.SetActive(false);
        }
    }

    private IEnumerator CachePhoto(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        photoQuad.gameObject.SetActive(false);
    }

    /// <summary>
    /// Accroche le performeur VR au godet de la pelleteuse, ou le relache.
    ///
    /// true  : le rig VR — le GameObject tague "VRRig", soit OVRCameraRig dans
    ///         00_MASTER_ — est deplace a chaque frame, dans LateUpdate, de facon
    ///         a amener 'accrocheReference' (sa tete par defaut) sur la position
    ///         de 'pellePosition' ("Cube (4)", enfant de
    ///         TOXXICavator_Rig_TPOSE_3). Seule la POSITION est contrainte :
    ///         ni l'Animator ni le RigBuilder ne sont touches, donc la mocap
    ///         continue d'animer les membres normalement. La locomotion VR est
    ///         coupee pour que le performeur ne puisse pas sortir du godet en
    ///         marchant.
    /// false : on arrete d'ecrire la position. Le rig reste ou il est, au point
    ///         de lachage et pas a un point d'origine, et la locomotion est
    ///         rendue dans l'etat ou elle etait avant l'accroche.
    ///
    /// Pas de SetParent : "Cube (4)" a un scale local de 0.005, un reparentage
    /// diviserait le performeur par 200. D'ou le suivi frame par frame.
    ///
    /// Idempotent : rappeler avec la meme valeur ne fait rien. Un appel depuis
    /// Update n'accumule donc pas d'effets de bord.
    /// </summary>
    public void AccrochePelleteuse(bool _accroche)
    {
        if (_accroche == _accrochee) return;

        if (_accroche)
        {
            if (pellePosition == null)
            {
                Debug.LogWarning("[AccrochePelleteuse] 'pellePosition' non renseigne, accroche ignoree.");
                return;
            }

            GameObject rig = VrRig();
            if (rig == null)
            {
                Debug.LogWarning("[AccrochePelleteuse] aucun objet tague 'VRRig' : 00_MASTER_ n'est " +
                                 "probablement pas chargee. Accroche ignoree.");
                return;
            }

            // Memorise avant de couper, pour restituer a l'identique au lachage.
            _locomotorEtait = LOCOMOTOR != null && LOCOMOTOR.activeSelf;
            _interactionEtait = INTERACTION != null && INTERACTION.activeSelf;

            if (LOCOMOTOR != null) LOCOMOTOR.SetActive(false);
            if (INTERACTION != null) INTERACTION.SetActive(false);

            // LA cause des vibrations. La vraie locomotion du show n'est pas
            // LOCOMOTOR / INTERACTION — ces deux-la pointent sur le groupe
            // "VR (1)" de 02_PELLETEUSE, qui est desactive — mais OVRLocomotion
            // et son CharacterController, poses sur l'OVRCameraRig de
            // 00_MASTER_. OVRLocomotion appelle _cc.Move() a chaque Update,
            // gravite comprise, et la physique resout la penetration dans les
            // colliders de la pelleteuse ; notre LateUpdate reecrit derriere.
            // Deux ecritures par frame qui se contredisent = tremblement. Un
            // CharacterController desactive ne fait plus ni gravite ni
            // depenetration : l'ecriture directe redevient souveraine.
            _ovrLoco = rig.GetComponent<OVRLocomotion>();
            _ccRig   = rig.GetComponent<UnityEngine.CharacterController>();

            _ovrLocoEtait = _ovrLoco != null && _ovrLoco.enabled;
            _ccEtait      = _ccRig   != null && _ccRig.enabled;

            if (_ovrLoco != null) _ovrLoco.enabled = false;
            if (_ccRig   != null) _ccRig.enabled   = false;

            // Rotation du godet au moment du top. accrocheOffset est tune en
            // axes monde a cet instant, et tourne ensuite avec la pelle.
            _rotGodetAccroche = pellePosition.transform.rotation;

            // La reference de 00_MASTER_ n'a pas pu etre glissee dans
            // l'Inspector : derniere chance de la resoudre par son nom.
            ResoudreAccrocheReference();

            // Une reference hors du rig ne suit pas le rig dans notre phase :
            // on releve son ecart a la tete une fois pour toutes, et on
            // travaillera ensuite sur la tete. Voir _ecartRefTete.
            Transform tete = TeteDuRig(rig);
            _refHorsDuRig = accrocheReference != null
                         && tete != null
                         && !accrocheReference.IsChildOf(rig.transform);

            _ecartRefTete = _refHorsDuRig
                ? accrocheReference.position - tete.position
                : Vector3.zero;

            _accrochee = true;

            // Pose immediate : sans ca le performeur resterait une frame en
            // arriere, ce qui se verrait sur un top.
            SuitLaPelle();
        }
        else
        {
            _accrochee = false;

            if (LOCOMOTOR != null) LOCOMOTOR.SetActive(_locomotorEtait);
            if (INTERACTION != null) INTERACTION.SetActive(_interactionEtait);

            // Dans l'ordre inverse de l'accroche : le CharacterController se
            // re-enregistre a la position courante, donc au point de lachage,
            // puis OVRLocomotion reprend la main par-dessus.
            if (_ccRig   != null) _ccRig.enabled   = _ccEtait;
            if (_ovrLoco != null) _ovrLoco.enabled = _ovrLocoEtait;

            // Rien a repositionner : le rig est deja au point de lachage.
        }
    }

    // Apres Update, ou tourne la machine a etats, et apres la locomotion VR :
    // notre ecriture a le dernier mot sur la position du rig.
    void LateUpdate()
    {
        if (_accrochee) SuitLaPelle();
    }

    void SuitLaPelle()
    {
        if (pellePosition == null) return;

        GameObject rig = VrRig();
        if (rig == null) return;

        // L'offset est regle en axes monde au moment du top, puis emmene par la
        // rotation du godet : le performeur reste au meme endroit a l'interieur
        // de la pelle quand elle bascule, au lieu de glisser dessous.
        Vector3 offset = accrocheOffset;
        if (accrocheOffsetSuitLeGodet)
            offset = (pellePosition.transform.rotation * Quaternion.Inverse(_rotGodetAccroche)) * offset;

        Vector3 cible = pellePosition.transform.position + offset;

        // On ne teleporte PAS la racine du rig sur la cible. Cette racine n'est
        // que l'origine de l'espace de tracking : la tete du performeur est
        // decalee a l'interieur, de son deplacement physique dans la piece. Poser
        // la racine sur le godet laisse donc l'avatar translate de ce decalage —
        // c'est exactement le bug observe. On deplace le rig du delta qui amene
        // le point suivi sur la cible.
        //
        // Le point suivi est toujours pris DANS le rig : soit la reference elle
        // meme quand elle en fait partie, soit la tete corrigee de _ecartRefTete
        // quand la reference est exterieure. Lire une position exterieure a
        // chaque frame rouvrirait la boucle de retard decrite sur _ecartRefTete.
        Transform suivi = _refHorsDuRig ? TeteDuRig(rig) : AccrocheReference(rig);

        if (suivi == null)
        {
            rig.transform.position = cible;   // dernier recours, sans correction
            return;
        }

        rig.transform.position += cible - (suivi.position + _ecartRefTete);
    }

    // Le point du rig a amener sur le godet. Par defaut la tete : c'est le seul
    // repere que le rig expose de maniere fiable, et c'est le point que le
    // performeur ressent comme "lui".
    Transform AccrocheReference(GameObject rig)
    {
        if (accrocheReference != null) return accrocheReference;

        return TeteDuRig(rig);
    }

    // CenterEyeAnchor, le seul point du rig dont la position est garantie
    // coherente avec la racine dans toutes les phases.
    Transform TeteDuRig(GameObject rig)
    {
        OVRCameraRig ovr = rig.GetComponent<OVRCameraRig>();
        if (ovr != null && ovr.centerEyeAnchor != null) return ovr.centerEyeAnchor;

        // centerEyeAnchor est rempli par OVRCameraRig a l'init : null hors Play,
        // ou si le composant manque. On retombe sur le nom, puis sur rien.
        return rig.transform.Find("TrackingSpace/CenterEyeAnchor");
    }

    // rigFromMasterScene est resolu dans Start(), mais Start() peut ne pas avoir
    // tourne (ExecuteInEditMode, 02_PELLETEUSE ouverte seule) et un domain reload
    // de l'editeur peut avoir vide la reference. On re-resout a la demande plutot
    // que de lacher une NullReference en plein show.
    GameObject VrRig()
    {
        if (rigFromMasterScene != null) return rigFromMasterScene;

        GameObject[] gos = GameObject.FindGameObjectsWithTag("VRRig");
        if (gos.Length > 0) rigFromMasterScene = gos[0];

        return rigFromMasterScene;
    }


    public void activateCowPhysics(bool _val)
    {
        if (_val)
        {
            cow.GetComponent<Rigidbody>().isKinematic = false; //enable physics
        }
        else
        {
            cow.GetComponent<Rigidbody>().isKinematic = true; //disable physics
        }
    }
}
