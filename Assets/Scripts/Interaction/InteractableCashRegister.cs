using ThreeDUnity.Audio;
using ThreeDUnity.Economy;
using ThreeDUnity.UI;
using UnityEngine;
using UnityEngine.Events;

namespace ThreeDUnity.Interaction
{
    /// <summary>
    /// Caja registradora interactuable: abre el panel de cobro y emite el importe al confirmar con Enter.
    /// </summary>
    [RequireComponent(typeof(InteractablePhysicsLayer))]
    public class InteractableCashRegister : MonoBehaviour, IInteractable
    {
        [Header("UI")]
        [SerializeField] private CashRegisterPaymentUI paymentUI;

        [Header("Zona de cobro")]
        [SerializeField] private PayArea payArea;
        [SerializeField] private bool requireItemsInPayArea = true;
        [SerializeField, Min(0f)] private float paymentTolerance = 0.01f;
        [SerializeField] private bool clearPayAreaOnSuccess = true;

        [Header("Interacción")]
        [SerializeField] private string interactionPrompt = "Usar caja registradora";
        [SerializeField] private string interactionPromptEmptyPayArea = "Sin productos en mostrador";

        [Header("Eventos")]
        [SerializeField] private UnityEvent<float> onPaymentSubmitted;
        [SerializeField] private UnityEvent onPaymentSuccess;
        [SerializeField] private UnityEvent onPaymentFailure;

        public string InteractionPrompt =>
            HasRequiredPayAreaItems() ? interactionPrompt : interactionPromptEmptyPayArea;

        public PayArea PayArea => payArea;

        public void AddPaymentSuccessListener(UnityAction listener)
        {
            if (listener != null)
            {
                onPaymentSuccess.AddListener(listener);
            }
        }

        public void RemovePaymentSuccessListener(UnityAction listener)
        {
            if (listener != null)
            {
                onPaymentSuccess.RemoveListener(listener);
            }
        }

        public void AddPaymentFailureListener(UnityAction listener)
        {
            if (listener != null)
            {
                onPaymentFailure.AddListener(listener);
            }
        }

        public void RemovePaymentFailureListener(UnityAction listener)
        {
            if (listener != null)
            {
                onPaymentFailure.RemoveListener(listener);
            }
        }

        public bool CanInteract(PlayerController interactor)
        {
            if (interactor == null || paymentUI == null)
            {
                return false;
            }

            if (CashRegisterPaymentUI.IsOpen)
            {
                return false;
            }

            if (requireItemsInPayArea && payArea != null && !payArea.HasItems)
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

            MovePlayerController movement = interactor.GetComponent<MovePlayerController>();
            paymentUI.Open(this, movement);
        }

        /// <summary>Llamado por <see cref="CashRegisterPaymentUI"/> tras confirmar el importe con Enter.</summary>
        public void NotifyPaymentSubmitted(float amount)
        {
            if (payArea != null)
            {
                float expected = payArea.TotalAmount;

                if (requireItemsInPayArea && !payArea.HasItems)
                {
                    PlayPaymentFailSound();
                    onPaymentFailure?.Invoke();
                    return;
                }

                if (Mathf.Abs(amount - expected) <= paymentTolerance)
                {
                    float saleTotal = expected;
                    PlayerEconomy economy = PlayerEconomy.Instance;
                    if (economy != null && saleTotal > 0f)
                    {
                        economy.AddMoney(saleTotal);
                    }

                    AudioManager.Instance?.PlayClip(AudioClipId.CoinsGather, transform.position);
                    onPaymentSuccess?.Invoke();

                    if (clearPayAreaOnSuccess)
                    {
                        payArea.ClearItems();
                    }
                }
                else
                {
                    PlayPaymentFailSound();
                    onPaymentFailure?.Invoke();
                }
            }

            onPaymentSubmitted?.Invoke(amount);
        }

        private void PlayPaymentFailSound()
        {
            AudioManager.Instance?.PlayClip(AudioClipId.Fail, transform.position);
        }

        private bool HasRequiredPayAreaItems()
        {
            if (!requireItemsInPayArea || payArea == null)
            {
                return true;
            }

            return payArea.HasItems;
        }

        private void OnValidate()
        {
            if (paymentUI == null)
            {
                paymentUI = FindFirstObjectByType<CashRegisterPaymentUI>();
            }
        }
    }
}
