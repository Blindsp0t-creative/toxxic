using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class placeTrendmillAvatar : MonoBehaviour
{


    public GameObject[] places;
    [Range(0, 2)]
    public int activePlace;


    void Start()
    {
        
    }

    void Update()
    {
        this.transform.position = places[activePlace].transform.position;
    }
}
