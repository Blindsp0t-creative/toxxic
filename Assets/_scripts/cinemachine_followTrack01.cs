using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class cinemachine_followTrack01 : MonoBehaviour
{

    private CinemachineVirtualCamera vCam;
    private CinemachineTrackedDolly trackedDolly;


    [Range(0,1)]
    public float speed;

    public bool loop;

    void Start()
    {
        vCam = GetComponent<CinemachineVirtualCamera>();
        trackedDolly = vCam.GetCinemachineComponent<CinemachineTrackedDolly>();
    }

    void Update()
    {
        if (trackedDolly != null)
        {
            trackedDolly.m_PathPosition += speed / 10.0f;
        }
            
        else
        {
            vCam = GetComponent<CinemachineVirtualCamera>();
            trackedDolly = vCam.GetCinemachineComponent<CinemachineTrackedDolly>();
        }

        if(loop && trackedDolly.m_PathPosition >= 1)
        {
            trackedDolly.m_PathPosition = 0;
        }
    }
}
