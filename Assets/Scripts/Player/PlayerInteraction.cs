using UnityEngine;
using UnityEngine.InputSystem;

namespace TombOfServilii
{
    [RequireComponent(typeof(Camera))]
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [Tooltip("Maximum distance from the camera to interact with objects.")]
        [SerializeField] private float interactionDistance = 5f;

        [Header("UI Reference")]
        [Tooltip("The UI Panel or Text indicating 'Press E to Interact'.")]
        [SerializeField] private GameObject interactHintUI;

        [Header("Hand Animator Reference")]
        [Tooltip("Optional hand animation controller to play the interaction trigger.")]
        [SerializeField] private HandAnimationController handAnimator;

        private Camera playerCamera;

        private void Start()
        {
            playerCamera = GetComponent<Camera>();

            if (interactHintUI != null)
            {
                interactHintUI.SetActive(false);
            }
        }

        private void Update()
        {
            HandleRaycast();
        }

        private void HandleRaycast()
        {
            if (playerCamera == null) return;

            // Viewport ray from the exact center of the screen
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            bool lookingAtGate = false;
            GateInteraction detectedGate = null;

            if (Physics.Raycast(ray, out hit, interactionDistance))
            {
                // Find GateInteraction on the hit object or any of its parent hierarchy
                detectedGate = hit.collider.GetComponentInParent<GateInteraction>();
                if (detectedGate != null)
                {
                    lookingAtGate = true;
                }
            }

            // Toggle the interact hint UI
            if (interactHintUI != null)
            {
                interactHintUI.SetActive(lookingAtGate);
            }

            // Check for E key press when looking at the gate
            if (lookingAtGate && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                // Play hand interaction animation if assigned
                if (handAnimator != null)
                {
                    handAnimator.PlayInteract();
                }

                // Trigger the gate action
                if (detectedGate != null)
                {
                    detectedGate.TriggerOpen();
                }
                else
                {
                    Debug.LogWarning("PlayerInteraction: Found a GameObject tagged 'Gate' but it does not have a GateInteraction script!");
                }
            }
        }
    }
}
