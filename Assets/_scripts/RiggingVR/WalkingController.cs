using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkingController : MonoBehaviour
{
    [SerializeField]
    private float speedMinimum = 0.05f;
    [SerializeField, Range(0,1)]
    private float lerpFactor = 0.3f;

    private Animator animator;
    private HeadBodyRig rig;
    private Transform headTarget;
    private Vector3 previousPosition;

    public GameObject vrTargetCUSTOM;
    private Rigidbody rgTargetCUSTOM;
    public float speedMinimumRotation;

    private Quaternion lastRot;



    internal static Vector3 GetAngularVelocity(Quaternion foreLastFrameRotation, Quaternion lastFrameRotation)
    {
        var q = lastFrameRotation * Quaternion.Inverse(foreLastFrameRotation);
        // no rotation?
        // You may want to increase this closer to 1 if you want to handle very small rotations.
        // Beware, if it is too close to one your answer will be Nan
        if (Mathf.Abs(q.w) > 1023.5f / 1024.0f)
            return new Vector3(0, 0, 0);
        float gain;
        // handle negatives, we could just flip it but this is faster
        if (q.w < 0.0f)
        {
            var angle = Mathf.Acos(-q.w);
            gain = -2.0f * angle / (Mathf.Sin(angle) * Time.deltaTime);
        }
        else
        {
            var angle = Mathf.Acos(q.w);
            gain = 2.0f * angle / (Mathf.Sin(angle) * Time.deltaTime);
        }
        return new Vector3(q.x * gain, q.y * gain, q.z * gain);
    }




    private void Start()
    {
        animator = GetComponent<Animator>();
        rig = GetComponent<HeadBodyRig>();
        headTarget = rig.head.VRTarget;
        previousPosition = headTarget.position;

        rgTargetCUSTOM = vrTargetCUSTOM.GetComponent<Rigidbody>();
    }

    private void Update() //FixedUpdate()
    {

        //var deltaRot = transform.rotation * Quaternion.Inverse(lastRot);
        // var eulerRot = new Vector3(Mathf.DeltaAngle(0, deltaRot.eulerAngles.x), Mathf.DeltaAngle(0, deltaRot.eulerAngles.y), Mathf.DeltaAngle(0, deltaRot.eulerAngles.z));

        float angularVelocity = GetAngularVelocity(lastRot, rgTargetCUSTOM.transform.rotation).magnitude;
        //Debug.Log("angular velocity : " + angularVelocity);

        Vector3 headSpeed = (headTarget.position - previousPosition) / Time.deltaTime;

        float headRotationSpeed = rgTargetCUSTOM.angularVelocity.magnitude;
        Debug.Log(headRotationSpeed);

        headSpeed.y = 0;
        Vector3 localHeadSpeed = transform.InverseTransformDirection(headSpeed);
        previousPosition = headTarget.position;

        float previousX = animator.GetFloat("x");
        float previousY = animator.GetFloat("y");

        //Debug.Log("isWalking ?" + (localHeadSpeed.magnitude).ToString());
        //Debug.Log("headSpeed ?" + (headSpeed.magnitude).ToString());

        animator.SetBool("isWalking", localHeadSpeed.magnitude > speedMinimum);
        animator.SetFloat("x", Mathf.Lerp(previousX, Mathf.Clamp(localHeadSpeed.x, -1, 1), lerpFactor)); //left right turn
        animator.SetFloat("y", Mathf.Lerp(previousY, Mathf.Clamp(localHeadSpeed.z, -1, 1), lerpFactor)); //walking / backward

        if(angularVelocity > speedMinimumRotation)
        {
            animator.SetBool("isWalking", true);
            animator.SetFloat("x", Mathf.Lerp(previousX, Mathf.Clamp(headRotationSpeed, -1, 1), lerpFactor));
        }

        lastRot = rgTargetCUSTOM.transform.rotation;

    }
}
