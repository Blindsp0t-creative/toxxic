using UnityEngine;

public class VRAnimatorController : MonoBehaviour
{
    public float speedThreshold = 0.1f;
    [Range(0f, 1f)]
    public float smoothing=1.0f;

    private Animator animator;
    private Vector3 previousPos;
    public GameObject vrRig; //vrRig = VR Head Target ?

    void Start()
    {
        animator = GetComponent<Animator>();
        previousPos = vrRig.transform.position;
    }

    void Update()
    {
        //compute speed
        Vector3 headsetSpeed = (vrRig.transform.position - previousPos) / Time.deltaTime;
        headsetSpeed.y = 0;

        Vector3 headsetLocalSpeed = transform.InverseTransformDirection(headsetSpeed);
        previousPos = vrRig.transform.position;


        float previousDirectionX = animator.GetFloat("DirectionX");
        float previousDirectionY = animator.GetFloat("DirectionY");


        animator.SetBool("isMoving", headsetLocalSpeed.magnitude > speedThreshold);
        animator.SetFloat("DirectionX", Mathf.Lerp(previousDirectionX,   Mathf.Clamp(headsetLocalSpeed.x, -1, 1), smoothing));
        animator.SetFloat("DirectionY", Mathf.Lerp(previousDirectionY,   Mathf.Clamp(headsetLocalSpeed.z, -1, 1), smoothing));

    }
}
