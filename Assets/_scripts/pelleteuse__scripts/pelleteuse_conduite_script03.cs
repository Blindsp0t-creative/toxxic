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



    public Material materialQuad;

    //pelleteuse_OSC
    public OSC _handler;

    private bool done = false;

    void Start()
    {
        sceneNB = 1;
        canvasBlackOut.SetActive(false);
        camSelector.activeCamera = 0;
        photoQuad.SetActive(false);

        //materialQuad.color = new Color(255,255,255, 255);


        //INIT pelleteuse_OSC
        /*
        _handler.SetAllMessageHandler(allMessages);
        _handler.SetAddressHandler("/osc/next", onMessageNext);
        _handler.SetAddressHandler("/osc/back", onMessageBack);
        _handler.SetAddressHandler("/osc/calib", onMessageCalib);
        */
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

        
        if (sceneNB == 1)
        {
            camSelector.activeCamera = 0;
        }

        if (sceneNB == 2) 
        {            
            camSelector.activeCamera = 1;
        }

        if (sceneNB == 3) 
        {
            camSelector.activeCamera = 2;
        }


        if (sceneNB == 4) 
        {
            
            photoQuad.SetActive(true);
            //photoQuad.GetComponent<Renderer>().material.color = new Color(255, 255, 255, 255);
            //materialQuad.color = new Color(255, 255, 255, 255);
            camSelector.activeCamera = 5;
        }

        if (sceneNB == 5) 
        {

            //DISPARITION EN FADE DU QUAD
            StartCoroutine(hideQuad());


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

        if (sceneNB == 6) 
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
}
