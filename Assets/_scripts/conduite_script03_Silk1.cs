using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteInEditMode]
public class conduite_script03_Silk1: MonoBehaviour
{
    [Header("---------REFERENCES ---------")]

    public cameraSelector camSelector;
    public placeTrendmillAvatar avatarPlaces;
    public GameObject avatarsAudience;
    public GameObject avatarOrcDancing;
    public GameObject avatarsAudience2;
    //public GameObject avatarJen;

    public GameObject whiteLight;
    public GameObject redLight;


    public GameObject[] lookAtTargets;

    [Range(1, 15)]
    public int sceneNB;

    [Header("--------- OVERRIDES PRESETS ---------")]
    [Tooltip("-1 = la camera suit sceneNB. >= 0 : fige cet index de camera, " +
             "independamment de l'etat dramaturgique. Permet d'ajouter une camera " +
             "sans toucher a la machine a etats : il suffit de l'ajouter a " +
             "cameraSelector.cameras et de capturer un preset qui pointe dessus.")]
    public int cameraOverride = -1;

    [Tooltip("Ancre de l'avatar Rokoko. Le placement ne vient plus de la machine " +
             "a etats mais de cette ancre, dont la pose est capturee par le preset. " +
             "Sert ici uniquement a appliquer la hauteur du slider d'elevation.")]
    public PresetAnchor rokokoAnchor;

    [Tooltip("Le AnchorFollower pose sur l'avatar Rokoko. Sert a l'accrocher a la " +
             "plateforme pendant la montee.")]
    public AnchorFollower rokokoFollower;

    [Tooltip("Objet solidaire de la plateforme auquel l'avatar s'accroche pendant " +
             "l'etat 2. C'est l'ancien places[4] de placeTrendmillAvatar : un " +
             "marqueur enfant de l'Elevator, donc il monte avec lui.")]
    public Transform plateformeAttach;

    [Range(-1, 1)]
    private float elevationAvatar;

    private bool otherAvatars = false;
    private SkinnedMeshRenderer[] skinnedMRenders;

    public Rokoko.CommandAPI.StudioCommandAPI rokoko;
    public GameObject canvasBlackOut;

    public float multiplierLerpTime;

    public GameObject blackout;

    [Header("---------SHOT INTRO NEW ---------")]
    public cinemachine_followTrack01 dollyFollower;
    public Animator _animPodium;

    [Tooltip("Nom de l'etat de l'Animator du podium (controller Elevator)")]
    public string podiumStateName = "podiumAnim";

    [Tooltip("Remet la camera en POV au depart de la montee")]
    public bool resetCameraOnMontee = true;

    [Tooltip("Alarme relancee a chaque top plateforme (laisser vide pour ne pas y toucher)")]
    public declencheurAlarm alarm;

    // sceneNB "reellement entree" : permet de ne declencher la sequence
    // qu'une fois par entree dans l'etat, au lieu de chaque frame.
    private int currentScene = -1;
    private Coroutine coCloseUp, coDolly;
    void Start()
    {

        //init
        sceneNB = 1;
        elevationAvatar = 0.65f; //commence debout
        canvasBlackOut.SetActive(false);


        camSelector.activeCamera = 0;
        avatarsAudience.SetActive(false);
        avatarsAudience2.SetActive(false);
        avatarOrcDancing.SetActive(true);

        whiteLight.SetActive(true);
        redLight.SetActive(false);


        //grab all the skinnedMeshRenderers components
        skinnedMRenders = new SkinnedMeshRenderer[25];
        skinnedMRenders = avatarPlaces.GetComponentsInChildren<SkinnedMeshRenderer>();


        //new shot intro
        dollyFollower.enabled = false;
        _animPodium.speed = 0.0f;// Stop();

        ResetPodium();
        currentScene = sceneNB;

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

    public void onMessageHeight(OscMessage message)
    {
        elevationAvatar = message.GetFloat(0);
    }

    public void setAvatarElevation(float value)
    {
        elevationAvatar = value;
    }

    IEnumerator avatarsReveal()
    {
        yield return new WaitForSeconds(2);

        avatarsAudience.SetActive(true);
        otherAvatars = true;
    }

    void Update()
    {
        // detection d'entree dans un etat : tout ce qui doit partir "de zero"
        // se declenche ici, et une seule fois.
        if (sceneNB != currentScene)
        {
            currentScene = sceneNB;

            if (sceneNB == 2)
                StartMonteePlateforme();
        }

        // Par defaut l'avatar est libre : c'est son ancre, donc le preset, qui le
        // place. Seul l'etat 2 le rattache ci-dessous a la plateforme.
        if (rokokoFollower != null) rokokoFollower.attachTo = null;

        if (sceneNB == 1) // en POV dans les loges 
        {
            camSelector.activeCamera = 0; // VCAM_POV_newAvatar

            showAvatarJen();

            avatarsAudience.SetActive(false);
            avatarsAudience2.SetActive(false);

            whiteLight.SetActive(true);
            redLight.SetActive(false);


        }

        if (sceneNB == 2) // montée plateforme
        {
            // le declenchement (cameras + anim a zero) est fait par
            // StartMonteePlateforme(), ici on ne fait qu'entretenir l'etat


            avatarsAudience.SetActive(true);
            avatarOrcDancing.SetActive(true);

            // L'avatar devient solidaire de la plateforme : il monte avec elle.
            // Rattache a chaque frame plutot qu'une fois a l'entree dans l'etat,
            // pour resister a un rappel de preset pendant la montee.
            if (rokokoFollower != null) rokokoFollower.attachTo = plateformeAttach;

            _animPodium.speed = 1.0f;
        }

        if (sceneNB == 3) // dollyMain
        {
            camSelector.activeCamera = 3;
        }

        // A VIRER //
        if (sceneNB == 4) //publicDolly
        {
            avatarsAudience.SetActive(true);
            //setCamPublicLookAt(lookAtTargets[0].transform);
            camSelector.activeCamera = 4;
        }

        if (sceneNB == 5) //dolly Elevate
        {

            camSelector.activeCamera = 5;
            showAvatarJen();

        }

        if (sceneNB == 6) // dolly Zoom
        {

            //hideAvatarJen();

            camSelector.activeCamera = 7;
            //setCamPublicLookAt(lookAtTargets[0].transform);

        }

        if (sceneNB == 7) // equilibre
        {
            //showAvatarJen();

            camSelector.activeCamera = 6;
        }

        if (sceneNB == 8) // reversePOV
                          //hideAvatarJen();
        {
            camSelector.activeCamera = 8;

            //avatarPlaces.activePlace = 1; //place Winnie
            //avatarsAudience.SetActive(true);
        }

        if (sceneNB == 9) // camera public 03
        {
            showAvatarJen();
            camSelector.activeCamera = 9;
        }

        if (sceneNB == 10) // danse Winnie
        {
            showAvatarJen();
            camSelector.activeCamera = 10;

        }

        if (sceneNB == 11) // camera public 04
        {
            hideAvatarJen();
            camSelector.activeCamera = 11;

        }

        if (sceneNB == 12) //camera public 05
        {
            hideAvatarJen();

            avatarOrcDancing.SetActive(false); //orc qui danse

            avatarsAudience2.SetActive(false); //orc couché
            camSelector.activeCamera = 12;

        }

        if (sceneNB == 13) // dolly Orc
        {
            showAvatarJen();
            avatarsAudience2.SetActive(true); //orc couché
            camSelector.activeCamera = 13;

        }

        if (sceneNB == 14) // camera finale
        {
            camSelector.activeCamera = 14;
            showAvatarJen();
        }

        if (sceneNB == 15) // black out
        {
            blackout.SetActive(true);
        }

        // Override camera : applique apres la chaine d'etats, qui vient de
        // reecrire activeCamera depuis sceneNB. -1 = pas d'override.
        if (cameraOverride >= 0) camSelector.activeCamera = cameraOverride;


        //APPLY HEIGHT
        // La hauteur du slider pilote l'altitude de l'ancre ; l'avatar la suit via
        // son AnchorFollower. Comme avant, elle ecrase le Y pose a la main : c'est
        // le slider qui fait foi sur cet axe, hors etats 1 et 2.
        if (sceneNB != 1 && sceneNB != 2 && rokokoAnchor != null)
            rokokoAnchor.position.y = elevationAvatar;

    }

    public void hideAvatarJen()
    {
        for (int i = 0; i < skinnedMRenders.Length; i++)
        {
            skinnedMRenders[i].enabled = false;
        }
    }

    public void showAvatarJen()
    {
        for (int i = 0; i < skinnedMRenders.Length; i++)
        {
            skinnedMRenders[i].enabled = true;
        }
    }

    public void setCamPublicLookAt(Transform subject)
    {
        camSelector.cameras[5].GetComponent<Cinemachine.CinemachineVirtualCamera>().LookAt = subject;
    }

    // Toute entree manuelle dans un etat â€” bouton de la surface Kontrols, OSC,
    // menu contextuel â€” passe par ici, et relache les overrides de preset.
    // Sans ca, une fois un preset rappele la camera resterait figee et les tops
    // auraient l'air morts.
    void SetScene(int n)
    {
        sceneNB        = n;
        cameraOverride = -1;
    }

    public void ButtonNEXT()
    {
        if (sceneNB + 1 <= 15)
            SetScene(sceneNB + 1);
    }

    public void ButtonBACK()
    {
        if (sceneNB - 1 > 0)
            SetScene(sceneNB - 1);
    }
    public void DollyMain()
    {
        SetScene(3);
    }

    public void DollyPublic()
    {
        SetScene(4);
    }
    public void DollyMain2()
    {
        SetScene(5);
    }
    public void DollyPublic2()
    {
        SetScene(6);
    }
    public void ReversePOV()
    {
        SetScene(8);
    }
    public void Zoom()
    {
        SetScene(7);
    }

    public void MonteePlateforme()
    {
        // -1 force la re-entree dans l'etat, meme si on y est deja :
        // le bouton rejoue donc la sequence a la demande.
        currentScene = -1;
        SetScene(2);
    }

    // Declenche (ou rejoue) la montee de plateforme depuis le debut.
    private void StartMonteePlateforme()
    {
        // 1. on coupe les tops cameras de la sequence precedente
        if (coCloseUp != null) { StopCoroutine(coCloseUp); coCloseUp = null; }
        if (coDolly != null) { StopCoroutine(coDolly); coDolly = null; }

        // 2. anim du podium remise a la frame 0, a l'arret
        ResetPodium();

        // 3. on repart en POV
        if (resetCameraOnMontee)
            camSelector.activeCamera = 0;

        // 4. re-arme les tops cameras et relance l'alarme
        if (Application.isPlaying)
        {
            coCloseUp = StartCoroutine(CloseUpPipeCamera(6));
            coDolly = StartCoroutine(DollyPipeCamera(10));

            if (alarm != null)
                alarm.RestartAll();
        }

        // 5. la montee repart (Update maintient speed = 1 tant que sceneNB == 2)
        _animPodium.speed = 1.0f;
    }

    // Replace l'Animator du podium sur la premiere frame de son clip.
    private void ResetPodium()
    {
        _animPodium.speed = 0.0f;
        _animPodium.Play(podiumStateName, 0, 0.0f);

        if (Application.isPlaying)
            _animPodium.Update(0.0f); // force l'evaluation immediate de la frame 0
    }

    public void DollySol()
    {
        SetScene(13);
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

    private IEnumerator DollyPipeCamera(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        Debug.Log("top camera dolly pipe");
        camSelector.activeCamera = 2;

        coDolly = null;
    }

    private IEnumerator CloseUpPipeCamera(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        Debug.Log("top camera close up pipe");
        camSelector.activeCamera = 1;

        coCloseUp = null;
    }
}
