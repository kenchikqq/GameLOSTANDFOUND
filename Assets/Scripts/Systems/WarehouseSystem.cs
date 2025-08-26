using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace LostAndFound.Systems
{
    /// <summary>
    /// СИСТЕМА СКЛАДА - телепортация, хранение и управление предметами
    /// Горячие клавиши: Z = отправить предмет на склад (рядом со складом)
    /// </summary>
    public class WarehouseSystem : MonoBehaviour
    {
        [Header("Настройки склада")]
        public Transform warehouseLocation;
        public bool autoAssignIds = true;
        public bool enableDebugLogs = true;
        
        [Header("Позиции на складе")]
        [Tooltip("Точки где будут появляться предметы на складе")]
        public Transform[] itemPositions;
        
        [Tooltip("Расстояние между предметами если позиции не заданы")]
        public float positionSpacing = 2f;
        
        [Header("Взаимодействие игрока")]
        public float interactionRange = 3f;
        public LayerMask playerLayer = 1;

        [Header("Статистика (только для чтения)")]
        [SerializeField] private int totalItemsStored = 0;
        [SerializeField] private int contrabandItemsCount = 0;
        [SerializeField] private int magicItemsCount = 0;
        [SerializeField] private int animatedItemsCount = 0;
        [SerializeField] private int modelItemsCount = 0;

        // Внутренние данные
        private Dictionary<string, LostAndFound.Systems.Item> warehouseItems = new Dictionary<string, LostAndFound.Systems.Item>();
        private int nextUniqueId = 1000; // Начальный ID
        private List<Vector3> availablePositions = new List<Vector3>();
        private List<Vector3> occupiedPositions = new List<Vector3>(); // Отслеживаем занятые позиции
        private Dictionary<string, Vector3> itemToPositionMap = new Dictionary<string, Vector3>(); // Связь предмет -> позиция
        private bool isPlayerNearby = false;

        // События
        public System.Action<LostAndFound.Systems.Item> OnItemStored;
        public System.Action<LostAndFound.Systems.Item> OnItemRetrieved;
        public System.Action OnStatisticsUpdated;

        // Singleton паттерн
        public static WarehouseSystem Instance { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        void Start()
        {
            InitializeWarehouse();
        }

        void Update()
        {
            HandlePlayerProximity();
            HandleInput();
        }

        void InitializeWarehouse()
        {
            GeneratePositions();
            ClearPositionTracking(); // Очищаем данные позиций
            LoadWarehouseData();
            
            if (enableDebugLogs)
                Debug.Log("[WarehouseSystem] Система склада инициализирована");
        }
        
        /// <summary>
        /// Очищает данные отслеживания позиций
        /// </summary>
        void ClearPositionTracking()
        {
            occupiedPositions.Clear();
            itemToPositionMap.Clear();
            
            if (enableDebugLogs)
                Debug.Log("[WarehouseSystem] Данные позиций очищены");
        }

        void GeneratePositions()
        {
            availablePositions.Clear();
            
            if (itemPositions != null && itemPositions.Length > 0)
            {
                // Используем заданные позиции
                foreach (Transform pos in itemPositions)
                {
                    if (pos != null)
                        availablePositions.Add(pos.position);
                }
            }
            else
            {
                // Генерируем сетку позиций
                Vector3 startPos = warehouseLocation ? warehouseLocation.position : Vector3.zero;
                
                for (int x = 0; x < 5; x++)
                {
                    for (int z = 0; z < 5; z++)
                    {
                        Vector3 newPos = startPos + new Vector3(x * positionSpacing, 0, z * positionSpacing);
                        availablePositions.Add(newPos);
                    }
                }
            }
            
            if (enableDebugLogs)
                Debug.Log($"[WarehouseSystem] Создано {availablePositions.Count} позиций для предметов");
        }

        void HandlePlayerProximity()
        {
            if (!warehouseLocation) return;
            
            // Проверяем расстояние до игрока
            GameObject player = GameObject.FindWithTag("Player");
            if (player)
            {
                float distance = Vector3.Distance(player.transform.position, warehouseLocation.position);
                bool wasNearby = isPlayerNearby;
                isPlayerNearby = distance <= interactionRange;
                
                if (isPlayerNearby && !wasNearby)
                {
                    ShowInteractionHint();
                }
                else if (!isPlayerNearby && wasNearby)
                {
                    HideInteractionHint();
                }
            }
        }

        void HandleInput()
        {
            if (isPlayerNearby && Input.GetKeyDown(KeyCode.Z))
            {
                TryStorePlayerItem();
            }
        }

        void TryStorePlayerItem()
        {
            // Получаем предмет из инвентаря игрока
            if (PlayerInventory.Instance && PlayerInventory.Instance.HasItem())
            {
                LostAndFound.Systems.Item item = PlayerInventory.Instance.GetCurrentItem();
                if (item != null)
                {
                    if (enableDebugLogs)
                        Debug.Log($"[WarehouseSystem] Попытка разместить предмет: {item.itemName}. Занято позиций: {occupiedPositions.Count}/{availablePositions.Count}");
                    
                    // Убираем из инвентаря
                    PlayerInventory.Instance.RemoveItem();
                    
                    // Отправляем на склад
                    bool success = StoreItem(item);
                    
                    if (enableDebugLogs)
                    {
                        if (success)
                            Debug.Log($"[WarehouseSystem] ✅ Предмет {item.itemName} успешно размещен на складе");
                        else
                            Debug.LogError($"[WarehouseSystem] ❌ Не удалось разместить предмет {item.itemName} на складе");
                    }
                }
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log("[WarehouseSystem] Нет предмета в инвентаре для размещения");
            }
        }

        // === ОСНОВНЫЕ МЕТОДЫ СКЛАДА ===

        /// <summary>
        /// Сохраняет предмет на складе
        /// </summary>
        public bool StoreItem(LostAndFound.Systems.Item item)
        {
            if (item == null) return false;

            // ВСЕГДА создаем новый уникальный ID для каждого размещения
            string newItemId = $"WH_{nextUniqueId++}_{System.DateTime.Now.Ticks}";
            item.SetUniqueId(newItemId);

            Debug.Log($"[WarehouseSystem] === РАЗМЕЩЕНИЕ ПРЕДМЕТА НА СКЛАДЕ ===");
            Debug.Log($"[WarehouseSystem] Название: '{item.itemName}'");
            Debug.Log($"[WarehouseSystem] Описание: '{item.itemDescription}'");
            Debug.Log($"[WarehouseSystem] Новый ID: {newItemId}");
            Debug.Log($"[WarehouseSystem] Предметов на складе ДО: {warehouseItems.Count}");

            // Добавляем в коллекцию (теперь ВСЕГДА уникальный ID)
            warehouseItems[newItemId] = item;
            totalItemsStored++;
            
            Debug.Log($"[WarehouseSystem] Предметов на складе ПОСЛЕ: {warehouseItems.Count}");
            
            // Телепортируем предмет
            TeleportItemToWarehouse(item);
            
            // Обновляем статистику
            UpdateStatistics();
            
            // Вызываем событие
            OnItemStored?.Invoke(item);
            
            Debug.Log($"[WarehouseSystem] ✅ Предмет успешно сохранен: '{item.itemName}' (ID: {newItemId}). Всего на складе: {totalItemsStored}");
            
            return true;
        }

        /// <summary>
        /// Телепортирует предмет в свободную позицию на складе
        /// </summary>
        void TeleportItemToWarehouse(LostAndFound.Systems.Item item)
        {
            Vector3 targetPosition = GetNextAvailablePosition();
            
            if (targetPosition != Vector3.zero)
            {
                // Размещаем предмет в новой позиции
                item.transform.position = targetPosition;
                item.transform.rotation = Quaternion.identity;
                
                // ГАРАНТИРОВАННО делаем предмет видимым и активным
                item.gameObject.SetActive(true);
                
                // Убеждаемся что предмет не заблокирован другими компонентами
                Renderer itemRenderer = item.GetComponent<Renderer>();
                if (itemRenderer != null)
                {
                    itemRenderer.enabled = true;
                }

                // ВАЖНО: Отмечаем позицию как занятую
                if (!occupiedPositions.Contains(targetPosition))
                {
                    occupiedPositions.Add(targetPosition);
                }
                
                // Сохраняем связь предмет -> позиция
                string itemId = item.GetUniqueId();
                itemToPositionMap[itemId] = targetPosition;

                if (enableDebugLogs)
                    Debug.Log($"[WarehouseSystem] ✅ Предмет '{item.itemName}' размещен в позиции: {targetPosition}. Занято позиций: {occupiedPositions.Count}/{availablePositions.Count}. Активен: {item.gameObject.activeInHierarchy}");
            }
            else
            {
                Debug.LogError($"[WarehouseSystem] ❌ КРИТИЧЕСКАЯ ОШИБКА: Не удалось найти позицию для предмета {item.itemName}!");
            }
        }

        /// <summary>
        /// Получает следующую доступную позицию для предмета
        /// </summary>
        Vector3 GetNextAvailablePosition()
        {
            // Ищем первую незанятую позицию
            foreach (Vector3 pos in availablePositions)
            {
                if (!occupiedPositions.Contains(pos))
                {
                    return pos;
                }
            }
            
            // Если все основные позиции заняты, создаем новую позицию в сетке
            if (warehouseLocation != null)
            {
                // Расширяем склад по сетке
                int gridSize = Mathf.CeilToInt(Mathf.Sqrt(totalItemsStored + 1));
                Vector3 basePos = warehouseLocation.position;
                
                // Находим первую свободную позицию в расширенной сетке
                for (int x = 0; x < gridSize + 2; x++)
                {
                    for (int z = 0; z < gridSize + 2; z++)
                    {
                        Vector3 testPos = basePos + new Vector3(x * positionSpacing, 0, z * positionSpacing);
                        
                        if (!occupiedPositions.Contains(testPos))
                        {
                            // Добавляем новую позицию в список доступных
                            availablePositions.Add(testPos);
                            
                            if (enableDebugLogs)
                                Debug.Log($"[WarehouseSystem] Создана новая позиция в сетке: {testPos} (сетка {gridSize+2}x{gridSize+2})");
                            
                            return testPos;
                        }
                    }
                }
                
                // Если и это не помогло, создаем случайную позицию
                Vector3 randomPos = basePos + new Vector3(
                    UnityEngine.Random.Range(-20f, 20f), 
                    0, 
                    UnityEngine.Random.Range(-20f, 20f)
                );
                
                if (enableDebugLogs)
                    Debug.LogWarning($"[WarehouseSystem] Сетка заполнена! Создана случайная позиция: {randomPos}");
                
                return randomPos;
            }
            
            if (enableDebugLogs)
                Debug.LogError("[WarehouseSystem] Критическая ошибка: не удается создать позицию для предмета!");
                
            // Последний резерв - просто ставим в центр мира
            return Vector3.zero;
        }

        /// <summary>
        /// Извлекает предмет со склада по ID
        /// </summary>
        public LostAndFound.Systems.Item RetrieveItem(string itemId)
        {
            if (warehouseItems.ContainsKey(itemId))
            {
                LostAndFound.Systems.Item item = warehouseItems[itemId];
                warehouseItems.Remove(itemId);
                totalItemsStored--;
                
                // ВАЖНО: Освобождаем позицию
                if (itemToPositionMap.ContainsKey(itemId))
                {
                    Vector3 itemPosition = itemToPositionMap[itemId];
                    occupiedPositions.Remove(itemPosition);
                    itemToPositionMap.Remove(itemId);
                    
                    if (enableDebugLogs)
                        Debug.Log($"[WarehouseSystem] Позиция {itemPosition} освобождена. Занятых позиций: {occupiedPositions.Count}");
                }
                
                // Обновляем статистику
                UpdateStatistics();
                
                // Вызываем событие
                OnItemRetrieved?.Invoke(item);
                
                if (enableDebugLogs)
                    Debug.Log($"[WarehouseSystem] Предмет извлечен: {item.itemName}");
                
                return item;
            }
            
            return null;
        }

        /// <summary>
        /// Получает предмет по ID без извлечения
        /// </summary>
        public LostAndFound.Systems.Item GetItem(string itemId)
        {
            return warehouseItems.ContainsKey(itemId) ? warehouseItems[itemId] : null;
        }

        /// <summary>
        /// Получает все предметы на складе
        /// </summary>
        public List<LostAndFound.Systems.Item> GetAllItems()
        {
            return warehouseItems.Values.ToList();
        }

        /// <summary>
        /// Проверяет состояние склада
        /// </summary>
        [ContextMenu("Проверить состояние склада")]
        public void CheckWarehouseStatus()
        {
            Debug.Log($"[WarehouseSystem] === ПРОВЕРКА СОСТОЯНИЯ СКЛАДА ===");
            Debug.Log($"[WarehouseSystem] Всего предметов на складе: {warehouseItems.Count}");
            Debug.Log($"[WarehouseSystem] totalItemsStored: {totalItemsStored}");
            
            int index = 1;
            foreach (var kvp in warehouseItems)
            {
                var item = kvp.Value;
                Debug.Log($"[WarehouseSystem] Предмет #{index}: ID='{kvp.Key}', Name='{item.itemName}', Description='{item.itemDescription}'");
                index++;
            }
            
            Debug.Log($"[WarehouseSystem] === ПРОВЕРКА ЗАВЕРШЕНА ===");
        }

        /// <summary>
        /// Получает предметы по типу
        /// </summary>
        public List<LostAndFound.Systems.Item> GetItemsByType(LostAndFound.Systems.Item.ItemType type)
        {
            return warehouseItems.Values.Where(item => item.GetItemType() == type).ToList();
        }

        /// <summary>
        /// Получает все контрабандные предметы
        /// </summary>
        public List<LostAndFound.Systems.Item> GetContrabandItems()
        {
            return warehouseItems.Values.Where(item => item.IsContraband()).ToList();
        }

        // === ПОИСК И ФИЛЬТРАЦИЯ ===

        /// <summary>
        /// Поиск предметов по названию
        /// </summary>
        public List<LostAndFound.Systems.Item> SearchItems(string searchQuery)
        {
            if (string.IsNullOrEmpty(searchQuery))
                return GetAllItems();
                
            return warehouseItems.Values.Where(item => 
                item.MatchesSearch(searchQuery)).ToList();
        }

        /// <summary>
        /// Фильтр предметов по состоянию/патерну
        /// </summary>
        public List<LostAndFound.Systems.Item> GetItemsByPattern(int patternLevel)
        {
            return warehouseItems.Values.Where(item => item.GetPattern() == patternLevel).ToList();
        }

        /// <summary>
        /// Получает предметы в определенном ценовом диапазоне
        /// </summary>
        public List<LostAndFound.Systems.Item> GetItemsByValueRange(int minValue, int maxValue)
        {
            return warehouseItems.Values.Where(item => 
                item.GetCurrentValue() >= minValue && 
                item.GetCurrentValue() <= maxValue).ToList();
        }

        // === СТАТИСТИКА ===

        /// <summary>
        /// Обновляет статистику склада
        /// </summary>
        void UpdateStatistics()
        {
            contrabandItemsCount = warehouseItems.Values.Count(item => item.IsContraband());
            magicItemsCount = warehouseItems.Values.Count(item => item.HasMagicEffects());
            animatedItemsCount = warehouseItems.Values.Count(item => item.HasAnimations());
            modelItemsCount = warehouseItems.Values.Count(item => item.GetItemType() == LostAndFound.Systems.Item.ItemType.Models);

            OnStatisticsUpdated?.Invoke();
        }

        /// <summary>
        /// Получает статистику склада
        /// </summary>
        public WarehouseStatistics GetStatistics()
        {
            return new WarehouseStatistics
            {
                TotalItems = totalItemsStored,
                ContrabandItems = contrabandItemsCount,
                MagicItems = magicItemsCount,
                AnimatedItems = animatedItemsCount,
                ModelItems = modelItemsCount,
                TotalValue = warehouseItems.Values.Sum(item => item.GetCurrentValue())
            };
        }

        // === СОХРАНЕНИЕ/ЗАГРУЗКА ===

        /// <summary>
        /// Сохраняет данные склада
        /// </summary>
        public void SaveWarehouseData()
        {
            WarehouseSaveData saveData = new WarehouseSaveData
            {
                nextUniqueId = this.nextUniqueId,
                totalItemsStored = this.totalItemsStored,
                itemIds = warehouseItems.Keys.ToArray(),
                itemSaveData = warehouseItems.Values.Select(item => item.GetSaveData()).ToArray()
            };

            string json = JsonUtility.ToJson(saveData, true);
            PlayerPrefs.SetString("WarehouseData", json);
            PlayerPrefs.Save();

            if (enableDebugLogs)
                Debug.Log("[WarehouseSystem] Данные склада сохранены");
        }

        /// <summary>
        /// Загружает данные склада
        /// </summary>
        public void LoadWarehouseData()
        {
            if (PlayerPrefs.HasKey("WarehouseData"))
            {
                string json = PlayerPrefs.GetString("WarehouseData");
                WarehouseSaveData saveData = JsonUtility.FromJson<WarehouseSaveData>(json);

                nextUniqueId = saveData.nextUniqueId;
                totalItemsStored = saveData.totalItemsStored;

                // Очищаем существующие данные
                warehouseItems.Clear();

                // Восстанавливаем предметы (здесь нужна более сложная логика для реальной игры)
                if (enableDebugLogs)
                    Debug.Log($"[WarehouseSystem] Загружено {saveData.itemIds.Length} предметов");
            }
        }

        // === УТИЛИТЫ ===

        void ShowInteractionHint()
        {
            if (enableDebugLogs)
                Debug.Log("[WarehouseSystem] Z - отправить предмет на склад");
        }

        void HideInteractionHint()
        {
            // Скрываем подсказку взаимодействия
        }

        /// <summary>
        /// Очищает весь склад
        /// </summary>
        public void ClearWarehouse()
        {
            foreach (var item in warehouseItems.Values)
            {
                if (item != null)
                    DestroyImmediate(item.gameObject);
            }
            
            warehouseItems.Clear();
            totalItemsStored = 0;
            UpdateStatistics();
            
            if (enableDebugLogs)
                Debug.Log("[WarehouseSystem] Склад очищен");
        }

        /// <summary>
        /// Печатает информацию о складе в консоль
        /// </summary>
        [ContextMenu("Показать информацию о складе")]
        public void PrintWarehouseInfo()
        {
            Debug.Log($"=== ИНФОРМАЦИЯ О СКЛАДЕ ===");
            Debug.Log($"Всего предметов: {totalItemsStored}");
            Debug.Log($"Контрабанда: {contrabandItemsCount}");
            Debug.Log($"Магические: {magicItemsCount}");
            Debug.Log($"Анимированные: {animatedItemsCount}");
            Debug.Log($"Модели: {modelItemsCount}");
            Debug.Log($"Следующий ID: {nextUniqueId}");
            Debug.Log($"Доступно позиций: {availablePositions.Count}");
        }

        // === КЛАССЫ ДАННЫХ ===

        [System.Serializable]
        public class WarehouseStatistics
        {
            public int TotalItems;
            public int ContrabandItems;
            public int MagicItems;
            public int AnimatedItems;
            public int ModelItems;
            public int TotalValue;
        }

        [System.Serializable]
        public class WarehouseSaveData
        {
            public int nextUniqueId;
            public int totalItemsStored;
            public string[] itemIds;
            public LostAndFound.Systems.Item.ItemSaveData[] itemSaveData;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void OnDrawGizmosSelected()
        {
            // Показываем зону взаимодействия
            if (warehouseLocation)
            {
                Gizmos.color = isPlayerNearby ? Color.green : Color.blue;
                Gizmos.DrawWireSphere(warehouseLocation.position, interactionRange);
            }

            // Показываем позиции для предметов
            Gizmos.color = Color.yellow;
            foreach (Vector3 pos in availablePositions)
            {
                Gizmos.DrawWireCube(pos, Vector3.one * 0.3f);
            }
        }
    }
} 