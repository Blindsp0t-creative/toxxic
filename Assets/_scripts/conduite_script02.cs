using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class conduite_script02 : MonoBehaviour
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

    [Range(1,10)]
    public int sceneNB;

    [Range(-1,1)]
    private float elevationAvatar;

    private bool otherAvatars = false;
    private SkinnedMeshRenderer[] skinnedMRenders;

    public Rokoko.CommandAPI.StudioCommandAPI rokoko;
    public GameObject canvasBlackOut;


    public GameObject[] notesConduites;

    // starting value for the Lerp
    static float t = 0.0f;


    public bool goDown;
    public float multiplierLerpTime;

    [Header("---------COMMANDES / LIVE ---------")]
    public bool toto;


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
    }

    IEnumerator avatarsReveal()
    {
        yield return new WaitForSeconds(2);

        avatarsAudience.SetActive(true);
        otherAvatars = true;
    }

    void Update()
    {
        if (sceneNB == 1)
        {
            showAvatarJen();

            whiteLight.SetActive(true);
            redLight.SetActive(false);
            avatarsAudience2.SetActive(false);

            camSelector.activeCamera = 0;
            avatarPlaces.activePlace = 0;
            avatarsAudience.SetActive(false);
        }

        if (sceneNB == 2) // top shot
        {
            showAvatarJen();

            whiteLight.SetActive(true);
            redLight.SetActive(false);
            avatarsAudience2.SetActive(false);


            camSelector.activeCamera = 1;
            avatarPlaces.activePlace = 0;
            avatarsAudience.SetActive(false);
        }

        if (sceneNB == 3) // close shot fix
        {
            showAvatarJen();

            whiteLight.SetActive(false);
            redLight.SetActive(true);
            avatarsAudience2.SetActive(false);


            camSelector.activeCamera = 2;
            avatarPlaces.activePlace = 0;
            //avatarsAudience.SetActive(true); // a appeler dans une coroutine avec 2 secondes de delai

            if(otherAvatars == false)
                StartCoroutine(avatarsReveal());
        }

        if (sceneNB == 4) //scene 5 
        {
            showAvatarJen();

            whiteLight.SetActive(false);
            redLight.SetActive(true);
            avatarsAudience2.SetActive(false);

            camSelector.activeCamera = 4;
            avatarPlaces.activePlace = 0;
            avatarsAudience.SetActive(true);
        }

        if (sceneNB == 5) // scene 6
        {

            hideAvatarJen();

            whiteLight.SetActive(false);
            redLight.SetActive(true);
            avatarsAudience2.SetActive(false);

            camSelector.activeCamera = 5;
            setCamPublicLookAt(lookAtTargets[0].transform);

            avatarsAudience.SetActive(true);
        }

        if (sceneNB == 6) // scene 4
        {
            showAvatarJen();

            whiteLight.SetActive(false);
            redLight.SetActive(true);
            avatarsAudience2.SetActive(false);

            camSelector.activeCamera = 3;
            avatarPlaces.activePlace = 0;
            avatarsAudience.SetActive(true);
        }

        if(sceneNB == 7) // passage public
        {
            hideAvatarJen();

            whiteLight.SetActive(false);
            redLight.SetActive(true);
            avatarsAudience2.SetActive(false);

            camSelector.activeCamera = 5;
            setCamPublicLookAt(lookAtTargets[1].transform);

            avatarPlaces.activePlace = 1;
            avatarsAudience.SetActive(true);
        }

        if (sceneNB == 8) // scene 7
        {
            showAvatarJen();

            whiteLight.SetActive(false);
            redLight.SetActive(true);
            avatarsAudience2.SetActive(false);

            camSelector.activeCamera = 7;
            avatarPlaces.activePlace = 1;
            avatarsAudience.SetActive(true);
        }

        if (sceneNB == 9) // scene 6
        {
            hideAvatarJen();

            whiteLight.SetActive(false);
            redLight.SetActive(true);
            avatarsAudience2.SetActive(false);

            camSelector.activeCamera = 5;
            setCamPublicLookAt(lookAtTargets[2].transform);

            avatarPlaces.activePlace = 1;
            avatarsAudience.SetActive(true);
            avatarOrcDancing.SetActive(false);
        }

        if (sceneNB == 10) // scene 8
        {
            showAvatarJen();

            whiteLight.SetActive(false);
            redLight.SetActive(true);

            camSelector.activeCamera = 8;
            avatarPlaces.activePlace = 2;
            avatarsAudience.SetActive(true);
            avatarsAudience2.SetActive(true);

            avatarOrcDancing.SetActive(false);
        }


        // LERP UP AND DOWN
        elevationAvatar = 0.0f * t + 0.65f * (1 - t);

        if (goDown == true && t<=1.0f)
        {
            t += multiplierLerpTime;
        }

        else if(goDown == false && t>= 0.0f)
        {
            t -= multiplierLerpTime;
        }

        //APPLY HEIGHT
        avatarPlaces.places[avatarPlaces.activePlace].transform.position = new Vector3(avatarPlaces.places[avatarPlaces.activePlace].transform.position.x, elevationAvatar, avatarPlaces.places[avatarPlaces.activePlace].transform.position.z);


        //NOTES CONDUITE
        setNoteConduite();

    }

    public void hideAvatarJen()
    {
        for(int i=0; i<skinnedMRenders.Length; i++)
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
        if (sceneNB + 1 <= 10)
            sceneNB++;
    }

    public void ButtonBACK()
    {
        if (sceneNB - 1 > 0)
            sceneNB--;
    }

    public void setNoteConduite()
    {
        for(int i=0; i<notesConduites.Length; i++)
        {
            notesConduites[i].SetActive(false);
        }
        notesConduites[sceneNB - 1].SetActive(true);
    }

}
