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

            AVATAR.GetComponent<RigBuilder>().enabled = false;
            LOCOMOTOR.SetActive(false);
            INTERACTION.SetActive(false);

            AVATAR.transform.position = pellePosition.transform.position;

            
            //locomotor.enabled = false;
            //locomotorRoot.SetActive(false);
            
            //vrRoot.transform.position = pellePosition.transform.position;
            //cameraRigXR.transform.localPosition = new pelleteuse_Vector3(0, 0, 0);

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



}
