using System.Globalization;
using ThreeDUnity.Audio;
using ThreeDUnity.Economy;
using UnityEngine;
using UnityEngine.Events;

namespace ThreeDUnity.Interaction
{
    /// <summary>
    /// Cartel "On Sale": cobra al jugador, desaparece y habilita la tienda
    /// (puerta, estanterías y pay area para clientes NPC).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(InteractablePhysicsLayer))]
    public class InteractableShopOnSale : MonoBehaviour, IInteractable
    {
        [Header("Compra")]
        [SerializeField, Min(0f)] private float purchasePrice = 500f;

        [Header("Contenido bloqueado hasta comprar")]
        [Tooltip("Collider de la puerta interactuable. Opcional; solo se habilita si está asignado.")]
        [SerializeField] private Collider doorCollider;

        [Tooltip("Estanterías que se activan al comprar. Los NPC solo buscan objetos activos en escena.")]
        [SerializeField] private ShopShelf[] shelvesToActivate;

        [Tooltip("Pay area que se activa al comprar para que los NPC depositen productos.")]
        [SerializeField] private PayArea payAreaToActivate;

        [Tooltip("Si está activo, desactiva puerta, estanterías y pay area al iniciar la escena.")]
        [SerializeField] private bool lockShopUntilPurchased = true;

        [Header("Texto UI")]
        [SerializeField] private string promptBuy = "Comprar tienda ({0})";
        [SerializeField] private string currencySymbol = "$";
        [SerializeField] private bool allowDecimals;

        [Header("Al comprar")]
        [SerializeField] private bool destroyOnPurchase = true;
        [SerializeField] private UnityEvent onPurchased;

        private bool purchased;

        public string InteractionPrompt
        {
            get
            {
                if (purchased)
                {
                    return string.Empty;
                }

                return string.Format(
                    CultureInfo.InvariantCulture,
                    promptBuy,
                    FormatMoney(purchasePrice));
            }
        }

        private void Awake()
        {
            if (!lockShopUntilPurchased)
            {
                return;
            }

            LockShopContent();
        }

        public bool CanInteract(PlayerController interactor)
        {
            return !purchased && interactor != null;
        }

        public void Interact(PlayerController interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            PlayerEconomy economy = PlayerEconomy.Instance;
            if (economy == null || !economy.TrySpend(purchasePrice))
            {
                PlayFailSound();
                return;
            }

            purchased = true;
            UnlockShopContent();
            onPurchased?.Invoke();
            PlayPurchaseSound();

            if (destroyOnPurchase)
            {
                Destroy(gameObject);
                return;
            }

            gameObject.SetActive(false);
        }

        private void LockShopContent()
        {
            if (doorCollider != null)
            {
                doorCollider.enabled = false;
            }

            SetShelvesActive(false);
            SetPayAreaActive(false);
        }

        private void UnlockShopContent()
        {
            if (doorCollider != null)
            {
                doorCollider.enabled = true;
            }

            SetShelvesActive(true);
            SetPayAreaActive(true);
        }

        private void SetShelvesActive(bool active)
        {
            if (shelvesToActivate == null)
            {
                return;
            }

            foreach (ShopShelf shelf in shelvesToActivate)
            {
                if (shelf == null)
                {
                    continue;
                }

                shelf.gameObject.SetActive(active);
            }
        }

        private void SetPayAreaActive(bool active)
        {
            if (payAreaToActivate == null)
            {
                return;
            }

            payAreaToActivate.gameObject.SetActive(active);
        }

        private string FormatMoney(float amount)
        {
            if (!allowDecimals)
            {
                return $"{currencySymbol}{amount.ToString("0", CultureInfo.InvariantCulture)}";
            }

            return $"{currencySymbol}{amount.ToString("F2", CultureInfo.InvariantCulture)}";
        }

        private void PlayPurchaseSound()
        {
            AudioManager.Instance?.PlayClip(AudioClipId.CoinsGather, transform.position);
        }

        private void PlayFailSound()
        {
            AudioManager.Instance?.PlayClip(AudioClipId.Fail, transform.position);
        }

        private void OnValidate()
        {
            if (purchasePrice < 0f)
            {
                purchasePrice = 0f;
            }
        }
    }
}
