using ThreeDUnity.AI;
using ThreeDUnity.Items;
using UnityEngine;
using UnityEngine.Events;

namespace ThreeDUnity.Interaction
{
    /// <summary>
    /// Estantería con un único <see cref="ItemData"/> permitido y posiciones de colocación fijas.
    /// </summary>
    public class ShopShelf : MonoBehaviour
    {
        [Header("Producto")]
        [SerializeField] private ItemData allowedItem;

        [Header("Spawn")]
        [SerializeField] private GameObject shopItemPrefab;
        [SerializeField] private ShopShelfSlot[] slots;

        [Header("Navegación NPC")]
        [Tooltip("Punto donde el cliente debe pararse para recoger productos de esta estantería.")]
        [SerializeField] private Transform customerActionPoint;

        [Header("Eventos")]
        [SerializeField] private UnityEvent<InteractableShopItem> onItemPlaced;
        [SerializeField] private UnityEvent onShelfFull;

        public ItemData AllowedItem => allowedItem;
        public int SlotCount => slots != null ? slots.Length : 0;
        public int OccupiedCount { get; private set; }
        public Transform CustomerActionPoint => customerActionPoint;

        public bool HasFreeSlot => GetFirstFreeSlot() != null;

        public bool CanPlace => allowedItem != null && shopItemPrefab != null && HasFreeSlot;

        public bool IsReserved => reservedBy != null;
        public ShopCustomerAgent ReservedBy => reservedBy;

        private ShopCustomerAgent reservedBy;

        private void Reset()
        {
            slots = GetComponentsInChildren<ShopShelfSlot>();
            if (customerActionPoint == null)
            {
                Transform actionPoint = transform.Find("CustomerActionPoint");
                if (actionPoint != null)
                {
                    customerActionPoint = actionPoint;
                }
            }
        }

        public bool TryPlaceItem()
        {
            if (!CanPlace)
            {
                return false;
            }

            ShopShelfSlot slot = GetFirstFreeSlot();
            if (slot == null)
            {
                return false;
            }

            GameObject instance = Instantiate(shopItemPrefab, slot.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            InteractableShopItem shopItem = instance.GetComponent<InteractableShopItem>();
            if (shopItem == null)
            {
                Debug.LogError(
                    $"{nameof(shopItemPrefab)} debe incluir {nameof(InteractableShopItem)}.",
                    this);
                Destroy(instance);
                return false;
            }

            shopItem.Configure(allowedItem, slot);
            slot.SetOccupant(shopItem);
            OccupiedCount++;

            onItemPlaced?.Invoke(shopItem);

            if (!HasFreeSlot)
            {
                onShelfFull?.Invoke();
            }

            return true;
        }

        internal void NotifyItemRemoved()
        {
            RecountOccupiedSlots();
        }

        internal void RecountOccupiedSlots()
        {
            OccupiedCount = 0;
            if (slots == null)
            {
                return;
            }

            foreach (ShopShelfSlot slot in slots)
            {
                if (slot != null && slot.IsOccupied)
                {
                    OccupiedCount++;
                }
            }
        }

        public bool IsAvailableFor(ShopCustomerAgent customer)
        {
            return reservedBy == null || reservedBy == customer;
        }

        /// <summary>
        /// Bloquea el acceso a la estantería para un cliente (hasta <see cref="Release"/>).
        /// </summary>
        public bool TryReserve(ShopCustomerAgent customer)
        {
            if (customer == null)
            {
                return false;
            }

            if (reservedBy == null || reservedBy == customer)
            {
                reservedBy = customer;
                return true;
            }

            return false;
        }

        public void Release(ShopCustomerAgent customer)
        {
            if (reservedBy == customer)
            {
                reservedBy = null;
            }
        }

        /// <summary>
        /// Retira el primer <see cref="InteractableShopItem"/> ocupado (p. ej. para un NPC).
        /// </summary>
        public bool TryTakeItem(out InteractableShopItem item)
        {
            item = null;
            ShopShelfSlot slot = GetFirstOccupiedSlot();
            if (slot == null)
            {
                return false;
            }

            item = slot.Occupant;
            slot.ClearOccupant();
            RecountOccupiedSlots();
            return item != null;
        }

        private ShopShelfSlot GetFirstFreeSlot()
        {
            if (slots == null)
            {
                return null;
            }

            foreach (ShopShelfSlot slot in slots)
            {
                if (slot != null && !slot.IsOccupied)
                {
                    return slot;
                }
            }

            return null;
        }

        private ShopShelfSlot GetFirstOccupiedSlot()
        {
            if (slots == null)
            {
                return null;
            }

            foreach (ShopShelfSlot slot in slots)
            {
                if (slot != null && slot.IsOccupied)
                {
                    return slot;
                }
            }

            return null;
        }

        private void OnValidate()
        {
            if (slots == null || slots.Length == 0)
            {
                slots = GetComponentsInChildren<ShopShelfSlot>();
            }

            if (customerActionPoint == null)
            {
                Transform actionPoint = transform.Find("CustomerActionPoint");
                if (actionPoint != null)
                {
                    customerActionPoint = actionPoint;
                }
            }

            RecountOccupiedSlots();
        }
    }
}
