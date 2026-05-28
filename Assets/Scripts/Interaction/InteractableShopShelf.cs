using ThreeDUnity.Economy;
using ThreeDUnity.Items;
using UnityEngine;

namespace ThreeDUnity.Interaction
{
    /// <summary>
    /// Permite al jugador colocar un <see cref="InteractableShopItem"/> en la estantería asociada.
    /// </summary>
    [RequireComponent(typeof(ShopShelf))]
    [RequireComponent(typeof(InteractablePhysicsLayer))]
    public class InteractableShopShelf : MonoBehaviour, IInteractable
    {
        [SerializeField] private ShopShelf shelf;
        [SerializeField] private string promptPlace = "Colocar producto";
        [SerializeField] private string promptFull = "Estantería llena";
        [SerializeField] private string promptUnconfigured = "Estantería sin configurar";
        [SerializeField] private string promptInsufficientFunds = "Dinero insuficiente";

        public string InteractionPrompt
        {
            get
            {
                if (shelf == null)
                {
                    return promptUnconfigured;
                }

                if (!shelf.CanPlace)
                {
                    return shelf.HasFreeSlot ? promptUnconfigured : promptFull;
                }

                if (!CanAffordRestock())
                {
                    return promptInsufficientFunds;
                }

                return promptPlace;
            }
        }

        private void Awake()
        {
            if (shelf == null)
            {
                shelf = GetComponent<ShopShelf>();
            }
        }

        public bool CanInteract(PlayerController interactor)
        {
            return interactor != null && shelf != null && shelf.CanPlace && CanAffordRestock();
        }

        public void Interact(PlayerController interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            ItemData item = shelf.AllowedItem;
            float cost = item != null ? item.BuyPrice : 0f;
            PlayerEconomy economy = PlayerEconomy.Instance;

            if (economy != null && cost > 0f && !economy.TrySpend(cost))
            {
                return;
            }

            if (!shelf.TryPlaceItem())
            {
                if (economy != null && cost > 0f)
                {
                    economy.AddMoney(cost);
                }

                return;
            }
        }

        private bool CanAffordRestock()
        {
            if (shelf == null || shelf.AllowedItem == null)
            {
                return true;
            }

            float cost = shelf.AllowedItem.BuyPrice;
            if (cost <= 0f)
            {
                return true;
            }

            PlayerEconomy economy = PlayerEconomy.Instance;
            return economy == null || economy.CanAfford(cost);
        }

        private void OnValidate()
        {
            if (shelf == null)
            {
                shelf = GetComponent<ShopShelf>();
            }
        }
    }
}
