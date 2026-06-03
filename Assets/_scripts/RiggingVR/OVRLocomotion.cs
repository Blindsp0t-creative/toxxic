using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class OVRLocomotion : MonoBehaviour
{
    [Header("Références")]
    public OVRCameraRig cameraRig;

    [Header("Déplacement (joystick gauche)")]
    public float moveSpeed = 3.0f;
    public bool enableStrafe = true;

    [Header("Rotation continue (joystick droit)")]
    public float rotationSpeed = 90f;      // degrés par seconde
    public float rotationDeadzone = 0.2f;  // seuil d'activation

    [Header("Gravité")]
    public float gravity = -9.81f;
    private float _verticalVelocity = 0f;

    private CharacterController _cc;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (cameraRig == null)
            cameraRig = GetComponentInChildren<OVRCameraRig>();
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    void HandleMovement()
    {
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        Transform cam = cameraRig.centerEyeAnchor;
        Vector3 forward = cam.forward; forward.y = 0f; forward.Normalize();
        Vector3 right   = cam.right;   right.y   = 0f; right.Normalize();

        Vector3 move = (forward * input.y) + (enableStrafe ? right * input.x : Vector3.zero);

        if (_cc.isGrounded)
            _verticalVelocity = -0.5f;
        else
            _verticalVelocity += gravity * Time.deltaTime;

        move.y = _verticalVelocity;
        _cc.Move(move * moveSpeed * Time.deltaTime);
    }

    /*
    void HandleRotation()
    {
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        if (Mathf.Abs(input.x) < rotationDeadzone) return;

        // Pivoter autour de la position exacte de la tête (CenterEyeAnchor)
        Vector3 headPos = cameraRig.centerEyeAnchor.position;
        float angle = input.x * rotationSpeed * Time.deltaTime;
        transform.RotateAround(headPos, Vector3.up, angle);
    }
    */

    void HandleRotation()
    {
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        if (Mathf.Abs(input.x) < rotationDeadzone) return;

        float angle = input.x * rotationSpeed * Time.deltaTime;

        // Position de la tête dans l'espace monde
        Vector3 headPos = cameraRig.centerEyeAnchor.position;

        // Calculer le décalage entre la tête et le pivot de l'OVRCameraRig
        Vector3 offset = transform.position - headPos;

        // Faire pivoter ce décalage autour de Y
        offset = Quaternion.Euler(0, angle, 0) * offset;

        // Nouvelle position cible de l'OVRCameraRig
        Vector3 targetPosition = headPos + offset;

        // Déplacer via CharacterController (respecte les collisions)
        Vector3 delta = targetPosition - transform.position;
        _cc.Move(delta);

        // Rotation pure du GameObject (pas de déplacement ici)
        transform.Rotate(0, angle, 0, Space.World);
    }
}