using UnityEngine;

namespace TombOfServilii
{
    [RequireComponent(typeof(Collider))]
    public class InfoTriggerZone : MonoBehaviour
    {
        [Tooltip("Reference to the Info Menu Manager that controls the UI.")]
        [SerializeField] private InfoMenuManager infoMenuManager;

        private Collider zoneCollider;

        private void Start()
        {
            zoneCollider = GetComponent<Collider>();
            // Force isTrigger to be true
            if (zoneCollider != null)
            {
                zoneCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // 1. Tell InfoMenuManager about the player reference and open panel
                if (infoMenuManager != null)
                {
                    infoMenuManager.ShowMenu(other.gameObject);
                }

                // 2. Transition game state to ViewingInfo
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetState(GameManager.GameState.ViewingInfo);
                }
                else
                {
                    // Fallback to manual locking if GameManager is not present
                    PlayerControllerState.SetPlayerControlActive(other.gameObject, false);
                }

                // 3. Disable this collider so it only triggers once
                if (zoneCollider != null)
                {
                    zoneCollider.enabled = false;
                }
            }
        }
    }
}
