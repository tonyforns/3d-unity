using ThreeDUnity.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThreeDUnity.UI
{
    /// <summary>
    /// Muestra en pantalla el <see cref="IInteractable.InteractionPrompt"/> del objeto que mira el jugador.
    /// Colócalo en el Canvas del HUD y asigna el <see cref="TMP_Text"/> del mensaje.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private TMP_Text promptLabel;
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private PlayerInteractionController interactionController;

        [Header("Formato")]
        [SerializeField] private bool showInteractKey = true;
        [SerializeField] private string interactKeyFormat = "[{0}] {1}";

        [Header("Visibilidad")]
        [SerializeField] private bool hideWhenPaymentUiOpen = true;
        [Tooltip("Si está activo, solo muestra el mensaje cuando CanInteract devuelve true.")]
        [SerializeField] private bool onlyWhenCanInteract = true;

        private void Awake()
        {
            if (promptLabel == null)
            {
                promptLabel = GetComponentInChildren<TMP_Text>(true);
            }

            if (promptRoot == null && promptLabel != null)
            {
                promptRoot = promptLabel.gameObject;
            }

            if (interactionController == null)
            {
                interactionController = FindFirstObjectByType<PlayerInteractionController>();
            }
        }

        private void LateUpdate()
        {
            RefreshPrompt();
        }

        private void OnDisable()
        {
            HidePrompt();
        }

        private void RefreshPrompt()
        {
            if (ShouldHide())
            {
                HidePrompt();
                return;
            }

            IInteractable target = interactionController.CurrentTarget;
            if (target == null)
            {
                HidePrompt();
                return;
            }

            PlayerController interactor = interactionController.Interactor;
            if (onlyWhenCanInteract && !target.CanInteract(interactor))
            {
                HidePrompt();
                return;
            }

            string prompt = target.InteractionPrompt;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                HidePrompt();
                return;
            }

            ShowPrompt(FormatPrompt(prompt));
        }

        private bool ShouldHide()
        {
            if (interactionController == null)
            {
                return true;
            }

            return hideWhenPaymentUiOpen && CashRegisterPaymentUI.IsOpen;
        }

        private string FormatPrompt(string prompt)
        {
            if (!showInteractKey)
            {
                return prompt;
            }

            string keyLabel = GetInteractKeyLabel();
            if (string.IsNullOrEmpty(keyLabel))
            {
                return prompt;
            }

            return string.Format(interactKeyFormat, keyLabel, prompt);
        }

        private string GetInteractKeyLabel()
        {
            Key interactKey = interactionController.InteractKey;
            return interactKey.ToString();
        }

        private void ShowPrompt(string text)
        {
            if (promptLabel != null)
            {
                promptLabel.text = text;
            }

            if (promptRoot != null && !promptRoot.activeSelf)
            {
                promptRoot.SetActive(true);
            }
        }

        private void HidePrompt()
        {
            if (promptRoot != null && promptRoot.activeSelf)
            {
                promptRoot.SetActive(false);
            }
        }
    }
}
