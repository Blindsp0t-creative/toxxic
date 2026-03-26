using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class particlesSometimes : MonoBehaviour
{
    private ParticleSystem tmp;
    private float time;

    void Start()
    {
        tmp = GetComponent<ParticleSystem>();
        if (tmp == null)
            Debug.Log("cannot find particle system");

        time = Time.time;

    }

    void Update()
    {
        if(Time.time - time > 5)
        {
            tmp.enableEmission = false;
            time = Time.time;

            //random activation
            if(Random.Range(0,1.0f) > 0.75)
                tmp.enableEmission = true;
        }
    }
}
