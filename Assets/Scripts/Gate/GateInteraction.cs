using UnityEngine;

namespace TombOfServilii
{
    public class GateInteraction : MonoBehaviour
    {
        public virtual void TriggerOpen()
        {
            Debug.Log("GateInteraction: TriggerOpen called!");
        }
    }
}
