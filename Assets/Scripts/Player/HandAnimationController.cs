using UnityEngine;

namespace TombOfServilii
{
    [RequireComponent(typeof(Animator))]
    public class HandAnimationController : MonoBehaviour
    {
        [Tooltip("The visual root GameObject of the hands (to enable/disable visibility without disabling this script).")]
        [SerializeField] private GameObject handVisualRoot;

        private Animator animator;
        private CharacterController characterController;

        private void Start()
        {
            animator = GetComponent<Animator>();
            characterController = GetComponentInParent<CharacterController>();

            if (characterController == null)
            {
                Debug.LogWarning("HandAnimationController: No CharacterController found in parent hierarchy!");
            }

            if (handVisualRoot == null)
            {
                // Fallback to this GameObject if no specific visual root is assigned
                handVisualRoot = gameObject;
            }
        }

        private void Update()
        {
            if (characterController == null) return;

            // Get horizontal velocity (ignore gravity/vertical movement for animation speed)
            Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
            float moveSpeed = horizontalVelocity.magnitude;

            // Update animator parameters
            animator.SetBool("isWalking", moveSpeed > 0.1f);
            animator.SetFloat("speed", moveSpeed);
        }

        public void PlayInteract()
        {
            if (animator != null)
            {
                animator.SetTrigger("isInteracting");
            }
        }

        public void ShowHands()
        {
            if (handVisualRoot != null)
            {
                handVisualRoot.SetActive(true);
            }
        }

        public void HideHands()
        {
            if (handVisualRoot != null)
            {
                handVisualRoot.SetActive(false);
            }
        }
    }
}
