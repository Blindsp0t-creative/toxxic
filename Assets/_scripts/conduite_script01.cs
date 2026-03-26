using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class conduite_script01 : MonoBehaviour
{

    public cameraSelector camSelector;
    public placeTrendmillAvatar avatarPlaces;
    public GameObject avatarsAudience;
    public GameObject avatarsAudience2;

    public GameObject whiteLight;
    public GameObject redLight;

    //ajouter le style de transition entre cmaeras ? 

    [Range(1,8)]
    public int sceneNB;

    [Range(-1,1)]
    public float elevationAvatar;

    private bool otherAvatars = false;


    void Start()
    {
        camSelector.activeCamera = 0;
        avatarPlaces.activePlace = 0;
        avatarsAudience.SetActive(false);
        avatarsAudience2.SetActive(false);

        whiteLight.SetActive(true);
        redLight.SetActive(false);
    }

    IEnumerator avatarsReveal()
    {
        //Debug.Log("started coroutine");
        yield return new WaitForSeconds(2);

        //Debug.Log("reveal avatars now");
        avatarsAudience.SetActive(true);
        otherAvatars = true;
    }

    void Update()
    {
        if(sceneNB == 1)
        {
            whiteLight.SetActive(true);
            redLight.SetActive(false);
            avatarsAudience2.SetActive(false);

            camSelector.activeCamera = 0;
            avatarPlaces.activePlace = 0;
            avatarsAudience.SetActive(false);
        }

        if (sceneNB == 2) // top shot
        {
            whiteLight.SetActive(true);
            redLight.SetActive(false);
            avatarsAudience2.SetActive(false);


            camSelector.activeCamera = 1;
            avatarPlaces.activePlace = 0;
            avatarsAudience.SetActive(false);
        }

        if (sceneNB == 3) // close shot fix
        {
            whiteLight.SetActive(false);
            redLight.SetActive(true);
            avatarsAudience2.SetActive(false);


            camSelector.activeCamera = 2;
            avatarPlaces.activePlace = 0;
            //avatarsAudience.SetActive(true); // a appeler dans une coroutine avec 2 secondes de delai

            if(otherAvatars == false)
                StartCoroutine(avatarsReveal());
        }

        if (sceneNB == 4) // shot avec public (fix)
        {
            whiteLight.SetActive(false);
            redLight.SetActive(true);
            avatarsAudience2.SetActive(false);


            camSelector.activeCamera = 3;
            avatarPlaces.activePlace = 0;
            avatarsAudience.SetActive(true);
        }

        if (sceneNB == 5) // dolly rotative
        {
            whiteLight.SetActive(false);
            redLight.SetActive(true);
            avatarsAudience2.SetActive(false);


            camSelector.activeCamera = 4;
            avatarPlaces.activePlace = 0;
            avatarsAudience.SetActive(true);
        }

        if (sceneNB == 6) // camera parcours public
        {
            whiteLight.SetActive(false);
            redLight.SetActive(true);
            avatarsAudience2.SetActive(false);


            camSelector.activeCamera = 5;
            avatarPlaces.activePlace = 1;
            avatarsAudience.SetActive(true);
        }

        if(sceneNB == 7) // camera danse mec 1
        {
            whiteLight.SetActive(false);
            redLight.SetActive(true);
            avatarsAudience2.SetActive(false);


            camSelector.activeCamera = 7;
            avatarPlaces.activePlace = 1;
            avatarsAudience.SetActive(true);
        }

        if (sceneNB == 8) // camera danse mec 2
        {
            whiteLight.SetActive(false);
            redLight.SetActive(true);

            camSelector.activeCamera = 8;
            avatarPlaces.activePlace = 2;
            avatarsAudience.SetActive(true);
            avatarsAudience2.SetActive(true);
        }

        avatarPlaces.places[avatarPlaces.activePlace].transform.position = new Vector3(avatarPlaces.places[avatarPlaces.activePlace].transform.position.x, elevationAvatar, avatarPlaces.places[avatarPlaces.activePlace].transform.position.z);

    }

}
