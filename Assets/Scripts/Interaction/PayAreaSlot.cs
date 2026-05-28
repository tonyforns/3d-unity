using UnityEngine;

namespace ThreeDUnity.Interaction
{
    /// <summary>
    /// Posición fija en una <see cref="PayArea"/> para colocar un <see cref="InteractableShopItem"/> al cobrar.
    /// </summary>
    public class PayAreaSlot : MonoBehaviour
    {
        private InteractableShopItem occupant;

        public bool IsOccupied => occupant != null;

        public InteractableShopItem Occupant => occupant;

        public void SetOccupant(InteractableShopItem item)
        {
            occupant = item;
        }

        public void ClearOccupant()
        {
            occupant = null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = IsOccupied ? new Color(1f, 0.6f, 0.2f, 0.9f) : new Color(0.3f, 0.7f, 1f, 0.9f);
            Gizmos.DrawWireCube(transform.position, new Vector3(0.22f, 0.15f, 0.22f));
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.12f, name);
        }
#endif
    }
}
