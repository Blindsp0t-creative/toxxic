using UnityEngine;
using System.Collections;

public class Set_VR_Avatar_height : MonoBehaviour
{
    public float height = 1.9f;

    private void Start()
    {
        //Debug.Log("start coroutine height Avatar");
        StartCoroutine(setHeightAfterSeconds(1));
        StartCoroutine(setHeightAfterSeconds(3));

    }


    IEnumerator setHeightAfterSeconds(float _seconds)
    {
        yield return new WaitForSeconds(_seconds);
        this.transform.position = new Vector3(0,height,0);
        //Debug.Log("-- coroutine height Avatar done --");
    }


    public void setHeight(float value)
    {
        height = value;
    }

    public void Update()
    {
        transform.position = new Vector3(0, height, 0);
    }
}
