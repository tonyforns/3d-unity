using ThreeDUnity.Audio;
using ThreeDUnity.Items;
using UnityEngine;
using UnityEngine.Events;

namespace ThreeDUnity.Interaction
{
    /// <summary>
    /// Zona de cobro con posiciones fijas para <see cref="InteractableShopItem"/> y cálculo del total de venta.
    /// Los ítems los coloca un sistema externo (p. ej. inventario de NPC) con <see cref="TryPlaceItem"/> o <see cref="TryAddItem"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class PayArea : MonoBehaviour
    {
        [Header("Slots")]
        [SerializeField] private PayAreaSlot[] slots;

        [Header("Spawn")]
        [Tooltip("Prefab con InteractableShopItem. Lo usará el NPC al vaciar su inventario en el mostrador.")]
        [SerializeField] private GameObject shopItemPrefab;

        [Header("Navegación NPC")]
        [Tooltip("Punto donde el cliente debe pararse para dejar los productos en esta pay area.")]
        [SerializeField] private Transform customerActionPoint;

        [Header("Eventos")]
        [SerializeField] private UnityEvent<InteractableShopItem> onItemAdded;
        [SerializeField] private UnityEvent onItemsCleared;

        public int SlotCount => slots != null ? slots.Length : 0;
        public int OccupiedCount { get; private set; }
        public bool HasItems => OccupiedCount > 0;
        public bool HasFreeSlot => GetFirstFreeSlot() != null;
        public Transform CustomerActionPoint => customerActionPoint;

        public float TotalAmount
        {
            get
            {
                float total = 0f;
                if (slots == null)
                {
                    return total;
                }

                foreach (PayAreaSlot slot in slots)
                {
                    if (slot == null || !slot.IsOccupied || slot.Occupant.Data == null)
                    {
                        continue;
                    }

                    total += slot.Occupant.Data.SellPrice;
                }

                return total;
            }
        }

        private void Reset()
        {
            slots = GetComponentsInChildren<PayAreaSlot>();
            if (customerActionPoint == null)
            {
                Transform actionPoint = transform.Find("CustomerActionPoint");
                if (actionPoint != null)
                {
                    customerActionPoint = actionPoint;
                }
            }
        }

        /// <summary>
        /// Instancia un ShopItem en el siguiente slot libre. Punto de entrada previsto para el NPC y su inventario.
        /// </summary>
        public bool TryPlaceItem(ItemData itemData)
        {
            if (itemData == null || shopItemPrefab == null || !HasFreeSlot)
            {
                return false;
            }

            PayAreaSlot slot = GetFirstFreeSlot();
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

            shopItem.Configure(itemData);
            return TryAddItem(shopItem);
        }

        public bool CanAcceptItem(InteractableShopItem item)
        {
            return item != null && item.Data != null && !item.IsAtPayArea && HasFreeSlot;
        }

        /// <summary>Registra un ShopItem ya existente en el mostrador (p. ej. si el NPC lo trae instanciado).</summary>
        public bool TryAddItem(InteractableShopItem item)
        {
            if (!CanAcceptItem(item))
            {
                return false;
            }

            PayAreaSlot slot = GetFirstFreeSlot();
            if (slot == null)
            {
                return false;
            }

            item.AssignToPayArea(slot);
            slot.SetOccupant(item);
            OccupiedCount++;
            PlayPlacementSound(slot.transform.position);
            onItemAdded?.Invoke(item);
            return true;
        }

        public void ClearItems()
        {
            if (slots == null)
            {
                return;
            }

            foreach (PayAreaSlot slot in slots)
            {
                if (slot == null || !slot.IsOccupied)
                {
                    continue;
                }

                InteractableShopItem item = slot.Occupant;
                slot.ClearOccupant();
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            RecountOccupiedSlots();
            onItemsCleared?.Invoke();
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

            foreach (PayAreaSlot slot in slots)
            {
                if (slot != null && slot.IsOccupied)
                {
                    OccupiedCount++;
                }
            }
        }

        private static void PlayPlacementSound(Vector3 position)
        {
            AudioManager.Instance?.PlayClip(AudioClipId.CardboardHit, position);
        }

        private PayAreaSlot GetFirstFreeSlot()
        {
            if (slots == null)
            {
                return null;
            }

            foreach (PayAreaSlot slot in slots)
            {
                if (slot != null && !slot.IsOccupied)
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
                slots = GetComponentsInChildren<PayAreaSlot>();
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
