using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FPS : MonoBehaviour
{

    private TextMeshProUGUI _txt;
    private float currentFPS;

    void Start()
    {
        _txt = GetComponent<TextMeshProUGUI>();
        if (_txt == null)
            Debug.Log("text is NULL");
    }

    void Update()
    {
        currentFPS = 1f / Time.deltaTime;
        _txt.text = "fps : " + Mathf.RoundToInt(currentFPS);
    }
}
