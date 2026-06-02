using UnityEngine;

[ExecuteInEditMode] // Script runs even without pressing Play 

public class MirrorReflection : MonoBehaviour 
{ [Header("Mirror Settings")] 
    public Camera mirrorCamera; // The dedicated mirror camera public RenderTexture renderTexture; // The render texture we created

    private Camera mainCamera;

    void Start()
    {
        // Grab the player/main camera
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (mainCamera == null || mirrorCamera == null) return;

        // ── STEP 1: Get mirror plane normal (forward direction) ──
        Vector3 mirrorNormal = transform.forward;

        // ── STEP 2: Get main camera world position ──
        Vector3 camPos = mainCamera.transform.position;

        // ── STEP 3: Calculate distance from camera to mirror plane ──
        float distToMirror = Vector3.Dot(
            camPos - transform.position, 
            mirrorNormal
        );

        // ── STEP 4: Reflect camera POSITION across mirror plane ──
        Vector3 reflectedPos = camPos - 2f * distToMirror * mirrorNormal;
        mirrorCamera.transform.position = reflectedPos;

        // ── STEP 5: Reflect camera DIRECTION across mirror plane ──
        Vector3 lookDir = Vector3.Reflect(
            mainCamera.transform.forward,
            mirrorNormal
        );

        // ── STEP 6: Reflect camera UP vector too ──
        Vector3 reflectedUp = Vector3.Reflect(
            mainCamera.transform.up,
            mirrorNormal
        );

        // ── STEP 7: Apply reflected rotation to mirror camera ──
        mirrorCamera.transform.rotation = Quaternion.LookRotation(
            lookDir,
            reflectedUp
        );

        // ── STEP 8: Match FOV to main camera ──
        mirrorCamera.fieldOfView = mainCamera.fieldOfView;
    }
}