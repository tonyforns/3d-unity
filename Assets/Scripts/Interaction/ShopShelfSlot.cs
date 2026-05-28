using UnityEngine;

namespace ThreeDUnity.Interaction
{
    /// <summary>
    /// Posición fija en una <see cref="ShopShelf"/>. Solo puede albergar un <see cref="InteractableShopItem"/> a la vez.
    /// </summary>
    public class ShopShelfSlot : MonoBehaviour
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
            Gizmos.color = IsOccupied ? new Color(1f, 0.35f, 0.35f, 0.9f) : new Color(0.35f, 1f, 0.5f, 0.9f);
            Gizmos.DrawWireCube(transform.position, new Vector3(0.2f, 0.2f, 0.2f));
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.15f, name);
        }
#endif
    }
}
