using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.AI;
using LostAndFound.NPCs;
using LostAndFound.Systems;
using System.Collections.Generic;

namespace LostAndFound.NPCs
{
    public enum DetailLevel { Full, Half, Brief, Random }
    
    public class BlackNPC : MonoBehaviour
    {
        public enum NPCState { Idle, InDialog, Finished }
        public enum PanelState { None, MainPanel, DetailPanel, QuestionPanel, RefusePanel }
        
        private NPCState npcState = NPCState.Idle;
        private PanelState currentPanelState = PanelState.None;
        private bool dialogWasInterrupted = false;
        
        [Header("Предмет")]
        private Item wantedItemData;
        private bool dialogCompleted = false;
        private List<Item> availableItems = new List<Item>(); // Все доступные предметы из папки
        private Item currentSearchingItem; // Предмет, который ищет NPC
        private string currentRandomItemName = null; // Сохраняем выбранное случайное имя предмета

        [Header("Система предметов")]
        public bool useRandomItems = true; // Использовать случайные предметы или реальные из папки
        public bool canLieAboutItems = true; // Может ли NPC лгать о несуществующих предметах

        [Header("Система анимации")]
        private Animator animator;
        private bool isWalking = false;
        private bool isIdle = true;

        [Header("NavMesh")]
        private NavMeshAgent navAgent;
        private Transform targetPosition; // Задается спавнером автоматически
        private Transform spawnPoint; // Задается спавнером автоматически
        private Transform doorTrigger; // промежуточная точка у двери
        private bool doorEntryCompleted = false; // прошли дверь на входе
        public float stoppingDistance = 2f;
        [Tooltip("На каком расстоянии от точки возврата NPC считается прибывшим и исчезает")]
        public float despawnThreshold = 0.3f;

        [Header("Система очереди")]
        private int queuePosition = -1; // Позиция в очереди (-1 = не в очереди)
        private Vector3 targetQueuePosition; // Конкретная позиция в очереди
        private bool isInQueue = false;

        [Header("Система расстояний")]
        public float minDistanceToOtherNPCs = 2f; // Минимальное расстояние до других NPCs
        private bool isStoppedByDistance = false;

        [Header("UI - Мэйн панель")]
        public GameObject mainPanel;
        public TextMeshProUGUI mainText;
        public Button tellMoreButton; // "Расскажите подробнее"
        public Button cannotHelpButton; // "Ничем не могу помочь"

        [Header("UI - Панель подробного описания")]
        public GameObject detailPanel;
        public TextMeshProUGUI detailText;
        public Button searchWarehouseButton; // "Сейчас поищу на складе"
        public Button refuseClientButton; // "Отказать клиенту"

        [Header("UI - Панель вопроса о предмете")]
        public GameObject questionPanel;
        public TextMeshProUGUI questionText;
        public Button foundItemButton; // "Да, вот ваш предмет"
        public Button notFoundItemButton; // "Нет, я не нашел вашего предмета"

        [Header("UI - Панель отказа")]
        public GameObject refusePanel;
        public TextMeshProUGUI refuseText;
        public Button closeRefuseButton; // "Закрыть"

        [Header("Настройки детализации описания")]
        public DetailLevel detailLevel = DetailLevel.Random;
        public bool useRandomDetailLevel = true; // Случайный уровень детализации для каждого NPC

        [Header("Настройки диалога")]
        public string[] greetingPhrases = {
            "Здравствуйте! Я потерял {0}. Можете помочь?",
            "Добрый день! Ищу {0}, очень важно найти. Вы не видели?",
            "Привет! Потерял {0}, очень расстроен. Поможете?",
            "Здравствуйте! Ищу {0}, может быть у вас есть?"
        };

        public string[] randomItemNames = {
            "ключи от машины",
            "кошелек",
            "телефон",
            "часы",
            "очки",
            "сумку",
            "зонт",
            "документы",
            "паспорт",
            "карточку",
            "кепку",
            "куртку",
            "рюкзак",
            "ноутбук",
            "планшет"
        };

        [Header("Описания случайных предметов")]
        public string[] keyDescriptions = {
            "Ключи от моей машины, серебристые, с брелком в виде футбольного мяча. Потерял их вчера в торговом центре.",
            "Автомобильные ключи, черные, с кнопкой сигнализации. Очень важно найти, без них не могу ездить.",
            "Ключи от машины, с металлическим брелком. Потерял их в парке, когда гулял с собакой."
        };

        public string[] walletDescriptions = {
            "Кожаный кошелек, коричневый, с отделениями для карточек. Внутри были деньги и документы.",
            "Кошелек из черной кожи, с застежкой. Потерял его в автобусе, когда доставал проездной.",
            "Коричневый кошелек, с отделением для мелочи. Внутри были кредитные карты и водительские права."
        };

        public string[] phoneDescriptions = {
            "Смартфон Samsung, черный, с чехлом синего цвета. На экране была фотография моей семьи.",
            "Телефон iPhone, белый, с трещиной на экране. Потерял его в кафе, когда обедал.",
            "Мобильный телефон, с красным чехлом. Внутри много важных контактов и фотографий."
        };

        public string[] watchDescriptions = {
            "Наручные часы, серебристые, с черным ремешком. Подарок от родителей на день рождения.",
            "Часы Casio, черные, с цифровым дисплеем. Потерял их в спортзале, когда занимался.",
            "Наручные часы, золотистые, с кожаным ремешком. Очень дорогие, подарок от жены."
        };

        public string[] capDescriptions = {
            "Кепка бейсбольная, красная, с логотипом любимой команды. Носил её каждый день.",
            "Кепка из джинсовой ткани, синяя, с вышивкой. Подарок от друга, очень дорогая память.",
            "Кепка спортивная, черная, с белой вышивкой. Потерял её в парке во время пробежки."
        };

        private bool isInDialog = false;
        private bool isSearchingForItem = false; // Флаг поиска предмета
        public System.Action<BlackNPC> OnNPCFinished;

        // Методы для спавнера
        public void SetTargetPosition(Transform target) { targetPosition = target; }
        public void SetSpawnPoint(Transform spawn) { spawnPoint = spawn; }
        public void SetDoorTrigger(Transform door) { doorTrigger = door; }
        public Transform GetTargetPosition() { return targetPosition; }
        public void SetQueuePosition(int position) { queuePosition = position; }
        public int GetQueuePosition() { return queuePosition; }

        void Start()
        {
            Debug.Log("[BlackNPC] NPC создан");
            LoadAvailableItems();
            SetupDialogUI();
            SetupNavMeshAgent();
            StartCoroutine(CheckDistanceToOtherNPCs());
        }

        void LoadAvailableItems()
        {
            // Загружаем все предметы из папки Resources/Items
            Item[] items = Resources.LoadAll<Item>("Items");
            availableItems.Clear();
            availableItems.AddRange(items);
            
            Debug.Log($"[BlackNPC] Загружено {availableItems.Count} предметов из папки Resources/Items");
            
            // Выбираем случайный предмет для поиска
            SelectRandomSearchingItem();
        }

        void SelectRandomSearchingItem()
        {
            if (useRandomItems && canLieAboutItems)
            {
                // NPC может лгать о несуществующих предметах
                string randomItemName = randomItemNames[Random.Range(0, randomItemNames.Length)];
                Debug.Log($"[BlackNPC] NPC ищет случайный предмет: {randomItemName}");
                currentSearchingItem = null; // Несуществующий предмет
                currentRandomItemName = randomItemName; // Сохраняем выбранное случайное имя
            }
            else if (availableItems.Count > 0)
            {
                // NPC ищет реальный предмет из папки
                currentSearchingItem = availableItems[Random.Range(0, availableItems.Count)];
                Debug.Log($"[BlackNPC] NPC ищет реальный предмет: {currentSearchingItem.itemName}");
                currentRandomItemName = null; // Сбрасываем случайное имя, если используется реальный
            }
            else
            {
                // Если папка пустая, используем случайный
                string randomItemName = randomItemNames[Random.Range(0, randomItemNames.Length)];
                Debug.Log($"[BlackNPC] Папка пустая, NPC ищет случайный предмет: {randomItemName}");
                currentSearchingItem = null;
                currentRandomItemName = randomItemName; // Сохраняем выбранное случайное имя
            }
        }

        string GenerateDetailedDescription()
        {
            string itemName = GetCurrentItemName();
            DetailLevel currentDetailLevel = useRandomDetailLevel ? 
                (DetailLevel)Random.Range(0, 4) : detailLevel;

            // Если это реальный предмет из папки
            if (currentSearchingItem != null)
            {
                return GenerateRealItemDescription(currentDetailLevel);
            }
            else
            {
                // Если это случайный предмет
                return GenerateRandomItemDescription(itemName, currentDetailLevel);
            }
        }

        string GenerateRealItemDescription(DetailLevel level)
        {
            string fullDescription = currentSearchingItem.itemDescription;
            
            switch (level)
            {
                case DetailLevel.Full:
                    return fullDescription;
                    
                case DetailLevel.Half:
                    // Берем половину описания
                    int halfLength = fullDescription.Length / 2;
                    return fullDescription.Substring(0, halfLength) + "...";
                    
                case DetailLevel.Brief:
                    // Берем только первые 1-2 предложения
                    string[] sentences = fullDescription.Split('.');
                    if (sentences.Length >= 2)
                        return sentences[0] + ". " + sentences[1] + ".";
                    else
                        return sentences[0] + ".";
                        
                default:
                    return fullDescription;
            }
        }

        string GenerateRandomItemDescription(string itemName, DetailLevel level)
        {
            string[] descriptions = GetRandomDescriptionsForItem(itemName);
            string fullDescription = descriptions[Random.Range(0, descriptions.Length)];
            
            switch (level)
            {
                case DetailLevel.Full:
                    return fullDescription;
                    
                case DetailLevel.Half:
                    // Берем половину описания
                    int halfLength = fullDescription.Length / 2;
                    return fullDescription.Substring(0, halfLength) + "...";
                    
                case DetailLevel.Brief:
                    // Берем только первые 1-2 предложения
                    string[] sentences = fullDescription.Split('.');
                    if (sentences.Length >= 2)
                        return sentences[0] + ". " + sentences[1] + ".";
                    else
                        return sentences[0] + ".";
                        
                default:
                    return fullDescription;
            }
        }

        string[] GetRandomDescriptionsForItem(string itemName)
        {
            if (itemName.Contains("ключ"))
                return keyDescriptions;
            else if (itemName.Contains("кошелек"))
                return walletDescriptions;
            else if (itemName.Contains("телефон"))
                return phoneDescriptions;
            else if (itemName.Contains("часы"))
                return watchDescriptions;
            else if (itemName.Contains("кепк"))
                return capDescriptions;
            else
                return keyDescriptions; // По умолчанию
        }

        string GetCurrentItemName()
        {
            if (currentSearchingItem != null)
                return currentSearchingItem.itemName;
            else if (currentRandomItemName != null)
                return currentRandomItemName;
            else
                return randomItemNames[Random.Range(0, randomItemNames.Length)];
        }

        void SetupNavMeshAgent()
        {
            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                Debug.LogError("[BlackNPC] NavMeshAgent не найден! Добавьте NavMeshAgent компонент вручную.");
                return;
            }
            
            // Настройки агента
            navAgent.speed = 3.5f;
            navAgent.angularSpeed = 120f;
            navAgent.acceleration = 8f;
            navAgent.stoppingDistance = stoppingDistance;
            
            // Старт: сначала к триггеру двери, затем к цели
            if (doorTrigger != null)
                navAgent.SetDestination(doorTrigger.position);
            else if (targetPosition != null)
                navAgent.SetDestination(targetPosition.position);
        }

        void Update()
        {
            if (npcState == NPCState.Finished) return;

            // Обработка ESC для закрытия диалога
            if (isInDialog && Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("[BlackNPC] ESC нажат - закрываем диалог");
                dialogWasInterrupted = true;
                EndDialog();
                return;
            }

            HandleMovement();
        }

        void HandleMovement()
        {
            // Проверяем, что агент активен и на NavMesh
            if (navAgent == null || !navAgent.isOnNavMesh) return;
            
            // Если остановлен из-за расстояния, не двигаемся
            if (isStoppedByDistance) return;
            
            // Если в очереди, идем к своей позиции в очереди
            if (isInQueue && queuePosition >= 0)
            {
                // Вычисляем позицию в очереди
                Vector3 queueStart = targetPosition.position;
                Vector3 queueDirection = targetPosition.forward; // Направление очереди
                
                // Fallback если forward равен нулю
                if (queueDirection.magnitude < 0.1f)
                {
                    queueDirection = Vector3.forward; // По умолчанию вперед
                    Debug.Log($"[BlackNPC] Target point не имеет forward, использую Vector3.forward");
                }
                
                float queueSpacing = 2f; // Расстояние между NPCs в очереди
                
                targetQueuePosition = queueStart + (queueDirection * queueSpacing * queuePosition);
                
                // Идем к позиции в очереди
                if (Vector3.Distance(transform.position, targetQueuePosition) > 0.3f)
                {
                    navAgent.SetDestination(targetQueuePosition);
                    Debug.Log($"[BlackNPC] Иду к позиции в очереди {queuePosition}: {targetQueuePosition}");
                }
                else
                {
                    // Поворачиваемся к стойке
                    Vector3 direction = (targetPosition.position - transform.position).normalized;
                    if (direction != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
                    }
                    Debug.Log($"[BlackNPC] Встал на позицию в очереди {queuePosition}");
                }
                return;
            }
            
            // Этап: доходим до триггера двери → пауза 1 сек → выставляем цель на стойку
            if (doorTrigger != null && !doorEntryCompleted)
            {
                if (Vector3.Distance(transform.position, doorTrigger.position) <= Mathf.Max(navAgent.stoppingDistance, 0.6f))
                {
                    doorEntryCompleted = true; // сохраняем ссылку для выхода
                    StartCoroutine(WaitAndGoToTarget(1f));
                    return;
                }
            }

            // Движение: приоритет — doorTrigger, затем целевая стойка
            if (doorTrigger != null && !doorEntryCompleted)
            {
                navAgent.SetDestination(doorTrigger.position);
                return;
            }

            // Проверяем, достигли ли цели
            if (navAgent.remainingDistance <= navAgent.stoppingDistance)
            {
                // Встаем в очередь
                if (!isInQueue && targetPosition != null)
                {
                    isInQueue = true;
                    Debug.Log($"[BlackNPC] Достиг target point, встаю в очередь на позицию {queuePosition}");
                }
                
                // Поворачиваемся к цели
                if (targetPosition != null)
                {
                    Vector3 direction = (targetPosition.position - transform.position).normalized;
                    if (direction != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
                    }
                }
            }
        }

        System.Collections.IEnumerator CheckDistanceToOtherNPCs()
        {
            while (npcState != NPCState.Finished)
            {
                if (npcState == NPCState.Idle && navAgent != null)
                {
                    bool shouldStop = false;
                    
                    // Находим всех черных NPCs в сцене
                    BlackNPC[] allBlackNPCs = FindObjectsOfType<BlackNPC>();
                    
                    foreach (BlackNPC otherNPC in allBlackNPCs)
                    {
                        if (otherNPC != null && otherNPC != this && 
                            otherNPC.GetCurrentState() != NPCState.Finished)
                        {
                            float distance = Vector3.Distance(transform.position, otherNPC.transform.position);
                            if (distance < minDistanceToOtherNPCs)
                            {
                                shouldStop = true;
                                break;
                            }
                        }
                    }
                    
                    // Проверяем также белых NPCs
                    WhiteNPC[] allWhiteNPCs = FindObjectsOfType<WhiteNPC>();
                    foreach (WhiteNPC otherNPC in allWhiteNPCs)
                    {
                        if (otherNPC != null && otherNPC.GetCurrentState() != WhiteNPC.NPCState.Finished)
                        {
                            float distance = Vector3.Distance(transform.position, otherNPC.transform.position);
                            if (distance < minDistanceToOtherNPCs)
                            {
                                shouldStop = true;
                                break;
                            }
                        }
                    }
                    
                    // Останавливаем или возобновляем движение
                    if (shouldStop && !isStoppedByDistance)
                    {
                        navAgent.isStopped = true;
                        isStoppedByDistance = true;
                        Debug.Log($"[BlackNPC] Остановлен из-за близости к другому NPC");
                    }
                    else if (!shouldStop && isStoppedByDistance)
                    {
                        navAgent.isStopped = false;
                        isStoppedByDistance = false;
                        Debug.Log($"[BlackNPC] Возобновлено движение");
                    }
                }
                
                yield return new WaitForSeconds(0.5f);
            }
        }

        void SetupDialogUI()
        {
            // Настройка кнопок мэйн панели
            if (tellMoreButton) tellMoreButton.onClick.AddListener(OnTellMoreButton);
            if (cannotHelpButton) cannotHelpButton.onClick.AddListener(OnCannotHelpButton);
            
            // Настройка кнопок панели подробного описания
            if (searchWarehouseButton) searchWarehouseButton.onClick.AddListener(OnSearchWarehouseButton);
            if (refuseClientButton) refuseClientButton.onClick.AddListener(OnRefuseClientButton);
            
            // Настройка кнопок панели вопроса
            if (foundItemButton) foundItemButton.onClick.AddListener(OnFoundItemButton);
            if (notFoundItemButton) notFoundItemButton.onClick.AddListener(OnNotFoundItemButton);
            
            // Настройка кнопки панели отказа
            if (closeRefuseButton) closeRefuseButton.onClick.AddListener(OnCloseRefuseButton);
            
            CloseAllPanels();
        }

        public void StartDialog()
        {
            if (npcState != NPCState.Idle && !dialogWasInterrupted) return;
            
            isInDialog = true;
            npcState = NPCState.InDialog;
            
            // Блокируем игрока
            if (PlayerController.Instance != null)
                PlayerController.Instance.Block();
            
            // Скрываем прицел
            if (CrosshairSystem.Instance != null)
                CrosshairSystem.Instance.HideCrosshairForDialog();
            
            // Останавливаем движение
            if (navAgent != null)
                navAgent.isStopped = true;
            
            // Показываем курсор
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            // Восстанавливаем состояние диалога или показываем нужную панель
            if (dialogWasInterrupted)
            {
                RestoreDialogState();
                Debug.Log("[BlackNPC] Диалог восстановлен с панели: " + currentPanelState);
            }
            else if (isSearchingForItem)
            {
                // Если NPC в состоянии поиска, показываем панель вопроса
                ShowQuestionPanel();
                Debug.Log("[BlackNPC] Диалог начат с панели вопроса о предмете");
            }
            else
            {
                ShowMainPanel();
                Debug.Log("[BlackNPC] Диалог начат с панели 1");
            }
            
            Debug.Log("[BlackNPC] Диалог начат");
        }

        public void TryOpenDialog()
        {
            if ((npcState == NPCState.Idle || dialogWasInterrupted) && !isInDialog)
            {
                StartDialog();
            }
        }

        void ShowMainPanel()
        {
            CloseAllPanels();
            currentPanelState = PanelState.MainPanel;
            
            if (mainPanel) mainPanel.SetActive(true);
            if (mainText)
            {
                string itemName;
                if (currentSearchingItem != null)
                {
                    // Используем реальный предмет из папки
                    itemName = currentSearchingItem.itemName;
                    Debug.Log($"[BlackNPC] Показываю диалог с реальным предметом: {itemName}");
                }
                else if (currentRandomItemName != null)
                {
                    // Используем сохраненное случайное имя
                    itemName = currentRandomItemName;
                    Debug.Log($"[BlackNPC] Показываю диалог со случайным предметом: {itemName}");
                }
                else
                {
                    // Используем случайный предмет
                    itemName = randomItemNames[Random.Range(0, randomItemNames.Length)];
                    Debug.Log($"[BlackNPC] Показываю диалог со случайным предметом: {itemName}");
                }
                
                string greeting = greetingPhrases[Random.Range(0, greetingPhrases.Length)];
                mainText.text = string.Format(greeting, itemName);
            }
        }

        void ShowDetailPanel()
        {
            CloseAllPanels();
            currentPanelState = PanelState.DetailPanel;
            
            if (detailPanel) detailPanel.SetActive(true);
            if (detailText)
            {
                string detailedDescription = GenerateDetailedDescription();
                detailText.text = detailedDescription;
                Debug.Log($"[BlackNPC] Показываю подробное описание: {detailedDescription}");
            }
        }

        void ShowQuestionPanel()
        {
            CloseAllPanels();
            currentPanelState = PanelState.QuestionPanel;
            
            if (questionPanel) questionPanel.SetActive(true);
            if (questionText)
            {
                string itemName = GetCurrentItemName();
                questionText.text = $"Вы нашли мой {itemName}?";
                Debug.Log($"[BlackNPC] Показываю панель вопроса о предмете: {itemName}");
            }
            // Управляем активностью кнопки в зависимости от инвентаря
            if (foundItemButton != null)
            {
                bool hasItem = PlayerInventory.Instance != null && PlayerInventory.Instance.HasItem();
                foundItemButton.interactable = hasItem;
            }
        }

        void ShowRefusePanel()
        {
            CloseAllPanels();
            currentPanelState = PanelState.RefusePanel;
            
            if (refusePanel) refusePanel.SetActive(true);
            Debug.Log("[BlackNPC] Показываю панель отказа");
        }

        void CloseAllPanels()
        {
            if (mainPanel) mainPanel.SetActive(false);
            if (detailPanel) detailPanel.SetActive(false);
            if (questionPanel) questionPanel.SetActive(false);
            if (refusePanel) refusePanel.SetActive(false);
        }

        // Обработчики кнопок мэйн панели
        void OnTellMoreButton()
        {
            Debug.Log("[BlackNPC] Кнопка 'Расскажите подробнее' нажата");
            ShowDetailPanel();
        }

        void OnCannotHelpButton()
        {
            Debug.Log("[BlackNPC] Кнопка 'Ничем не могу помочь' нажата");
            EndDialog();
        }

        // Обработчики кнопок панели подробного описания
        void OnSearchWarehouseButton()
        {
            Debug.Log("[BlackNPC] Кнопка 'Сейчас поищу на складе' нажата");
            isSearchingForItem = true;
            
            // Закрываем диалог, но НЕ завершаем NPC
            CloseAllPanels();
            isInDialog = false;
            
            // Разблокируем игрока
            if (PlayerController.Instance != null)
                PlayerController.Instance.Unblock();
            
            // Показываем прицел после диалога
            if (CrosshairSystem.Instance != null)
                CrosshairSystem.Instance.ShowCrosshairAfterDialog();
            
            // Скрываем курсор
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            
            // НЕ переходим в состояние Finished, остаемся в Idle
            npcState = NPCState.Idle;
            
            Debug.Log("[BlackNPC] NPC остался ждать, готов к повторному диалогу");
        }

        void OnRefuseClientButton()
        {
            Debug.Log("[BlackNPC] Кнопка 'Отказать клиенту' нажата");
            ShowRefusePanel();
        }

        // Обработчики кнопок панели вопроса
        void OnFoundItemButton()
        {
            Debug.Log("[BlackNPC] Кнопка 'Да, вот ваш предмет' нажата");
            // Здесь будет логика передачи предмета
            isSearchingForItem = false;
            EndDialog(); // Теперь правильно завершаем диалог
        }

        void OnNotFoundItemButton()
        {
            Debug.Log("[BlackNPC] Кнопка 'Нет, я не нашел вашего предмета' нажата");
            // Здесь будет логика отказа
            isSearchingForItem = false;
            ShowRefusePanel();
        }

        // Обработчик кнопки панели отказа
        void OnCloseRefuseButton()
        {
            Debug.Log("[BlackNPC] Кнопка 'Закрыть' нажата");
            EndDialog(); // Правильно завершаем диалог
        }

        void EndDialog()
        {
            CloseAllPanels();
            isInDialog = false;
            
            // Если диалог был прерван, не переходим в состояние Finished
            if (!dialogWasInterrupted)
            {
                npcState = NPCState.Finished;
            }
            else
            {
                // Если диалог был прерван, возвращаемся в Idle для возможности повторного открытия
                npcState = NPCState.Idle;
            }
            
            // Разблокируем игрока
            if (PlayerController.Instance != null)
                PlayerController.Instance.Unblock();
            
            // Показываем прицел после диалога
            if (CrosshairSystem.Instance != null)
                CrosshairSystem.Instance.ShowCrosshairAfterDialog();
            
            // Скрываем курсор
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            
            // Если диалог не был прерван, уходим через дверь к spawn point
            if (!dialogWasInterrupted)
            {
                if (navAgent != null)
                {
                    navAgent.isStopped = false;
                    StartCoroutine(ExitThroughDoorAndDespawn());
                }
                Debug.Log("[BlackNPC] Диалог завершен, уходим через дверь к spawn point");
            }
            else
            {
                // Если диалог был прерван, сбрасываем флаг и остаемся на месте
                dialogWasInterrupted = false;
                Debug.Log("[BlackNPC] Диалог прерван, остаемся на месте");
            }
        }

        void RestoreDialogState()
        {
            switch (currentPanelState)
            {
                case PanelState.MainPanel:
                    ShowMainPanel();
                    break;
                case PanelState.DetailPanel:
                    ShowDetailPanel();
                    break;
                case PanelState.QuestionPanel:
                    ShowQuestionPanel();
                    break;
                case PanelState.RefusePanel:
                    ShowRefusePanel();
                    break;
                default:
                    ShowMainPanel();
                    break;
            }
        }

        public void SetWantedItemData(Item itemData)
        {
            wantedItemData = itemData;
        }

        public bool IsInDialog()
        {
            return isInDialog;
        }

        System.Collections.IEnumerator GoToSpawnPoint()
        {
            // Ждем пока агент будет на NavMesh
            while (navAgent != null && !navAgent.isOnNavMesh)
            {
                yield return null;
            }
            
            // Ждем прокладки пути
            while (navAgent != null && navAgent.isOnNavMesh && navAgent.pathPending)
            {
                yield return null;
            }

            // Ждем прибытия: используем remainingDistance и допускаем остановку на stoppingDistance
            while (navAgent != null && navAgent.isOnNavMesh)
            {
                if (!navAgent.pathPending)
                {
                    float remain = navAgent.remainingDistance;
                    if (remain <= Mathf.Max(navAgent.stoppingDistance, despawnThreshold) + 0.05f)
                        break;
                }
                yield return null;
            }
            
            Debug.Log("[BlackNPC] Достиг exit point, пропадаем");
            Destroy(gameObject);
        }

        System.Collections.IEnumerator ExitThroughDoorAndDespawn()
        {
            // 1) Door trigger
            if (doorTrigger != null && navAgent != null)
            {
                navAgent.ResetPath();
                navAgent.SetDestination(doorTrigger.position);
                while (navAgent.pathPending) yield return null;
                while (navAgent.isOnNavMesh && navAgent.remainingDistance > Mathf.Max(navAgent.stoppingDistance, 0.6f))
                    yield return null;
                navAgent.isStopped = true; yield return new WaitForSeconds(0.5f); navAgent.isStopped = false;
                doorEntryCompleted = true; // исключаем возврат к триггеру на выходе
            }

            // 2) Exit point
            if (spawnPoint != null && navAgent != null)
            {
                navAgent.ResetPath();
                navAgent.SetDestination(spawnPoint.position);
                StartCoroutine(GoToSpawnPoint());
                yield break;
            }

            Destroy(gameObject);
        }

        System.Collections.IEnumerator WaitAndGoToTarget(float delay)
        {
            if (navAgent != null)
            {
                navAgent.isStopped = true;
            }
            yield return new WaitForSeconds(delay);
            if (navAgent != null)
            {
                navAgent.isStopped = false;
                if (targetPosition != null)
                {
                    navAgent.SetDestination(targetPosition.position);
                }
            }
        }

        public NPCState GetCurrentState()
        {
            return npcState;
        }
    }
}