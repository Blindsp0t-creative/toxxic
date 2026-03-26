using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class oscAutoAssign_vfx : MonoBehaviour
{
    public OSC server;
    private VisualEffect vfx;
    

    void Start()
    {

        vfx = GetComponent<VisualEffect>();
        if (vfx == null)
            Debug.Log("an't find VFX !!");


        server.SetAddressHandler("/FromVDMX/test", onReceiveTest);
    }

    void onMessage(OscMessage message)
    {
        Debug.Log(message);
    }


    void onReceiveTest(OscMessage message)
    {
        //Debug.Log("message" + message.address);
        vfx.SetFloat("depth", message.GetFloat(0));
    }




    
}
