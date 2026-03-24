using UnityEngine;

namespace Oneiric.Superposition
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public class SimplePlayerWalker : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform cameraPivot;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float sprintSpeed = 6f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float jumpVelocity;

        [Header("Look")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float verticalLookLimit = 80f;
        [SerializeField] private bool lockCursorOnStart = true;

        [Header("Grounding")]
        [SerializeField] private float groundedStickForce = -2f;

        private Vector3 planarVelocity;
        private float verticalVelocity;
        private float pitch;
        private bool cursorLocked;

        public Transform CameraPivot => cameraPivot;

        private void Reset()
        {
            characterController = GetComponent<CharacterController>();
            Camera childCamera = GetComponentInChildren<Camera>();
            cameraPivot = childCamera != null ? childCamera.transform : transform;
        }

        private void Awake()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (cameraPivot == null)
            {
                Camera childCamera = GetComponentInChildren<Camera>();
                cameraPivot = childCamera != null ? childCamera.transform : transform;
            }

            pitch = cameraPivot != null ? cameraPivot.localEulerAngles.x : 0f;
            if (pitch > 180f)
            {
                pitch -= 360f;
            }
        }

        private void Start()
        {
            if (lockCursorOnStart)
            {
                SetCursorLocked(true);
            }
        }

        private void Update()
        {
            HandleCursor();
            HandleLook();
            HandleMovement();
        }

        private void HandleCursor()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetCursorLocked(false);
            }
            else if (Input.GetMouseButtonDown(0) && !cursorLocked)
            {
                SetCursorLocked(true);
            }
        }

        private void HandleLook()
        {
            if (!cursorLocked || cameraPivot == null)
            {
                return;
            }

            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX, Space.Self);

            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -verticalLookLimit, verticalLookLimit);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);

            float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
            Vector3 desiredVelocity = (transform.right * moveInput.x + transform.forward * moveInput.y) * targetSpeed;
            planarVelocity = Vector3.Lerp(planarVelocity, desiredVelocity, 1f - Mathf.Exp(-acceleration * Time.deltaTime));

            if (characterController.isGrounded)
            {
                verticalVelocity = groundedStickForce;

                if (jumpVelocity > 0f && Input.GetButtonDown("Jump"))
                {
                    verticalVelocity = jumpVelocity;
                }
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            Vector3 frameVelocity = planarVelocity + Vector3.up * verticalVelocity;
            characterController.Move(frameVelocity * Time.deltaTime);
        }

        private void SetCursorLocked(bool shouldLock)
        {
            cursorLocked = shouldLock;
            Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shouldLock;
        }
    }
}
