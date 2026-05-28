using System.Globalization;
using ThreeDUnity.Economy;
using TMPro;
using UnityEngine;

namespace ThreeDUnity.UI
{
    /// <summary>
    /// Muestra el saldo actual del jugador en un <see cref="TMP_Text"/>.
    /// </summary>
    public class PlayerMoneyUI : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private TMP_Text balanceLabel;
        [SerializeField] private PlayerEconomy economy;

        [Header("Formato")]
        [SerializeField] private string currencySymbol = "$";
        [SerializeField] private bool allowDecimals;

        private void Awake()
        {
            if (balanceLabel == null)
            {
                balanceLabel = GetComponentInChildren<TMP_Text>(true);
            }

            if (economy == null)
            {
                economy = PlayerEconomy.Instance;
            }

            if (economy == null)
            {
                economy = FindFirstObjectByType<PlayerEconomy>();
            }
        }

        private void OnEnable()
        {
            if (economy == null)
            {
                return;
            }

            economy.AddBalanceChangedListener(HandleBalanceChanged);
            HandleBalanceChanged(economy.Balance);
        }

        private void OnDisable()
        {
            if (economy == null)
            {
                return;
            }

            economy.RemoveBalanceChangedListener(HandleBalanceChanged);
        }

        private void HandleBalanceChanged(float balance)
        {
            if (balanceLabel == null)
            {
                return;
            }

            balanceLabel.text = FormatMoney(balance);
        }

        private string FormatMoney(float amount)
        {
            if (!allowDecimals || Mathf.Approximately(amount % 1f, 0f))
            {
                return $"{currencySymbol}{amount.ToString("0", CultureInfo.InvariantCulture)}";
            }

            return $"{currencySymbol}{amount.ToString("F2", CultureInfo.InvariantCulture)}";
        }
    }
}
