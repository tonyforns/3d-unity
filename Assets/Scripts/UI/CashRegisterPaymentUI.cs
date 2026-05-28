using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ThreeDUnity.UI
{
    /// <summary>
    /// Panel de cobro con campo numérico (prefijo $ visual) y envío con Enter.
    /// Asigna el <see cref="TMP_InputField"/> y el objeto raíz del panel en el inspector.
    /// </summary>
    public class CashRegisterPaymentUI : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        [Header("Referencias")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_InputField amountInput;
        [SerializeField] private TMP_Text currencyPrefixLabel;

        [Header("Formato")]
        [SerializeField] private string currencySymbol = "$";
        [SerializeField] private bool allowDecimals = true;

        [Header("Comportamiento")]
        [SerializeField] private bool closeOnEscape = true;
        [SerializeField] private bool refocusOnDeselect = true;

        private Interaction.InteractableCashRegister activeRegister;
        private MovePlayerController activePlayerMovement;

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            if (currencyPrefixLabel != null)
            {
                currencyPrefixLabel.text = currencySymbol;
            }

            CloseImmediate();
        }

        private void OnEnable()
        {
            if (amountInput == null)
            {
                return;
            }

            amountInput.onSubmit.AddListener(OnSubmit);
            amountInput.onValueChanged.AddListener(OnInputValueChanged);
            amountInput.onDeselect.AddListener(OnInputDeselect);
        }

        private void OnDisable()
        {
            if (amountInput == null)
            {
                return;
            }

            amountInput.onSubmit.RemoveListener(OnSubmit);
            amountInput.onValueChanged.RemoveListener(OnInputValueChanged);
            amountInput.onDeselect.RemoveListener(OnInputDeselect);
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            if (closeOnEscape && UnityEngine.InputSystem.Keyboard.current != null
                && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void Open(Interaction.InteractableCashRegister register, MovePlayerController playerMovement)
        {
            if (register == null)
            {
                Debug.LogWarning($"{nameof(CashRegisterPaymentUI)}: registro nulo.", this);
                return;
            }

            if (amountInput == null)
            {
                Debug.LogError($"{nameof(CashRegisterPaymentUI)}: falta {nameof(amountInput)}.", this);
                return;
            }

            activeRegister = register;
            activePlayerMovement = playerMovement;

            IsOpen = true;
            panelRoot.SetActive(true);

            activePlayerMovement?.SetGameplayInputEnabled(false);

            UnlockCursor();

            amountInput.text = string.Empty;
            amountInput.contentType = allowDecimals
                ? TMP_InputField.ContentType.DecimalNumber
                : TMP_InputField.ContentType.IntegerNumber;

            FocusInput();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            MovePlayerController movement = activePlayerMovement;

            activeRegister = null;
            activePlayerMovement = null;
            IsOpen = false;

            if (amountInput != null)
            {
                amountInput.text = string.Empty;
            }

            CloseImmediate();

            if (movement != null)
            {
                movement.SetGameplayInputEnabled(true);
                movement.RestoreCursorFromGameplaySettings();
            }
        }

        private void OnInputValueChanged(string value)
        {
            if (!IsOpen || amountInput == null)
            {
                return;
            }

            string sanitized = SanitizeNumericInput(value);
            if (sanitized == value)
            {
                return;
            }

            amountInput.SetTextWithoutNotify(sanitized);
        }

        private void CloseImmediate()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void OnSubmit(string _)
        {
            TrySubmitCurrentAmount();
        }

        private void OnInputDeselect(string _)
        {
            if (!IsOpen || !refocusOnDeselect)
            {
                return;
            }

            FocusInput();
        }

        private void TrySubmitCurrentAmount()
        {
            if (!IsOpen || activeRegister == null || amountInput == null)
            {
                return;
            }

            string raw = amountInput.text ?? string.Empty;
            if (!TryParseAmount(raw, out float amount))
            {
                Debug.LogWarning(
                    $"[{nameof(CashRegisterPaymentUI)}] Importe no válido: '{raw}'.",
                    this);
                FocusInput();
                return;
            }

            Interaction.InteractableCashRegister register = activeRegister;
            Close();
            register.NotifyPaymentSubmitted(amount);
        }

        private bool TryParseAmount(string raw, out float amount)
        {
            amount = 0f;
            string sanitized = SanitizeNumericInput(raw);

            if (string.IsNullOrEmpty(sanitized))
            {
                return false;
            }

            return float.TryParse(
                sanitized,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out amount);
        }

        private string SanitizeNumericInput(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            value = value.Trim().TrimStart('$');

            var buffer = new System.Text.StringBuilder(value.Length);
            bool hasDecimalSeparator = false;

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsDigit(c))
                {
                    buffer.Append(c);
                    continue;
                }

                if (!allowDecimals)
                {
                    continue;
                }

                if ((c == '.' || c == ',') && !hasDecimalSeparator)
                {
                    hasDecimalSeparator = true;
                    buffer.Append('.');
                }
            }

            return buffer.ToString();
        }

        private void FocusInput()
        {
            if (amountInput == null || EventSystem.current == null)
            {
                return;
            }

            amountInput.Select();
            amountInput.ActivateInputField();
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
