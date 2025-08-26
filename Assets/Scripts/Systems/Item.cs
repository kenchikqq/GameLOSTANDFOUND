using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace LostAndFound.Systems
{
    /// <summary>
    /// БАЗОВЫЙ КЛАСС ПРЕДМЕТА - основа для всех предметов в игре
    /// Паттерны и стрелочка реализованы через Image[] и Image
    /// </summary>
    public class Item : MonoBehaviour, IInteractable
    {
        [Header("Основная информация")]
        public string itemName = "Неизвестный предмет";
        
        [Header("Описание предмета")]
        [Tooltip("Многострочное описание предмета")]
        [TextArea(5, 10)]
        public string itemDescription = "Описание отсутствует";
        
        [Header("Пользовательская UI панель")]
        [Tooltip("Твоя собственная UI панель для отображения предмета")]
        public GameObject customUIPanel;
        
        [Tooltip("Текст для имени предмета в твоей панели")]
        public TextMeshProUGUI customItemNameText;
        
        [Tooltip("Текст для описания предмета в твоей панели")]
        public TextMeshProUGUI customItemDescriptionText;
        
        [Tooltip("Текст для отображения цены предмета в твоей панели")]
        public TextMeshProUGUI customItemPriceText;
        
        [Header("Визуальное отображение паттерна (UI)")]
        [Tooltip("Массив UI Image компонентов для отображения паттернов")]
        public Image[] patternDisplayImages;

        [Header("Стрелочка указатель (UI)")]
        [Tooltip("UI Image компонент, который будет служить стрелочкой")]
        public Image arrowImage;

        [Tooltip("Массив возможных цен для рандомного выбора")]
        public int[] possiblePrices = { 100, 200, 350, 500, 750, 1000, 1500, 2000 };

        // Внутренние данные
        private string selectedPattern = "";
        private int currentPrice = 100;
        private ItemType currentItemType;
        private string uniqueItemId = "";
        private bool enableDebugLogs = true;

        // Типы предметов
        public enum ItemType
        {
            Models,          // Обычные модели
            Magic,           // Магические предметы  
            Animated,        // Анимированные предметы
            Contraband,      // Контрабанда
            MagicContraband, // Магическая контрабанда
            AnimatedContraband // Анимированная контрабанда
        }

        // События
        public System.Action<Item> OnInteracted;
        public System.Action<Item> OnInspected;
        public System.Action<Item> OnPickedUp;
        public System.Action<Item> OnDropped;

        void Start()
        {
            InitializeItem();
        }

        public void InitializeItem()
        {
            // Генерируем уникальный ID если нет
            if (string.IsNullOrEmpty(uniqueItemId))
            {
                uniqueItemId = System.Guid.NewGuid().ToString();
            }

            // Определяем тип по тегу
            SetItemTypeFromTag();

            // Рандомизируем цену
            RandomizePrice();

            // Обновляем визуальный паттерн и позиционируем стрелочку
            UpdatePatternDisplay();
            PositionArrowRandomly();

            // Обновляем UI
            UpdateCustomUI();

            if (enableDebugLogs)
                Debug.Log($"[Item] {itemName} инициализирован с паттерном/состоянием: {GetPatternName()}");
        }

        void SetItemTypeFromTag()
        {
            string itemTag = gameObject.tag.ToLower();
            switch (itemTag)
            {
                case "contraband":
                    currentItemType = ItemType.Contraband;
                    break;
                case "magiccontraband":
                    currentItemType = ItemType.MagicContraband;
                    break;
                case "animatedcontraband":
                    currentItemType = ItemType.AnimatedContraband;
                    break;
                case "models":
                    currentItemType = ItemType.Models;
                    break;
                case "magic":
                    currentItemType = ItemType.Magic;
                    break;
                case "animated":
                    currentItemType = ItemType.Animated;
                    break;
                default:
                    currentItemType = ItemType.Models; // По умолчанию модели
                    break;
            }
        }

        // === НОВЫЕ МЕТОДЫ ДЛЯ UI IMAGE КОМПОНЕНТОВ ===

        /// <summary>
        /// Обновляет визуальное отображение паттернов через массив Image компонентов
        /// </summary>
        void UpdatePatternDisplay()
        {
            if (patternDisplayImages != null && patternDisplayImages.Length > 0)
            {
                // Активируем все Image компоненты в массиве
                foreach (Image patternImage in patternDisplayImages)
                {
                    if (patternImage != null)
                    {
                        patternImage.gameObject.SetActive(true);
                    }
                }
                
                if (enableDebugLogs)
                    Debug.Log($"[Item] Pattern display updated for {itemName}, активировано {patternDisplayImages.Length} паттернов");
            }
        }

        /// <summary>
        /// Рандомно позиционирует стрелочку на одном из паттернов
        /// </summary>
        void PositionArrowRandomly()
        {
            if (arrowImage != null && patternDisplayImages != null && patternDisplayImages.Length > 0)
            {
                // Выбираем случайный паттерн из массива
                Image selectedPattern = null;
                for (int i = 0; i < patternDisplayImages.Length; i++)
                {
                    if (patternDisplayImages[i] != null)
                    {
                        selectedPattern = patternDisplayImages[i];
                        break; // Берем первый доступный паттерн
                    }
                }

                if (selectedPattern != null)
                {
                    RectTransform patternRect = selectedPattern.rectTransform;
                    RectTransform arrowRect = arrowImage.rectTransform;

                    // Активируем стрелочку
                    arrowImage.gameObject.SetActive(true);

                    // Получаем границы паттерна
                    float minX = patternRect.rect.xMin;
                    float maxX = patternRect.rect.xMax;
                    float minY = patternRect.rect.yMin;
                    float maxY = patternRect.rect.yMax;

                    // Рандомная позиция внутри паттерна
                    float randomX = UnityEngine.Random.Range(minX, maxX);
                    float randomY = UnityEngine.Random.Range(minY, maxY);

                    // Устанавливаем позицию стрелочки
                    arrowRect.anchoredPosition = new Vector2(randomX, randomY);
                    
                    if (enableDebugLogs)
                        Debug.Log($"[Item] Arrow for {itemName} positioned at: {arrowRect.anchoredPosition}");
                }
                else
                {
                    arrowImage.gameObject.SetActive(false);
                    if (enableDebugLogs)
                        Debug.LogWarning($"[Item] No valid pattern images found for {itemName}");
                }
            }
            else
            {
                if (arrowImage != null) arrowImage.gameObject.SetActive(false);
                if (enableDebugLogs)
                    Debug.LogWarning($"[Item] Cannot position arrow for {itemName}. arrowImage or patternDisplayImages is null/empty.");
            }
        }

        // === МЕТОДЫ РАНДОМИЗАЦИИ ===
        
        /// <summary>
        /// Рандомизирует паттерн/состояние (заглушка)
        /// </summary>
        public void RandomizePattern()
        {
            if (enableDebugLogs)
                Debug.Log($"[Item] {itemName} - паттерн рандомизирован");
        }

        /// <summary>
        /// Рандомизирует основной паттерн (заглушка)
        /// </summary>
        public void RandomizeMainPattern()
        {
            selectedPattern = "Обычный";
        }

        /// <summary>
        /// Рандомизирует цену из массива
        /// </summary>
        public void RandomizePrice()
        {
            if (possiblePrices != null && possiblePrices.Length > 0)
            {
                currentPrice = possiblePrices[Random.Range(0, possiblePrices.Length)];
                if (enableDebugLogs)
                    Debug.Log($"[Item] {itemName} - рандомная цена: {currentPrice}");
            }
        }

        // === МЕТОДЫ ПАТТЕРНА/СОСТОЯНИЯ ===
        
        /// <summary>
        /// Получает текущий паттерн/состояние (заглушка)
        /// </summary>
        public int GetPattern()
        {
            return 2; // Нормальное состояние по умолчанию
        }

        /// <summary>
        /// Получает название паттерна/состояния (заглушка)
        /// </summary>
        public string GetPatternName()
        {
            return "Нормальное";
        }

        /// <summary>
        /// Обновляет пользовательскую UI панель
        /// </summary>
        public void UpdateCustomUI()
        {
            // Обновляем текстовые поля независимо от активности панели
            if (customItemNameText)
                customItemNameText.text = itemName;

            if (customItemDescriptionText)
                customItemDescriptionText.text = GetFullDescription();
                
            if (customItemPriceText)
                customItemPriceText.text = $"{GetCurrentValue()} USD";
                
            if (enableDebugLogs)
                Debug.Log($"[Item] UI обновлен для {itemName}: цена {GetCurrentValue()} USD");
        }

        // === МЕТОДЫ ТИПА ПРЕДМЕТА ===
        
        public ItemType GetItemType()
        {
            return currentItemType;
        }

        public string GetItemTypeDisplayName()
        {
            switch (currentItemType)
            {
                case ItemType.Contraband: return "Контрабанда";
                case ItemType.Models: return "Модель";
                case ItemType.Magic: return "Магический предмет";
                case ItemType.MagicContraband: return "Магическая контрабанда";
                case ItemType.Animated: return "Анимированный предмет";
                case ItemType.AnimatedContraband: return "Анимированная контрабанда";
                default: return "Неизвестно";
            }
        }

        public bool IsContraband()
        {
            return currentItemType == ItemType.Contraband ||
                   currentItemType == ItemType.MagicContraband ||
                   currentItemType == ItemType.AnimatedContraband;
        }

        // === МЕТОДЫ СТОИМОСТИ ===
        
        /// <summary>
        /// Получает текущую стоимость предмета
        /// </summary>
        public int GetCurrentValue()
        {
            return currentPrice;
        }

        /// <summary>
        /// Устанавливает цену предмета
        /// </summary>
        public void SetPrice(int price)
        {
            currentPrice = price;
        }

        /// <summary>
        /// Получает базовую цену из массива
        /// </summary>
        public int GetBasePrice()
        {
            if (possiblePrices != null && possiblePrices.Length > 0)
                return possiblePrices[0];
            return 100;
        }

        // === МЕТОДЫ ОПИСАНИЯ ===
        
        public string GetFullDescription()
        {
            string desc = itemDescription;

            if (!string.IsNullOrEmpty(selectedPattern))
                desc += $"\nОсновной паттерн: {selectedPattern}";

            return desc;
        }

        // === ОПРЕДЕЛЕНИЕ ОСОБЕННОСТЕЙ ===
        
        /// <summary>
        /// Определяет есть ли у предмета магические эффекты
        /// </summary>
        public bool HasMagicEffects()
        {
            return currentItemType == ItemType.Magic ||
                   currentItemType == ItemType.MagicContraband;
        }

        /// <summary>
        /// Определяет есть ли у предмета анимации
        /// </summary>
        public bool HasAnimations()
        {
            return currentItemType == ItemType.Animated ||
                   currentItemType == ItemType.AnimatedContraband;
        }

        /// <summary>
        /// Проверяет соответствует ли предмет поисковому запросу
        /// </summary>
        public bool MatchesSearch(string searchQuery)
        {
            if (string.IsNullOrEmpty(searchQuery)) return true;

            string query = searchQuery.ToLower();
            
            // Поиск по основным полям
            if (itemName.ToLower().Contains(query)) return true;
            if (itemDescription.ToLower().Contains(query)) return true;
            if (GetItemTypeDisplayName().ToLower().Contains(query)) return true;
            if (GetPatternName().ToLower().Contains(query)) return true;

            // Поиск по ключевым словам
            switch (query)
            {
                case "контрабанда": return IsContraband();
                case "магия": return HasMagicEffects();
                case "анимация": return HasAnimations();
                case "новое": return true;
                case "поврежденное": return false;
                default: return true;
            }
        }

        // === СОБЫТИЯ ВЗАИМОДЕЙСТВИЯ ===
        
        public void OnInteract()
        {
            Debug.Log($"[Item] {itemName} - взаимодействие");
            OnInteracted?.Invoke(this);
        }

        public void OnInspect()
        {
            Debug.Log($"[Item] {itemName} осмотрен - {GetFullDescription()}");
            
            // Активируем UI панель и обновляем информацию при осмотре
            if (customUIPanel != null)
            {
                customUIPanel.SetActive(true);
                UpdateCustomUI(); // Обновляем всю информацию включая полное описание и цену
            }
            
            OnInspected?.Invoke(this);
        }
        
        /// <summary>
        /// Скрывает UI панель предмета (для выхода из осмотра)
        /// </summary>
        public void HideCustomUI()
        {
            if (customUIPanel != null)
            {
                customUIPanel.SetActive(false);
                if (enableDebugLogs)
                    Debug.Log($"[Item] UI панель скрыта для {itemName}");
            }
        }

        public void OnPickup()
        {
            Debug.Log($"[Item] {itemName} подобран");
            // Скрываем UI панель при подборе
            HideCustomUI();
            OnPickedUp?.Invoke(this);
        }

        public void OnDrop()
        {
            Debug.Log($"[Item] {itemName} выброшен");
            // Скрываем UI панель при выбрасывании
            HideCustomUI();
            OnDropped?.Invoke(this);
        }

        // === СОХРАНЕНИЕ/ЗАГРУЗКА ===
        
        [System.Serializable]
        public class ItemSaveData
        {
            public string itemName;
            public string itemDescription;
            public string selectedPattern;
            public int currentPrice;
            public int itemType;
            public Vector3 position;
            public Vector3 rotation;
        }

        public ItemSaveData GetSaveData()
        {
            return new ItemSaveData
            {
                itemName = this.itemName,
                itemDescription = this.itemDescription,
                selectedPattern = this.selectedPattern,
                currentPrice = this.currentPrice,
                itemType = (int)this.currentItemType,
                position = transform.position,
                rotation = transform.eulerAngles
            };
        }

        public void LoadSaveData(ItemSaveData data)
        {
            itemName = data.itemName;
            itemDescription = data.itemDescription;
            selectedPattern = data.selectedPattern;
            currentPrice = data.currentPrice;
            currentItemType = (ItemType)data.itemType;

            transform.position = data.position;
            transform.rotation = Quaternion.Euler(data.rotation);

            // Обновляем UI после загрузки
            UpdatePatternDisplay();
            PositionArrowRandomly();
            UpdateCustomUI();
        }

        public string GetUniqueId()
        {
            return uniqueItemId;
        }

        public void SetUniqueId(string id)
        {
            uniqueItemId = id;
        }

        // === ГИЗМО ===
        
        void OnDrawGizmosSelected()
        {
            // Основная сфера предмета
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.3f);

            // Центральная точка
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 0.1f);
        }
    }
} 