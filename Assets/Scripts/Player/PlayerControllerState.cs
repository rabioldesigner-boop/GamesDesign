using UnityEngine;
using StarterAssets;

namespace TombOfServilii
{
    public class PlayerControllerState : MonoBehaviour
    {
        /// <summary>
        /// Enables or disables player movement/camera controls and handles locking/unlocking the cursor.
        /// </summary>
        public static void SetPlayerControlActive(GameObject playerRoot, bool active)
        {
            if (playerRoot == null)
            {
                Debug.LogWarning("PlayerControllerState: Player Root is null!");
                return;
            }

            // 1. Toggle FirstPersonController movement/look update loops
            FirstPersonController fpc = playerRoot.GetComponent<FirstPersonController>();
            if (fpc != null)
            {
                fpc.enabled = active;
            }

            // 2. Configure StarterAssetsInputs inputs and cursor state
            StarterAssetsInputs inputs = playerRoot.GetComponent<StarterAssetsInputs>();
            if (inputs != null)
            {
                inputs.move = Vector2.zero;
                inputs.look = Vector2.zero;
                inputs.cursorLocked = active;
                inputs.cursorInputForLook = active;
            }

            // 3. Force actual Unity Cursor LockState and visibility
            Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !active;
        }
    }
}
