using UnityEngine;
using TMPro;

namespace LostAndFound.Systems
{
    /// <summary>
    /// Единый менеджер денег для всех систем.
    /// - Синглтон
    /// - Начальный баланс задается в Инспекторе
    /// - Событие OnBalanceChanged для UI
    /// </summary>
    public class MoneyManager : MonoBehaviour
    {
        public static MoneyManager Instance { get; private set; }

        [Header("Стартовый капитал")]
        public int startingBalance = 10000;

        [Header("Текущее состояние")]
        [SerializeField] private int currentBalance;
        public System.Action<int> OnBalanceChanged;

        [Header("UI (назначь в инспекторе)")]
        public GameObject moneyPanel;      // Панелька для денег (можно оставить пустым)
        public TMP_Text moneyText;         // Текст, куда писать баланс
        public string uiFormat = "{0}₽";   // Формат отображения

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            currentBalance = startingBalance;
            UpdateUI();
        }

        public int GetBalance() => currentBalance;

        public bool CanAfford(int amount) => amount >= 0 && currentBalance >= amount;

        public bool Spend(int amount)
        {
            if (!CanAfford(amount)) return false;
            currentBalance -= amount;
            OnBalanceChanged?.Invoke(currentBalance);
            UpdateUI();
            return true;
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            currentBalance += amount;
            OnBalanceChanged?.Invoke(currentBalance);
            UpdateUI();
        }

        public void SetStartingBalance(int amount)
        {
            startingBalance = Mathf.Max(0, amount);
            currentBalance = startingBalance;
            OnBalanceChanged?.Invoke(currentBalance);
            UpdateUI();
        }

        public void UpdateUI()
        {
            if (moneyText != null)
            {
                moneyText.text = string.Format(uiFormat, currentBalance);
            }
            if (moneyPanel != null && !moneyPanel.activeSelf)
            {
                moneyPanel.SetActive(true);
            }
        }

        void OnValidate()
        {
            if (!Application.isPlaying)
            {
                // Обновляем предпросмотр в редакторе
                if (moneyText != null)
                {
                    moneyText.text = string.Format(uiFormat, startingBalance);
                }
            }
        }
    }
}

