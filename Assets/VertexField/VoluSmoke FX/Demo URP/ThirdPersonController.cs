using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace VertexField.VoluSmokeFX
{
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonControllerPro : MonoBehaviour
    {

        [Header("Movement")]
        public float walkSpeed = 4.5f;
        public float sprintSpeed = 7.5f;
        public float acceleration = 18f;
        public float rotationSpeed = 12f;
        public bool rotateOnlyWhenMoving = true;


        [Header("Jump / Gravity")]
        public float jumpHeight = 2.0f;
        public float gravity = -20f;
        public float groundSnap = 4f;
        public float coyoteTime = 0.1f;


        [Header("Camera")]
        public Camera sceneCamera;
        public bool holdRightMouseToOrbit = true;
        public float mouseSensitivity = 210f;
        public bool invertY = false;
        public float minPitch = -30f;
        public float maxPitch = 70f;

        [Header("Camera Boom")]
        public float cameraHeight = 1.6f;
        public float cameraDistance = 5f;
        public float minDistance = 2f;
        public float maxDistance = 7.5f;
        public float zoomSpeed = 4f;
        public float camFollowDamp = 18f;

        [Header("Camera Collision")]
        public LayerMask cameraCollisionMask = ~0;
        public float cameraSphereRadius = 0.2f;
        public float cameraWallPadding = 0.1f;


        private CharacterController controller;
        private Transform camPivot;
        private float yaw, pitch;
        private Vector3 velocity;
        private float lastGroundedTime;
        private Vector3 planarVel;

        enum InputBackend { None, Legacy, InputSystem }
        InputBackend backend = InputBackend.None;
        NewInputBridge newInput;

        void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (!sceneCamera) sceneCamera = Camera.main;
            if (!sceneCamera)
            {
                Debug.LogError("ThirdPersonController: No Camera found. Assign a Camera.");
                enabled = false; return;
            }

            DetectInputBackend();

            camPivot = new GameObject("CameraPivot").transform;
            camPivot.SetPositionAndRotation(transform.position + new Vector3(0, cameraHeight, 0), Quaternion.identity);


            sceneCamera.transform.SetParent(camPivot, worldPositionStays: true);


            Vector3 toCam = (sceneCamera.transform.position - camPivot.position);
            if (toCam.sqrMagnitude < 0.001f)
            {
                yaw = transform.eulerAngles.y;
                pitch = 15f;
                sceneCamera.transform.position = camPivot.position - Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * cameraDistance;
                sceneCamera.transform.rotation = Quaternion.LookRotation(camPivot.position - sceneCamera.transform.position, Vector3.up);
            }
            else
            {
                Quaternion look = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
                var e = look.eulerAngles;
                yaw = e.y;
                pitch = NormalizeSignedAngle(e.x);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (backend == InputBackend.InputSystem)
                newInput.Refresh();

            HandleCameraInput(dt);


            HandleMovement(dt);


            HandleJumpAndGravity(dt);
        }

        void LateUpdate()
        {

            HandleCameraRig(Time.deltaTime);
        }


        // ---- Input abstraction ----
        // The new Input System is accessed via reflection so this script compiles and runs
        // whether or not com.unity.inputsystem is installed, in any Active Input Handling mode.
        // Priority: new Input System (if present and enabled) -> legacy Input -> none.

        void DetectInputBackend()
        {
            newInput = NewInputBridge.TryCreate();
            if (newInput != null && newInput.Refresh())
            {
                backend = InputBackend.InputSystem;
                return;
            }

            try
            {
                Input.GetKey(KeyCode.W);
                backend = InputBackend.Legacy;
            }
            catch (InvalidOperationException)
            {
                backend = InputBackend.None;
                Debug.LogWarning("ThirdPersonController: no usable input backend found (Input System package missing and legacy Input Manager disabled). Input is disabled.");
            }
        }

        float GetScrollNotches()
        {
            switch (backend)
            {
                case InputBackend.InputSystem: return newInput.ScrollNotches();
                case InputBackend.Legacy: return Input.mouseScrollDelta.y;
                default: return 0f;
            }
        }

        bool GetRightMouseHeld()
        {
            switch (backend)
            {
                case InputBackend.InputSystem: return newInput.RightMouseHeld();
                case InputBackend.Legacy: return Input.GetMouseButton(1);
                default: return false;
            }
        }

        Vector2 GetMouseDelta()
        {
            switch (backend)
            {
                case InputBackend.InputSystem: return newInput.MouseDelta();
                case InputBackend.Legacy:
                    // Legacy axes are pre-scaled (default Input Manager sensitivity 0.1),
                    // so scale back up to approximate pixel deltas like the new Input System.
                    return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f;
                default: return Vector2.zero;
            }
        }

        Vector2 GetMoveInput()
        {
            float h = 0f, v = 0f;
            switch (backend)
            {
                case InputBackend.InputSystem:
                    if (newInput.KeyPressed("dKey") || newInput.KeyPressed("rightArrowKey")) h += 1f;
                    if (newInput.KeyPressed("aKey") || newInput.KeyPressed("leftArrowKey")) h -= 1f;
                    if (newInput.KeyPressed("wKey") || newInput.KeyPressed("upArrowKey")) v += 1f;
                    if (newInput.KeyPressed("sKey") || newInput.KeyPressed("downArrowKey")) v -= 1f;
                    break;
                case InputBackend.Legacy:
                    if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
                    if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
                    if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
                    if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
                    break;
            }
            return new Vector2(h, v);
        }

        bool GetSprintHeld()
        {
            switch (backend)
            {
                case InputBackend.InputSystem: return newInput.KeyPressed("leftShiftKey");
                case InputBackend.Legacy: return Input.GetKey(KeyCode.LeftShift);
                default: return false;
            }
        }

        bool GetJumpPressed()
        {
            switch (backend)
            {
                case InputBackend.InputSystem: return newInput.KeyWasPressedThisFrame("spaceKey");
                case InputBackend.Legacy: return Input.GetKeyDown(KeyCode.Space);
                default: return false;
            }
        }

        void HandleCameraInput(float dt)
        {
            float scroll = GetScrollNotches();
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                cameraDistance -= scroll * zoomSpeed;
                cameraDistance = Mathf.Clamp(cameraDistance, minDistance, maxDistance);
            }

            bool orbiting = !holdRightMouseToOrbit || GetRightMouseHeld();
            if (!orbiting) return;

            Vector2 mouseDelta = GetMouseDelta();

            yaw += mouseDelta.x * mouseSensitivity * dt;
            pitch += (invertY ? 1f : -1f) * mouseDelta.y * mouseSensitivity * dt;

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            yaw = WrapAngle360(yaw);
        }


        void HandleMovement(float dt)
        {
            Vector3 camFwd = YawToForward(yaw);
            Vector3 camRight = new Vector3(camFwd.z, 0f, -camFwd.x);

            Vector2 move = GetMoveInput();
            Vector3 input = new Vector3(move.x, 0f, move.y);
            input = Vector3.ClampMagnitude(input, 1f);


            float targetSpeed = GetSprintHeld() ? sprintSpeed : walkSpeed;
            Vector3 targetPlanar = (camFwd * input.z + camRight * input.x) * targetSpeed;


            planarVel = Vector3.MoveTowards(planarVel, targetPlanar, acceleration * dt);


            controller.Move(planarVel * dt);


            if (planarVel.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(planarVel.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * dt);
            }
            else if (!rotateOnlyWhenMoving)
            {

                Quaternion toCam = Quaternion.Euler(0f, yaw, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, toCam, rotationSpeed * dt);
            }
        }


        void HandleJumpAndGravity(float dt)
        {
            bool jumpPressed = GetJumpPressed();
            bool grounded = controller.isGrounded;

            if (grounded)
            {
                lastGroundedTime = Time.time;


                if (velocity.y < 0f) velocity.y = -groundSnap;

                if (jumpPressed)
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }
            else
            {

                if ((Time.time - lastGroundedTime) <= coyoteTime && jumpPressed)
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }


            velocity.y += gravity * dt;
            controller.Move(velocity * dt);
        }


        void HandleCameraRig(float dt)
        {

            Vector3 targetPivotPos = transform.position + new Vector3(0f, cameraHeight, 0f);
            camPivot.position = Vector3.Lerp(camPivot.position, targetPivotPos, 1f - Mathf.Exp(-camFollowDamp * dt));


            Quaternion camRot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPos = camPivot.position - (camRot * Vector3.forward) * cameraDistance;


            Vector3 toCam = desiredPos - camPivot.position;
            float dist = toCam.magnitude;

            Vector3 finalPos = desiredPos;
            if (dist > 0.01f && Physics.SphereCast(camPivot.position, cameraSphereRadius, toCam.normalized, out RaycastHit hit, dist, cameraCollisionMask, QueryTriggerInteraction.Ignore))
            {
                finalPos = camPivot.position + toCam.normalized * Mathf.Max(hit.distance - cameraWallPadding, 0.1f);
            }


            sceneCamera.transform.position = finalPos;
            sceneCamera.transform.rotation = Quaternion.LookRotation(camPivot.position - sceneCamera.transform.position, Vector3.up);
        }


        static Vector3 YawToForward(float yawDeg)
        {
            float r = yawDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(r), 0f, Mathf.Cos(r)).normalized;
        }

        static float NormalizeSignedAngle(float angle)
        {

            float a = Mathf.Repeat(angle + 180f, 360f) - 180f;
            return a;
        }

        static float WrapAngle360(float angle)
        {
            return Mathf.Repeat(angle, 360f);
        }


        // Reflection bridge to UnityEngine.InputSystem. Never referenced at compile time,
        // so the script builds even when the Input System package is not installed.
        class NewInputBridge
        {
            readonly PropertyInfo keyboardCurrent;
            readonly PropertyInfo mouseCurrent;
            readonly Dictionary<string, PropertyInfo> propCache = new Dictionary<string, PropertyInfo>();
            readonly Dictionary<Type, MethodInfo> readValueCache = new Dictionary<Type, MethodInfo>();

            object keyboard, mouse;

            NewInputBridge(PropertyInfo kbCurrent, PropertyInfo mCurrent)
            {
                keyboardCurrent = kbCurrent;
                mouseCurrent = mCurrent;
            }

            public static NewInputBridge TryCreate()
            {
                var kbType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
                var mType = Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
                if (kbType == null || mType == null) return null;

                var kbCurrent = kbType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                var mCurrent = mType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                if (kbCurrent == null || mCurrent == null) return null;

                return new NewInputBridge(kbCurrent, mCurrent);
            }

            // Re-grabs current devices; returns false when the Input System is inactive
            // (package installed but Active Input Handling set to legacy only).
            public bool Refresh()
            {
                try
                {
                    keyboard = keyboardCurrent.GetValue(null);
                    mouse = mouseCurrent.GetValue(null);
                }
                catch
                {
                    keyboard = null;
                    mouse = null;
                }
                return keyboard != null || mouse != null;
            }

            public bool KeyPressed(string keyPropName)
            {
                return ReadBool(GetMember(keyboard, keyPropName), "isPressed");
            }

            public bool KeyWasPressedThisFrame(string keyPropName)
            {
                return ReadBool(GetMember(keyboard, keyPropName), "wasPressedThisFrame");
            }

            public bool RightMouseHeld()
            {
                return ReadBool(GetMember(mouse, "rightButton"), "isPressed");
            }

            public Vector2 MouseDelta()
            {
                return ReadVector2(GetMember(mouse, "delta"));
            }

            public float ScrollNotches()
            {
                return ReadVector2(GetMember(mouse, "scroll")).y / 120f;
            }

            object GetMember(object obj, string propName)
            {
                if (obj == null) return null;
                Type t = obj.GetType();
                string key = t.FullName + "." + propName;
                if (!propCache.TryGetValue(key, out PropertyInfo prop))
                {
                    prop = t.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                    propCache[key] = prop;
                }
                return prop != null ? prop.GetValue(obj) : null;
            }

            bool ReadBool(object control, string propName)
            {
                object value = GetMember(control, propName);
                return value is bool b && b;
            }

            Vector2 ReadVector2(object control)
            {
                if (control == null) return Vector2.zero;
                Type t = control.GetType();
                if (!readValueCache.TryGetValue(t, out MethodInfo read))
                {
                    read = t.GetMethod("ReadValue", Type.EmptyTypes);
                    readValueCache[t] = read;
                }
                if (read == null) return Vector2.zero;
                object value = read.Invoke(control, null);
                return value is Vector2 v ? v : Vector2.zero;
            }
        }
    }


}
