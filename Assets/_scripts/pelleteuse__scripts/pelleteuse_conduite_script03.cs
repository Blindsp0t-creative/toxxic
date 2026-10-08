using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.Locomotion;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[ExecuteInEditMode]
public class pelleteuse_conduite_script03 : MonoBehaviour
{
    [Header("---------REFERENCES ---------")]

    public pelleteuse_cameraSelector camSelector;
    public pelleteuse_placeTrendmillAvatar avatarPlaces;
    public GameObject photoQuad;
    //public GameObject[] lookAtTargets;

    [Range(1,10)]
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

    [Tooltip("Le point du rig VR qui doit se retrouver exactement sur 'Cube (4)'. " +
             "Vide = CenterEyeAnchor, la tete du performeur. Pour poser ses PIEDS " +
             "sur le godet, glisser ici la racine de l'avatar, ou laisser la tete " +
             "et monter accrocheOffset.y d'environ sa taille. Doit etre un enfant " +
             "du rig : la correction est un delta applique a la racine du rig.")]
    public Transform accrocheReference;

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

        camSelector.cameras[7].GetComponent<pelleteuse_cinemachine_followTrack01>().enabled = false;

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

    }

    IEnumerator avatarsReveal()
    {
        yield return new WaitForSeconds(2);

    }

    public void showPhoto()
    {
        photoQuad.SetActive(true);
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
            if (entreeDansEtat) AccrochePelleteuse(true);
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
            rigFromMasterScene.transform.position = pellePosition.transform.position;
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
        sceneNB        = n;
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

            if (VrRig() == null)
            {
                Debug.LogWarning("[AccrochePelleteuse] aucun objet tague 'VRRig' : 00_MASTER_ n'est " +
                                 "probablement pas chargee. Accroche ignoree.");
                return;
            }

            // Memorise avant de couper, pour restituer a l'identique au lachage.
            _locomotorEtait   = LOCOMOTOR   != null && LOCOMOTOR.activeSelf;
            _interactionEtait = INTERACTION != null && INTERACTION.activeSelf;

            if (LOCOMOTOR   != null) LOCOMOTOR.SetActive(false);
            if (INTERACTION != null) INTERACTION.SetActive(false);

            _accrochee = true;

            // Pose immediate : sans ca le performeur resterait une frame en
            // arriere, ce qui se verrait sur un top.
            SuitLaPelle();
        }
        else
        {
            _accrochee = false;

            if (LOCOMOTOR   != null) LOCOMOTOR.SetActive(_locomotorEtait);
            if (INTERACTION != null) INTERACTION.SetActive(_interactionEtait);

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

        Vector3 cible = pellePosition.transform.position + accrocheOffset;

        // On ne teleporte PAS la racine du rig sur la cible. Cette racine n'est
        // que l'origine de l'espace de tracking : la tete du performeur est
        // decalee a l'interieur, de son deplacement physique dans la piece. Poser
        // la racine sur le godet laisse donc l'avatar translate de ce decalage —
        // c'est exactement le bug observe. On deplace le rig du delta qui amene
        // la reference sur la cible ; comme la reference est un enfant du rig,
        // elle atterrit pile dessus.
        Transform reference = AccrocheReference(rig);

        if (reference == null)
        {
            rig.transform.position = cible;   // dernier recours, sans correction
            return;
        }

        rig.transform.position += cible - reference.position;
    }

    // Le point du rig a amener sur le godet. Par defaut la tete : c'est le seul
    // repere que le rig expose de maniere fiable, et c'est le point que le
    // performeur ressent comme "lui".
    Transform AccrocheReference(GameObject rig)
    {
        if (accrocheReference != null) return accrocheReference;

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

}
