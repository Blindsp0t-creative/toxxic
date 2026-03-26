using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraSelector : MonoBehaviour
{
    public GameObject[] cameras;

    [Range(0, 11)]
    public int activeCamera;

    private float time;

    int priority = 10;

    void Start()
    {
        priority++;
    }

    void Update()
    {
        if(Time.time - time > 2)
        {
            /*
            for (int i = 0; i < cameras.Length; i++)
            {
                cameras[i].GetComponent<Cinemachine.CinemachineVirtualCamera>().enabled = false;
            }
            */
            cameras[activeCamera].GetComponent<Cinemachine.CinemachineVirtualCamera>().Priority = priority ++ ;
        }
    }
}
