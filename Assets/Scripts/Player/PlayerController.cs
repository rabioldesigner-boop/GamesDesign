using UnityEngine;
using UnityEngine.InputSystem;

namespace TombOfServilii
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Mouse Look Settings")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float verticalClampMin = -80f;
        [SerializeField] private float verticalClampMax = 80f;

        [Header("References")]
        public Camera playerCamera;
        public GameObject interactHintUI;
        public GateInteraction gateInteraction;
        public HandAnimationController handAnimator;

        [Header("Interaction Settings")]
        [SerializeField] private float interactionDistance = 3f;

        private CharacterController characterController;
        private float verticalRotation = 0f;
        private Vector3 velocity;
        private bool isGrounded;

        private void Start()
        {
            characterController = GetComponent<CharacterController>();

            // Lock and hide cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (interactHintUI != null)
            {
                interactHintUI.SetActive(false);
            }
        }

        private void Update()
        {
            HandleLook();
            HandleMovement();
            HandleInteraction();
        }

        private void HandleLook()
        {
            if (playerCamera == null) return;

            Vector2 mouseDelta = Vector2.zero;
            if (Mouse.current != null)
            {
                mouseDelta = Mouse.current.delta.ReadValue();
            }

            // Horizontal rotation (turns the player body)
            float mouseX = mouseDelta.x * mouseSensitivity * 0.1f;
            transform.Rotate(Vector3.up * mouseX);

            // Vertical rotation (pitches the camera)
            float mouseY = mouseDelta.y * mouseSensitivity * 0.1f;
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, verticalClampMin, verticalClampMax);

            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        private void HandleMovement()
        {
            // Ground check
            isGrounded = characterController.isGrounded;
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // Slight downward force to keep grounded
            }

            // Get input
            Vector2 moveInput = Vector2.zero;
            if (Keyboard.current != null)
            {
                float x = 0;
                float z = 0;
                if (Keyboard.current.wKey.isPressed) z += 1f;
                if (Keyboard.current.sKey.isPressed) z -= 1f;
                if (Keyboard.current.aKey.isPressed) x -= 1f;
                if (Keyboard.current.dKey.isPressed) x += 1f;
                moveInput = new Vector2(x, z).normalized;
            }

            // Calculate movement direction relative to player rotation
            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
            characterController.Move(move * walkSpeed * Time.deltaTime);

            // Apply gravity
            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
        }

        private void HandleInteraction()
        {
            if (playerCamera == null) return;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;

            bool lookingAtGate = false;
            GateInteraction detectedGate = null;

            if (Physics.Raycast(ray, out hit, interactionDistance))
            {
                if (hit.collider.CompareTag("Gate"))
                {
                    lookingAtGate = true;
                    // Try to get GateInteraction from hit object or its parents
                    detectedGate = hit.collider.GetComponentInParent<GateInteraction>();
                }
            }

            // Update Interact Hint UI visibility
            if (interactHintUI != null)
            {
                interactHintUI.SetActive(lookingAtGate);
            }

            // Handle press E interaction
            if (lookingAtGate && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                // Trigger animator on hands if assigned
                if (handAnimator != null)
                {
                    handAnimator.PlayInteract();
                }

                // Call GateInteraction.TriggerOpen()
                if (detectedGate != null)
                {
                    detectedGate.TriggerOpen();
                }
                else if (gateInteraction != null)
                {
                    gateInteraction.TriggerOpen();
                }
                else
                {
                    Debug.LogWarning("PlayerController: Looked at Gate and pressed E, but no GateInteraction component was found or assigned!");
                }
            }
        }
    }
}
