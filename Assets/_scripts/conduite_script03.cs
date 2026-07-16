using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteInEditMode]
public class conduite_script03 : MonoBehaviour
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


    private bool done1, done2;
    void Start()
    {

        //init
        sceneNB = 1;
        elevationAvatar = 0.65f; //commence debout
        canvasBlackOut.SetActive(false);


        camSelector.activeCamera = 0;
        avatarPlaces.activePlace = 0;
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

        done1 = done2 = false;

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
        if (sceneNB == 1) // en POV dans les loges 
        {
            camSelector.activeCamera = 0; // VCAM_POV_newAvatar

            showAvatarJen();

            avatarsAudience.SetActive(false);
            avatarsAudience2.SetActive(false);

            whiteLight.SetActive(true);
            redLight.SetActive(false);


            avatarPlaces.activePlace = 4; //position plateforme
        }

        if (sceneNB == 2) // montée plateforme
        {
            //on reste en POV 10s, puis camera plateforme
            StartCoroutine(CloseUpPipeCamera(6));
            StartCoroutine(DollyPipeCamera(10));

            avatarPlaces.activePlace = 4; //position plateforme (qui monte)

            avatarsAudience.SetActive(true);
            avatarOrcDancing.SetActive(true);

            _animPodium.speed = 1.0f;
        }

        if (sceneNB == 3) // close shot fix
        {
            camSelector.activeCamera = 3;
        }

        // A VIRER //
        if (sceneNB == 4) //very close shot
        {
            avatarsAudience.SetActive(true);
            camSelector.activeCamera = 4;
        }

        if (sceneNB == 5) //dolly 
        {

            camSelector.activeCamera = 5;
            showAvatarJen();

        }

        if (sceneNB == 6) // camera public 1 (pour calib)
        {

            hideAvatarJen();

            camSelector.activeCamera = 6;
            setCamPublicLookAt(lookAtTargets[0].transform);

        }

        if (sceneNB == 7) // equilibre
        {
            showAvatarJen();

            camSelector.activeCamera = 7;
        }

        if (sceneNB == 8) // camera public 02
        {
            hideAvatarJen();

            camSelector.activeCamera = 8;

            avatarPlaces.activePlace = 1; //place Winnie
            avatarsAudience.SetActive(true);
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
            avatarPlaces.activePlace = 2;
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


        //APPLY HEIGHT
        if (sceneNB != 1 && sceneNB != 2)
            avatarPlaces.places[avatarPlaces.activePlace].transform.position = new Vector3(avatarPlaces.places[avatarPlaces.activePlace].transform.position.x, elevationAvatar, avatarPlaces.places[avatarPlaces.activePlace].transform.position.z);

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

    public void ButtonNEXT()
    {
        if (sceneNB + 1 <= 15)
            sceneNB++;
    }

    public void ButtonBACK()
    {
        if (sceneNB - 1 > 0)
            sceneNB--;
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
        if(!done2)
        {
            yield return new WaitForSeconds(waitTime);
            Debug.Log("top camera dolly pipe");
            camSelector.activeCamera = 2;

            done2 = true;
        }

    }

    private IEnumerator CloseUpPipeCamera(float waitTime)
    {
        if(!done1)
        {
            yield return new WaitForSeconds(waitTime);
            Debug.Log("top camera close up pipe");
            camSelector.activeCamera = 1;

            done1 = true;
        }
    }
}
