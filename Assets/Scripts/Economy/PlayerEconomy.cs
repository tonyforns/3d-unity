using UnityEngine;
using UnityEngine.Events;

namespace ThreeDUnity.Economy
{
    /// <summary>
    /// Saldo del jugador: resta al reponer estanterías y suma en ventas exitosas en caja.
    /// </summary>
    public class PlayerEconomy : MonoBehaviour
    {
        public static PlayerEconomy Instance { get; private set; }

        [Header("Saldo inicial")]
        [SerializeField, Min(0f)] private float startingBalance = 100f;

        [Header("Eventos")]
        [SerializeField] private UnityEvent<float> onBalanceChanged;

        public float Balance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"[{nameof(PlayerEconomy)}] Ya existe una instancia en escena; se destruye el duplicado.",
                    this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Balance = startingBalance;
            NotifyBalanceChanged();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool CanAfford(float amount)
        {
            return amount >= 0f && Balance >= amount;
        }

        public bool TrySpend(float amount)
        {
            if (amount < 0f || Balance < amount)
            {
                return false;
            }

            SetBalance(Balance - amount);
            return true;
        }

        public void AddMoney(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            SetBalance(Balance + amount);
        }

        public void AddBalanceChangedListener(UnityAction<float> listener)
        {
            if (listener != null)
            {
                onBalanceChanged.AddListener(listener);
            }
        }

        public void RemoveBalanceChangedListener(UnityAction<float> listener)
        {
            if (listener != null)
            {
                onBalanceChanged.RemoveListener(listener);
            }
        }

        private void SetBalance(float newBalance)
        {
            Balance = Mathf.Max(0f, newBalance);
            NotifyBalanceChanged();
        }

        private void NotifyBalanceChanged()
        {
            onBalanceChanged?.Invoke(Balance);
        }
    }
}
