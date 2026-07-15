using UnityEngine;
using System.Collections;
using UnityEditor;
public class blinkingEyes : MonoBehaviour
{
    public SkinnedMeshRenderer _skinnedMesh;
    public int eyeCloseBlendShapeIndex=19;

    [Header("Reglages")]
    public float speedOpenEyes = 1.0f;
    public float minValue = 30.0f;
    public float maxValue = 100.0f;

    void Start()
    {
        StartCoroutine(randomBlink());
    }

    void Update()
    {
        if(_skinnedMesh.GetBlendShapeWeight(eyeCloseBlendShapeIndex) >0.0f)
        {
            _skinnedMesh.SetBlendShapeWeight(eyeCloseBlendShapeIndex, _skinnedMesh.GetBlendShapeWeight(eyeCloseBlendShapeIndex)- speedOpenEyes);
        }
    }

    private IEnumerator randomBlink()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(2.0f, 5.0f));

            //animate blend shape here
            _skinnedMesh.SetBlendShapeWeight(eyeCloseBlendShapeIndex, Random.Range(minValue, maxValue));
        }
    }
}
