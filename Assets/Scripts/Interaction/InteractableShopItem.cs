using ThreeDUnity.Items;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace ThreeDUnity.Interaction
{
    /// <summary>
    /// Ítem de tienda interactuable: muestra el precio de venta en un canvas 3D al apuntar y siempre mira al jugador.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(InteractablePhysicsLayer))]
    public class InteractableShopItem : MonoBehaviour, IInteractable, IInteractionFocusable
    {
        [Header("Datos")]
        [SerializeField] private ItemData itemData;

        [Header("Visual")]
        [Tooltip("Hijo donde se instancia ItemData.Prefab si aún no tiene hijos.")]
        [SerializeField] private Transform visualAnchor;

        [Header("UI en mundo")]
        [Tooltip("Solo este objeto (y sus hijos) rota hacia el jugador. El mesh del ítem debe ser hermano, no hijo.")]
        [SerializeField] private GameObject priceSignRoot;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private FaceTarget priceBillboard;

        [Header("Interacción")]
        [SerializeField] private string interactionPrompt = "Comprar";
        [SerializeField] private string currencySymbol = "$";

        [Header("Eventos")]
        [SerializeField] private UnityEvent<ItemData> onInteract;

        public string InteractionPrompt => IsAtPayArea ? string.Empty : interactionPrompt;

        public ItemData Data => itemData;
        public ShopShelfSlot HomeSlot => homeSlot;
        public bool IsAtPayArea => payAreaSlot != null;

        private ShopShelfSlot homeSlot;
        private PayAreaSlot payAreaSlot;
        private bool visualSpawned;

        private void Awake()
        {
            ApplyItemData();
            SetInteractionFocused(false);
            ResolveBillboardTarget();
        }

        private void Start()
        {
            SpawnVisualIfNeeded();
        }

        /// <summary>Configura datos del ítem (estantería, pay area vía NPC, etc.).</summary>
        public void Configure(ItemData data, ShopShelfSlot slot = null)
        {
            itemData = data;
            homeSlot = slot;
            ApplyItemData();
            SpawnVisualIfNeeded();
        }

        public void AssignToPayArea(PayAreaSlot slot)
        {
            if (slot == null)
            {
                return;
            }

            ReleaseFromShelf();

            payAreaSlot = slot;
            gameObject.SetActive(true);
            Transform slotTransform = slot.transform;
            transform.SetParent(slotTransform);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Oculta el ítem y lo adjunta al portador (NPC). Debe llamarse tras retirarlo del slot de estantería.
        /// </summary>
        public void PrepareForCarry(Transform carryParent)
        {
            ReleaseFromShelf();

            if (carryParent != null)
            {
                transform.SetParent(carryParent);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }

            SetInteractionFocused(false);

            if (priceSignRoot != null)
            {
                priceSignRoot.SetActive(false);
            }

            Collider itemCollider = GetComponent<Collider>();
            if (itemCollider != null)
            {
                itemCollider.enabled = false;
            }

            gameObject.SetActive(false);
        }

        private void ReleaseFromShelf()
        {
            if (homeSlot == null)
            {
                return;
            }

            homeSlot.ClearOccupant();
            homeSlot.GetComponentInParent<ShopShelf>()?.NotifyItemRemoved();
            homeSlot = null;
        }

        private void OnDestroy()
        {
            if (payAreaSlot != null)
            {
                payAreaSlot.ClearOccupant();
                payAreaSlot.GetComponentInParent<PayArea>()?.NotifyItemRemoved();
                payAreaSlot = null;
            }

            if (homeSlot == null)
            {
                return;
            }

            homeSlot.ClearOccupant();
            homeSlot.GetComponentInParent<ShopShelf>()?.NotifyItemRemoved();
            homeSlot = null;
        }

        private void SpawnVisualIfNeeded()
        {
            if (visualSpawned || itemData == null || itemData.Prefab == null)
            {
                return;
            }

            Transform anchor = ResolveVisualAnchor();
            if (anchor == null)
            {
                return;
            }

            if (anchor.childCount > 0)
            {
                return;
            }

            if (itemData.Prefab.GetComponent<InteractableShopItem>() != null)
            {
                Debug.LogWarning(
                    $"{nameof(ItemData)}.{nameof(ItemData.Prefab)} debe ser el mesh del ítem, no el prefab {nameof(InteractableShopItem)}.",
                    this);
                return;
            }

            GameObject instance = Instantiate(itemData.Prefab, anchor);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            visualSpawned = true;
        }

        public bool CanInteract(PlayerController interactor)
        {
            if (interactor == null || itemData == null || IsAtPayArea)
            {
                return false;
            }

            return true;
        }

        public void Interact(PlayerController interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            onInteract?.Invoke(itemData);
        }

        public void SetInteractionFocused(bool focused)
        {
            if (priceSignRoot != null)
            {
                priceSignRoot.SetActive(focused);
            }
        }

        private void ApplyItemData()
        {
            if (itemData == null || priceLabel == null)
            {
                return;
            }

            priceLabel.text = FormatPrice(itemData.SellPrice);
        }

        private string FormatPrice(float amount)
        {
            if (Mathf.Approximately(amount % 1f, 0f))
            {
                return $"{currencySymbol}{amount:0}";
            }

            return $"{currencySymbol}{amount:F2}";
        }

        private void ResolveBillboardTarget()
        {
            ResolvePriceSignReferences();

            if (priceBillboard == null)
            {
                return;
            }

            Transform view = null;
            PlayerInteractionController interaction = FindFirstObjectByType<PlayerInteractionController>();
            if (interaction != null && interaction.ViewTransform != null)
            {
                view = interaction.ViewTransform;
            }
            else if (Camera.main != null)
            {
                view = Camera.main.transform;
            }

            if (view != null)
            {
                priceBillboard.SetTarget(view);
            }
        }

        private Transform ResolveVisualAnchor()
        {
            if (visualAnchor != null)
            {
                return visualAnchor;
            }

            Transform visual = transform.Find("Visual");
            return visual;
        }

        private void ResolvePriceSignReferences()
        {
            if (priceSignRoot == null)
            {
                Transform signTransform = transform.Find("PriceSign");
                if (signTransform == null)
                {
                    signTransform = transform.Find("PriceCanvas");
                }

                if (signTransform != null)
                {
                    priceSignRoot = signTransform.gameObject;
                }
            }

            if (priceBillboard == null && priceSignRoot != null)
            {
                priceBillboard = priceSignRoot.GetComponent<FaceTarget>();
            }

            if (priceLabel == null && priceSignRoot != null)
            {
                priceLabel = priceSignRoot.GetComponentInChildren<TMP_Text>(true);
            }
        }

        private void OnValidate()
        {
            if (visualAnchor == null)
            {
                visualAnchor = transform.Find("Visual");
            }

            ResolvePriceSignReferences();

            if (GetComponent<FaceTarget>() != null)
            {
                Debug.LogWarning(
                    $"{nameof(InteractableShopItem)}: quita {nameof(FaceTarget)} del root. Debe estar solo en {nameof(priceSignRoot)}.",
                    this);
            }

            ApplyItemData();
        }
    }
}
