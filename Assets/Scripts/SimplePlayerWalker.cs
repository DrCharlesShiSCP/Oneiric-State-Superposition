using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Oneiric.Superposition
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public class SimplePlayerWalker : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float sprintSpeed = 7.5f;
        [SerializeField] private float mouseLookSensitivity = 0.12f;
        [SerializeField] private float gravity = 25f;
        [SerializeField] private float lookPitchLimit = 80f;
        [SerializeField] private bool lockCursorOnPlay = true;

        private CharacterController characterController;
        private float pitch;
        private float verticalVelocity;

        public Transform CameraTransform
        {
            get => cameraTransform;
            set => cameraTransform = value;
        }

        private void Reset()
        {
            characterController = GetComponent<CharacterController>();

            Camera childCamera = GetComponentInChildren<Camera>(true);
            if (childCamera != null)
            {
                cameraTransform = childCamera.transform;
            }
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (cameraTransform == null)
            {
                Camera childCamera = GetComponentInChildren<Camera>(true);
                if (childCamera != null)
                {
                    cameraTransform = childCamera.transform;
                }
            }

            if (cameraTransform != null)
            {
                pitch = NormalizePitch(cameraTransform.localEulerAngles.x);
            }
        }

        private void Start()
        {
            if (lockCursorOnPlay)
            {
                SetCursorLocked(true);
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && lockCursorOnPlay && Cursor.lockState != CursorLockMode.None)
            {
                SetCursorLocked(true);
            }
        }

        private void Update()
        {
            HandleCursorState();
            HandleLook();
            HandleMovement();
        }

        public void AssignCameraTransform(Transform targetCamera)
        {
            cameraTransform = targetCamera;
            if (cameraTransform != null)
            {
                pitch = NormalizePitch(cameraTransform.localEulerAngles.x);
            }
        }

        private void HandleCursorState()
        {
            if (WasEscapePressedThisFrame())
            {
                SetCursorLocked(false);
            }
            else if (lockCursorOnPlay && Cursor.lockState == CursorLockMode.None && WasPrimaryClickPressedThisFrame())
            {
                SetCursorLocked(true);
            }
        }

        private void HandleLook()
        {
            if (cameraTransform == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            Vector2 lookInput = ReadLookInput();
            float yaw = lookInput.x * mouseLookSensitivity;
            float pitchDelta = lookInput.y * mouseLookSensitivity;

            transform.Rotate(Vector3.up * yaw, Space.Self);

            pitch = Mathf.Clamp(pitch - pitchDelta, -lookPitchLimit, lookPitchLimit);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            Vector2 moveInput = ReadMoveInput();
            Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
            float moveSpeed = IsSprinting() ? sprintSpeed : walkSpeed;

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity -= gravity * Time.deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, -gravity * 2f);

            Vector3 frameMotion = moveDirection * moveSpeed;
            frameMotion.y = verticalVelocity;

            characterController.Move(frameMotion * Time.deltaTime);
        }

        private static float NormalizePitch(float rawPitch)
        {
            if (rawPitch > 180f)
            {
                rawPitch -= 360f;
            }

            return rawPitch;
        }

        private void SetCursorLocked(bool shouldLock)
        {
            Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shouldLock;
        }

        private bool IsSprinting()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
#else
            return Input.GetKey(KeyCode.LeftShift);
#endif
        }

        private Vector2 ReadMoveInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return Vector2.zero;
            }

            Vector2 moveInput = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) moveInput.y += 1f;
            if (Keyboard.current.sKey.isPressed) moveInput.y -= 1f;
            if (Keyboard.current.dKey.isPressed) moveInput.x += 1f;
            if (Keyboard.current.aKey.isPressed) moveInput.x -= 1f;

            return Vector2.ClampMagnitude(moveInput, 1f);
#else
            Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            return Vector2.ClampMagnitude(moveInput, 1f);
#endif
        }

        private Vector2 ReadLookInput()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f;
#endif
        }

        private bool WasEscapePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private bool WasPrimaryClickPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }
    }
}
