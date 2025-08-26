using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace LostAndFound.Systems
{
    /// <summary>
    /// КОМПЬЮТЕРНАЯ СИСТЕМА - Базовая структура
    /// Взаимодействие: E при наведении прицела
    /// </summary>
    public class ComputerSystem : MonoBehaviour
    {
        [Header("Настройки системы")]
        public float interactionRange = 2f;

        [Header("UI Элементы")]
        public Canvas computerCanvas; // Canvas компьютера
        public GameObject desktopScreen; // Основной экран (DesktopScreen)
        public GameObject mainPanel; // Главная панель с обоями
        public Button databaseButton; // Кнопка "База Данных"
        public Button casinoButton; // Кнопка "Казино"
        public Button blackMarketButton; // Кнопка "BlackMarket"

        [Header("НОВАЯ БАЗА ДАННЫХ WB-СТИЛЬ")]
        public GameObject databasePanel; // Главная панель базы данных
        public GameObject itemCardPrefab; // Префаб карточки предмета (ты создашь)
        public RectTransform cardsContainer; // Контейнер для карточек (если не используешь 2 ряда)
        public RectTransform databaseRowTopContainer; // Верхний ряд (1920x430)
        public RectTransform databaseRowBottomContainer; // Нижний ряд (1920x430)
        
        [Header("ЧЕРНЫЙ РЫНОК")]
        public GameObject blackMarketPanel; // Панель черного рынка
        public GameObject blackMarketItemCardPrefab; // Префаб карточки (если null, используем itemCardPrefab)
        public RectTransform blackMarketCardsContainer; // Контейнер карточек черного рынка (если не используешь 2 ряда)
        public RectTransform blackMarketRowTopContainer; // Верхний ряд Черного рынка
        public RectTransform blackMarketRowBottomContainer; // Нижний ряд Черного рынка

        [Header("ЧР: Кнопки действий")] 
        public Button blackMarketListButton; // Кнопка "Выставить с инвентаря"
        
        [Header("Навигация Черного рынка")]
        public Button blackMarketPrevPageButton; // Назад
        public Button blackMarketNextPageButton; // Вперед
        public TextMeshProUGUI blackMarketPageInfoText; // Текущая страница
        public Button blackMarketCloseButton; // Закрыть черный рынок

        [Header("Навигация страниц")]
        public Button prevPageButton; // Кнопка "Назад"
        public Button nextPageButton; // Кнопка "Вперед"
        public TMPro.TextMeshProUGUI pageInfoText; // Текст "Страница X из Y"
        public Button closeDatabaseButton; // Кнопка "Закрыть" базу данных

        [Header("КАЗИНО СИСТЕМА")]
        public GameObject casinoPanel; // Главная панель казино
        public Button blackjackButton; // Кнопка "Блэк Джек"
        public Button rouletteButton; // Кнопка "Рулетка"
        public Button closeCasinoButton; // Кнопка "Закрыть" казино

        [Header("РУЛЕТКА СИСТЕМА")]
        public GameObject roulettePanel; // Панель рулетки
        public RouletteWheel rouletteWheel; // Компонент колеса (отдельный скрипт)
        public TMPro.TextMeshProUGUI balanceText; // Текст баланса "BALANCE: 10000₽"
        public TMPro.TMP_InputField betInputField; // Поле ввода ставки "BET:"
        public Button spinButton; // Кнопка "Крутить"
        public Button closeRouletteButton; // Кнопка закрыть рулетку

        [Header("BLACKJACK СИСТЕМА")]
        public GameObject blackjackPanel; // Панель блэк джека
        public TMPro.TextMeshProUGUI blackjackBalanceText; // Текст баланса в блэк джеке
        public TMPro.TMP_InputField blackjackBetInputField; // Поле ввода ставки в блэк джеке
        public TMPro.TextMeshProUGUI dealerSumText; // Текст суммы крупье
        public TMPro.TextMeshProUGUI playerSumText; // Текст суммы игрока
        public Button hitButton; // Кнопка "ВЗЯТЬ"
        public Button playButton; // Кнопка "ИГРАТЬ"
        public Button standButton; // Кнопка "ХВАТИТ"
        public Button closeBlackjackButton; // Кнопка закрыть блэк джек

        [Header("BLACKJACK КАРТЫ")]
        public GameObject playerCardsContainer; // Контейнер для карт игрока
        public GameObject dealerCardsContainer; // Контейнер для карт крупье
        public GameObject cardPrefab; // Префаб карты (UI Image)

        [Header("Кнопки чисел (0-36)")]
        public Button[] numberButtons = new Button[37]; // Кнопки чисел 0-36

        [Header("Кнопки ставок")]
        public Button[] column2to1Buttons = new Button[3]; // 3 кнопки "2to1" (столбцы)
        public Button[] dozen1st12Buttons = new Button[3]; // 1st12, 2nd12, 3rd12 (секторы)
        public Button button1to18; // 1-18
        public Button buttonEven; // EVEN (четные)
        public Button buttonRed; // Красное
        public Button buttonBlack; // Черное  
        public Button buttonOdd; // ODD (нечетные)
        public Button button19to36; // 19-36



        // ДАННЫЕ БАЗЫ
        [System.Serializable]
        public class DatabaseItem
        {
            public string id;
            public string name;
            public string description;
            public string date;
            public GameObject original3DObject; // Ссылка на оригинальный 3D предмет
            
            public DatabaseItem(string id, string name, string description, string date, GameObject obj)
            {
                this.id = id;
                this.name = name;
                this.description = description;
                this.date = date;
                this.original3DObject = obj;
            }
        }

        // Система пагинации
        private List<DatabaseItem> allItems = new List<DatabaseItem>();
        private List<GameObject> currentPageCards = new List<GameObject>();
        private int currentPage = 1;
        private int itemsPerPage = 12; // 6+6 в двух рядах
        private int totalPages = 1;

        // Черный рынок - своя пагинация и набор предметов (только с тегом contraband)
        private List<DatabaseItem> blackMarketItems = new List<DatabaseItem>();
        // Список лотов, выставленных игроком из инвентаря
        private List<DatabaseItem> blackMarketListedItems = new List<DatabaseItem>();
        private List<GameObject> blackMarketCurrentPageCards = new List<GameObject>();
        private int blackMarketCurrentPage = 1;
        private int blackMarketItemsPerPage = 12;
        private int blackMarketTotalPages = 1;

        // РУЛЕТКА - Игровые переменные
        // Баланс перенесен в MoneyManager
        private int currentBet = 100; // Текущая ставка
        private bool isSpinning = false; // Крутится ли колесо
        private List<RouletteWager> currentBets = new List<RouletteWager>(); // Активные ставки
        private const int MAX_BETS = 3; // Максимум 3 ставки как в реальном казино
        
        // BLACKJACK - Игровые переменные
        private int blackjackBalance = 10000; // Локальный fallback-баланс для блэк джека (если MoneyManager отсутствует)
        private int blackjackCurrentBet = 100; // Текущая ставка в блэк джеке
        private List<int> playerCards = new List<int>(); // Карты игрока (значения)
        private List<int> dealerCards = new List<int>(); // Карты крупье (значения)
        // Фиксированный выбор масти/спрайта для каждой выданной карты, чтобы не "прыгали" цвета
        private List<string> playerCardNames = new List<string>(); // имена спрайтов карт игрока
        private List<string> dealerCardNames = new List<string>(); // имена спрайтов карт крупье
        private bool gameInProgress = false; // Идет ли игра
        private bool dealerTurn = false; // Ход крупье
        private bool gameEnded = false; // Игра закончена

        // BLACKJACK - UI карты
        private List<GameObject> playerCardObjects = new List<GameObject>(); // UI объекты карт игрока
        private List<GameObject> dealerCardObjects = new List<GameObject>(); // UI объекты карт крупье

        // BLACKJACK - Анимации
        private bool isDealingCards = false; // Идет ли раздача карт
        private float cardDealDelay = 0.1f; // Быстрая задержка между картами
        private float gameEndDelay = 0.5f; // Быстрая задержка перед завершением игры

        // Массивы чисел для разных типов ставок
        private int[] redNumbers = { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };
        private int[] blackNumbers = { 2, 4, 6, 8, 10, 11, 13, 15, 17, 20, 22, 24, 26, 28, 29, 31, 33, 35 };

        // Структура для ставок
        [System.Serializable]
        public class RouletteWager
        {
            public string betType; // "number", "red", "black", "even", "odd", "1to18", "19to36", "1st12", "2nd12", "3rd12", "column1", "column2", "column3"
            public int betValue; // Для чисел - номер числа, для остальных -1
            public int betAmount; // Сумма ставки
            public float multiplier; // Множитель выигрыша
        }

        // Singleton
        public static ComputerSystem Instance;

        // Основные переменные
        private bool isOpen = false;

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
            // Создаем коллайдер для взаимодействия если его нет
            Collider systemCollider = GetComponent<Collider>();
            if (systemCollider == null)
            {
                BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(1f, 1f, 1f);
                boxCollider.isTrigger = false;
                Debug.Log("[ComputerSystem] Создан коллайдер для ComputerSystem GameObject");
            }

            // Скрываем UI при старте
            if (computerCanvas != null)
                computerCanvas.gameObject.SetActive(false);
            
            if (databasePanel != null)
                databasePanel.SetActive(false);

            if (casinoPanel != null)
            {
                casinoPanel.SetActive(false);
                Debug.Log("[ComputerSystem] Панель казино скрыта при старте");
            }

            if (roulettePanel != null)
            {
                roulettePanel.SetActive(false);
                Debug.Log("[ComputerSystem] Панель рулетки скрыта при старте");
            }
            else
            {
                Debug.LogWarning("[ComputerSystem] RoulettePanel не назначен при старте!");
            }

            if (blackjackPanel != null)
            {
                blackjackPanel.SetActive(false);
                Debug.Log("[ComputerSystem] Панель блэк джека скрыта при старте");
            }
            else
            {
                Debug.LogWarning("[ComputerSystem] BlackjackPanel не назначен при старте!");
            }

            // Черный рынок скрыт при старте
            if (blackMarketPanel != null)
            {
                blackMarketPanel.SetActive(false);
                Debug.Log("[ComputerSystem] Панель Черного рынка скрыта при старте");
            }
            
            // Подписываемся на события склада
            if (WarehouseSystem.Instance != null)
            {
                WarehouseSystem.Instance.OnItemStored += OnItemAddedToWarehouse;
                Debug.Log("[ComputerSystem] Подписка на события склада установлена");
            }
            
            // Настраиваем навигацию
            SetupNavigation();
            

        }

        void Update()
        {
            HandleInput();
        }

        void HandleInput()
        {
            // Проверяем наведение прицела на компьютер
            bool isAimingAtComputer = false;
            if (CrosshairSystem.Instance != null)
            {
                GameObject currentTarget = CrosshairSystem.Instance.GetCurrentTarget();
                isAimingAtComputer = (currentTarget == gameObject);
            }

            // Альтернативная проверка по дистанции
            if (!isAimingAtComputer && PlayerController.Instance != null)
            {
                float distance = Vector3.Distance(PlayerController.Instance.transform.position, transform.position);
                isAimingAtComputer = (distance <= interactionRange);
            }

            // Открываем компьютер по E при наведении прицела
            if (Input.GetKeyDown(KeyCode.E) && isAimingAtComputer && !isOpen)
            {
                OpenComputer();
            }
            
            // Закрываем по ESC
            if (Input.GetKeyDown(KeyCode.Escape) && isOpen)
            {
                // Если открыта база данных - закрываем её и возвращаемся в главное меню
                if (databasePanel != null && databasePanel.activeInHierarchy)
                {
                    HideDatabase();
                }
                // Если открыт Черный рынок - закрываем его и возвращаемся в главное меню
                else if (blackMarketPanel != null && blackMarketPanel.activeInHierarchy)
                {
                    HideBlackMarket();
                }
                                // Если открыто казино - закрываем его и возвращаемся в главное меню
                else if (casinoPanel != null && casinoPanel.activeInHierarchy)
                {
                    HideCasino();
                }
                // Если открыта рулетка - закрываем её и возвращаемся в казино
                else if (roulettePanel != null && roulettePanel.activeInHierarchy)
                {
                    HideRoulette();
                }
                // Если открыт блэк джек - закрываем его и возвращаемся в казино
                else if (blackjackPanel != null && blackjackPanel.activeInHierarchy)
                {
                    HideBlackjack();
                }
                else
                {
                    // Иначе закрываем весь компьютер
                CloseComputer();
                }
            }
        }

        public void OpenComputer()
        {
            if (isOpen) return;
            
            Debug.Log("[ComputerSystem] === ОТКРЫТИЕ КОМПЬЮТЕРА ===");
            isOpen = true;

            // Активируем Canvas
            if (computerCanvas != null)
            {
                computerCanvas.gameObject.SetActive(true);
                Debug.Log("[ComputerSystem] Canvas активирован");
            }

            // Скрываем панель денег во время работы за компьютером
            if (MoneyManager.Instance != null && MoneyManager.Instance.moneyPanel != null)
            {
                MoneyManager.Instance.moneyPanel.SetActive(false);
            }

            // Активируем DesktopScreen
            if (desktopScreen != null)
            {
                desktopScreen.SetActive(true);
                Debug.Log("[ComputerSystem] DesktopScreen активирован");
            }

            // Активируем MainPanel (обои) - только главное меню
            if (mainPanel != null)
            {
                mainPanel.SetActive(true);
                Debug.Log("[ComputerSystem] MainPanel активирован");
            }

            // СКРЫВАЕМ ПРИЦЕЛ И ИНВЕНТАРЬ
            if (CrosshairSystem.Instance != null)
            {
                CrosshairSystem.Instance.HideCrosshair();
                Debug.Log("[ComputerSystem] Прицел скрыт");
            }

            // Скрываем инвентарь игрока
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.HideInventoryUI();
                Debug.Log("[ComputerSystem] Инвентарь игрока скрыт");
            }
            
            // Блокируем управление игроком
            if (PlayerController.Instance)
                PlayerController.Instance.Block();
                
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            Debug.Log("[ComputerSystem] Компьютер открыт");
        }

        public void CloseComputer()
        {
            if (!isOpen) return;
            
            Debug.Log("[ComputerSystem] === ЗАКРЫТИЕ КОМПЬЮТЕРА ===");
            isOpen = false;
            
            // СКРЫВАЕМ ВСЕ ПАНЕЛИ КОМПЬЮТЕРА
            if (computerCanvas != null)
                computerCanvas.gameObject.SetActive(false);
            
            // Возвращаем панель денег на экран
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.UpdateUI();
                if (MoneyManager.Instance.moneyPanel != null)
                    MoneyManager.Instance.moneyPanel.SetActive(true);
            }
            if (mainPanel != null)
                mainPanel.SetActive(false);

            if (databasePanel != null)
                databasePanel.SetActive(false);

            if (casinoPanel != null)
                casinoPanel.SetActive(false);

            if (roulettePanel != null)
                roulettePanel.SetActive(false);

            if (blackjackPanel != null)
                blackjackPanel.SetActive(false);
            
            // Разблокируем управление игроком
            if (PlayerController.Instance)
                PlayerController.Instance.Unblock();
                
            // ПОКАЗЫВАЕМ ПРИЦЕЛ И ИНВЕНТАРЬ
            if (CrosshairSystem.Instance != null)
                CrosshairSystem.Instance.ShowCrosshair();

            // Показываем инвентарь игрока
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.ShowInventoryUI();
                Debug.Log("[ComputerSystem] Инвентарь игрока показан");
            }
                
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            Debug.Log("[ComputerSystem] Компьютер закрыт, все панели скрыты");
        }

        /// <summary>
        /// Показывает базу данных WB-стиль
        /// </summary>
        public void ShowDatabase()
        {
            Debug.Log("[ComputerSystem] === ПОКАЗ БАЗЫ ДАННЫХ WB-СТИЛЬ ===");
            
            // Скрываем главное меню
            if (mainPanel != null)
                mainPanel.SetActive(false);
            
            if (desktopScreen != null)
                desktopScreen.SetActive(false);
            
            // Показываем базу данных
            if (databasePanel != null)
                databasePanel.SetActive(true);
            
            // Загружаем данные из склада
            LoadWarehouseData();
            
            // Показываем первую страницу
            ShowPage(1);
            
            Debug.Log("[ComputerSystem] База данных WB-стиль показана");
        }

        /// <summary>
        /// Показывает Черный рынок (только предметы с тегом contraband)
        /// </summary>
        public void ShowBlackMarket()
        {
            Debug.Log("[ComputerSystem] === ПОКАЗ ЧЕРНОГО РЫНКА ===");

            // Скрываем главное меню
            if (mainPanel != null)
                mainPanel.SetActive(false);

            if (desktopScreen != null)
                desktopScreen.SetActive(false);

            // Показываем панель Черного рынка
            if (blackMarketPanel != null)
                blackMarketPanel.SetActive(true);

            // Загружаем данные для Черного рынка
            LoadBlackMarketData();

            // Показываем первую страницу
            ShowBlackMarketPage(1);

            Debug.Log("[ComputerSystem] Черный рынок показан");
        }

        /// <summary>
        /// Скрывает Черный рынок
        /// </summary>
        public void HideBlackMarket()
        {
            Debug.Log("[ComputerSystem] === СКРЫТИЕ ЧЕРНОГО РЫНКА ===");

            // Очищаем карточки
            ClearBlackMarketCurrentPageCards();

            // Скрываем панель
            if (blackMarketPanel != null)
                blackMarketPanel.SetActive(false);

            // Возвращаемся на рабочий стол
            if (desktopScreen != null)
                desktopScreen.SetActive(true);

            if (mainPanel != null)
                mainPanel.SetActive(true);

            Debug.Log("[ComputerSystem] Черный рынок скрыт");
        }

        /// <summary>
        /// Скрывает базу данных
        /// </summary>
        public void HideDatabase()
        {
            Debug.Log("[ComputerSystem] === СКРЫТИЕ БАЗЫ ДАННЫХ ===");
            
            // Очищаем карточки
            ClearCurrentPageCards();
            
            // Скрываем базу данных
            if (databasePanel != null)
                databasePanel.SetActive(false);
            
            // Показываем главное меню
            if (desktopScreen != null)
                desktopScreen.SetActive(true);
            
            if (mainPanel != null)
                mainPanel.SetActive(true);
            
            Debug.Log("[ComputerSystem] База данных скрыта");
        }

        /// <summary>
        /// Показывает казино меню
        /// </summary>
        public void ShowCasino()
        {
            Debug.Log("[ComputerSystem] === ПОКАЗ КАЗИНО МЕНЮ ===");

            // Скрываем главное меню
            if (mainPanel != null)
                mainPanel.SetActive(false);

            if (desktopScreen != null)
                desktopScreen.SetActive(false);

            // Показываем казино панель
            if (casinoPanel != null)
                casinoPanel.SetActive(true);

            Debug.Log("[ComputerSystem] Казино меню показано");
        }

        /// <summary>
        /// Скрывает казино меню
        /// </summary>
        public void HideCasino()
        {
            Debug.Log("[ComputerSystem] === СКРЫТИЕ КАЗИНО МЕНЮ ===");

            // Скрываем казино панель
            if (casinoPanel != null)
                casinoPanel.SetActive(false);

            // Показываем главное меню
            if (desktopScreen != null)
                desktopScreen.SetActive(true);

            if (mainPanel != null)
                mainPanel.SetActive(true);

            Debug.Log("[ComputerSystem] Казино меню скрыто");
        }

        /// <summary>
        /// Показывает рулетку
        /// </summary>
        public void ShowRoulette()
        {
            Debug.Log("[ComputerSystem] === ПОКАЗ РУЛЕТКИ ===");

            // Скрываем казино меню
            if (casinoPanel != null)
            {
                casinoPanel.SetActive(false);
                Debug.Log("[ComputerSystem] Казино панель скрыта");
            }

            // Показываем рулетку
            if (roulettePanel != null)
            {
                roulettePanel.SetActive(true);
                Debug.Log("[ComputerSystem] Панель рулетки активирована!");
            }
            else
            {
                Debug.LogError("[ComputerSystem] RoulettePanel НЕ НАЗНАЧЕН! Назначь панель рулетки в Inspector!");
                return;
            }

            // Обновляем UI (безопасно)
            try
            {
                UpdateRouletteUI();
                Debug.Log("[ComputerSystem] UI рулетки обновлен");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ComputerSystem] Ошибка при обновлении UI рулетки: {e.Message}");
            }

            Debug.Log("[ComputerSystem] Рулетка показана успешно!");
        }

        /// <summary>
        /// Скрывает рулетку
        /// </summary>
        public void HideRoulette()
        {
            Debug.Log("[ComputerSystem] === СКРЫТИЕ РУЛЕТКИ ===");

            // Сбрасываем ставки
            ClearAllBets();

            // Скрываем рулетку
            if (roulettePanel != null)
                roulettePanel.SetActive(false);

            // Показываем казино меню
            if (casinoPanel != null)
                casinoPanel.SetActive(true);

            Debug.Log("[ComputerSystem] Рулетка скрыта");
        }

        /// <summary>
        /// Обновляет UI рулетки
        /// </summary>
        void UpdateRouletteUI()
        {
            // Обновляем баланс с информацией о ставках
            if (balanceText != null)
            {
                int bal = MoneyManager.Instance != null ? MoneyManager.Instance.GetBalance() : 0;
                balanceText.text = $"BALANCE: {FormatCurrency(bal)} | СТАВКИ: {currentBets.Count}/{MAX_BETS}";
            }

            // НЕ СБРАСЫВАЕМ СТАВКУ - оставляем то что ввел игрок
            // if (betInputField != null)
            //     betInputField.text = currentBet.ToString();
        }

        /// <summary>
        /// Очищает все ставки
        /// </summary>
        void ClearAllBets()
        {
            currentBets.Clear();
            Debug.Log("[ComputerSystem] Все ставки очищены");
        }

        /// <summary>
        /// Делает ставку на число (0-36)
        /// </summary>
        public void BetOnNumber(int number)
        {
            if (isSpinning) return;
            
            int betAmount = GetCurrentBetAmount();
            if (!CanPlaceBet(betAmount)) return;

            RouletteWager wager = new RouletteWager
            {
                betType = "number",
                betValue = number,
                betAmount = betAmount,
                multiplier = 36f // Прямая ставка x36
            };

            currentBets.Add(wager);
            if (MoneyManager.Instance != null) MoneyManager.Instance.Spend(betAmount);
            UpdateRouletteUI();

            Debug.Log($"[ComputerSystem] Ставка {betAmount}₽ на число {number}");
        }

        /// <summary>
        /// Делает ставку на красное
        /// </summary>
        public void BetOnRed()
        {
            PlaceColorBet("red", 2f);
        }

        /// <summary>
        /// Делает ставку на черное
        /// </summary>
        public void BetOnBlack()
        {
            PlaceColorBet("black", 2f);
        }

        /// <summary>
        /// Делает ставку на четные
        /// </summary>
        public void BetOnEven()
        {
            PlaceSpecialBet("even", 2f);
        }

        /// <summary>
        /// Делает ставку на нечетные
        /// </summary>
        public void BetOnOdd()
        {
            PlaceSpecialBet("odd", 2f);
        }

        /// <summary>
        /// Делает ставку на 1-18
        /// </summary>
        public void BetOn1to18()
        {
            PlaceSpecialBet("1to18", 2f);
        }

        /// <summary>
        /// Делает ставку на 19-36
        /// </summary>
        public void BetOn19to36()
        {
            PlaceSpecialBet("19to36", 2f);
        }

        /// <summary>
        /// Делает ставку на дюжину (1st12, 2nd12, 3rd12)
        /// </summary>
        public void BetOnDozen(int dozenIndex)
        {
            if (isSpinning) return;
            
            string[] dozenTypes = { "1st12", "2nd12", "3rd12" };
            if (dozenIndex < 0 || dozenIndex >= dozenTypes.Length) return;

            PlaceSpecialBet(dozenTypes[dozenIndex], 3f);
        }

        /// <summary>
        /// Делает ставку на столбец (2to1)
        /// </summary>
        public void BetOnColumn(int columnIndex)
        {
            if (isSpinning) return;
            
            string[] columnTypes = { "column1", "column2", "column3" };
            if (columnIndex < 0 || columnIndex >= columnTypes.Length) return;

            PlaceSpecialBet(columnTypes[columnIndex], 3f);
        }

        /// <summary>
        /// Помощник для ставок на цвета
        /// </summary>
        void PlaceColorBet(string color, float multiplier)
        {
            if (isSpinning) return;
            
            int betAmount = GetCurrentBetAmount();
            if (!CanPlaceBet(betAmount)) return;

            RouletteWager wager = new RouletteWager
            {
                betType = color,
                betValue = -1,
                betAmount = betAmount,
                multiplier = multiplier
            };

            currentBets.Add(wager);
            if (MoneyManager.Instance != null) MoneyManager.Instance.Spend(betAmount);
            UpdateRouletteUI();

            Debug.Log($"[ComputerSystem] Ставка {betAmount}₽ на {color}");
        }

        /// <summary>
        /// Помощник для специальных ставок
        /// </summary>
        void PlaceSpecialBet(string betType, float multiplier)
        {
            if (isSpinning) return;
            
            int betAmount = GetCurrentBetAmount();
            if (!CanPlaceBet(betAmount)) return;

            RouletteWager wager = new RouletteWager
            {
                betType = betType,
                betValue = -1,
                betAmount = betAmount,
                multiplier = multiplier
            };

            currentBets.Add(wager);
            if (MoneyManager.Instance != null) MoneyManager.Instance.Spend(betAmount);
            UpdateRouletteUI();

            Debug.Log($"[ComputerSystem] Ставка {betAmount}₽ на {betType}");
        }

        /// <summary>
        /// Получает сумму текущей ставки
        /// </summary>
        int GetCurrentBetAmount()
        {
            if (betInputField != null && int.TryParse(betInputField.text, out int amount))
            {
                return Mathf.Clamp(amount, 50, 5000); // Мин 50₽, макс 5000₽
            }
            return currentBet;
        }

        /// <summary>
        /// Проверяет можно ли сделать ставку (реальные правила казино)
        /// </summary>
        bool CanPlaceBet(int amount)
        {
            // Проверка денег
            int bal = MoneyManager.Instance != null ? MoneyManager.Instance.GetBalance() : 0;
            if (amount > bal)
            {
                Debug.LogWarning("[ComputerSystem] Недостаточно средств для ставки!");
                return false;
            }

            // 🎰 ЛИМИТ СТАВОК КАК В РЕАЛЬНОМ КАЗИНО - МАКСИМУМ 3 СТАВКИ!
            if (currentBets.Count >= MAX_BETS)
            {
                Debug.LogWarning($"[ComputerSystem] Максимум {MAX_BETS} ставки! Нажми 'Крутить' или очисти ставки.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Запускает вращение рулетки
        /// </summary>
        public void SpinRoulette()
        {
            if (isSpinning)
            {
                Debug.LogWarning("[ComputerSystem] Рулетка уже крутится!");
                return;
            }

            if (currentBets.Count == 0)
            {
                Debug.LogWarning("[ComputerSystem] Нет активных ставок!");
                return;
            }

            if (rouletteWheel != null)
            {
                isSpinning = true;
                Debug.Log("[ComputerSystem] Запуск рулетки...");
                
                rouletteWheel.SpinWheel(OnRouletteResult);
            }
        }

        /// <summary>
        /// Обработчик результата рулетки
        /// </summary>
        void OnRouletteResult(int resultNumber)
        {
            isSpinning = false;
            Debug.Log($"[ComputerSystem] Результат рулетки: {resultNumber}");

            // Подсчитываем выигрыши
            int totalWinnings = CalculateWinnings(resultNumber);
            
            if (totalWinnings > 0)
            {
                if (MoneyManager.Instance != null) MoneyManager.Instance.Add(totalWinnings);
                Debug.Log($"[ComputerSystem] Выигрыш: {totalWinnings}₽");
            }
            else
            {
                Debug.Log("[ComputerSystem] Проигрыш");
            }

            // Очищаем ставки
            ClearAllBets();
            UpdateRouletteUI();
        }

        /// <summary>
        /// Подсчитывает выигрыши
        /// </summary>
        int CalculateWinnings(int resultNumber)
        {
            int totalWinnings = 0;

            foreach (RouletteWager bet in currentBets)
            {
                bool isWin = false;

                switch (bet.betType)
                {
                    case "number":
                        isWin = (bet.betValue == resultNumber);
                    break;

                    case "red":
                        isWin = (resultNumber != 0 && System.Array.IndexOf(redNumbers, resultNumber) >= 0);
                    break;

                    case "black":
                        isWin = (resultNumber != 0 && System.Array.IndexOf(blackNumbers, resultNumber) >= 0);
                    break;

                    case "even":
                        isWin = (resultNumber != 0 && resultNumber % 2 == 0);
                        break;

                    case "odd":
                        isWin = (resultNumber != 0 && resultNumber % 2 == 1);
                        break;

                    case "1to18":
                        isWin = (resultNumber >= 1 && resultNumber <= 18);
                        break;

                    case "19to36":
                        isWin = (resultNumber >= 19 && resultNumber <= 36);
                        break;

                    case "1st12":
                        isWin = (resultNumber >= 1 && resultNumber <= 12);
                        break;

                    case "2nd12":
                        isWin = (resultNumber >= 13 && resultNumber <= 24);
                        break;

                    case "3rd12":
                        isWin = (resultNumber >= 25 && resultNumber <= 36);
                        break;

                    case "column1":
                        isWin = (resultNumber > 0 && (resultNumber - 1) % 3 == 0);
                        break;

                    case "column2":
                        isWin = (resultNumber > 0 && (resultNumber - 2) % 3 == 0);
                        break;

                    case "column3":
                        isWin = (resultNumber > 0 && (resultNumber - 3) % 3 == 0);
                    break;
            }

                if (isWin)
                {
                    int winAmount = Mathf.RoundToInt(bet.betAmount * bet.multiplier);
                    totalWinnings += winAmount;
                    Debug.Log($"[ComputerSystem] Выигрышная ставка: {bet.betType}, выигрыш: {winAmount}₽");
                }
            }

            return totalWinnings;
        }

        // ==================== BLACKJACK МЕТОДЫ ====================

        /// <summary>
        /// Показывает блэк джек
        /// </summary>
        public void ShowBlackjack()
        {
            Debug.Log("[ComputerSystem] === ПОКАЗ BLACKJACK ===");

            // Скрываем казино меню
            if (casinoPanel != null)
            {
                casinoPanel.SetActive(false);
                Debug.Log("[ComputerSystem] Казино панель скрыта");
            }

            // Показываем блэк джек
            if (blackjackPanel != null)
            {
                blackjackPanel.SetActive(true);
                Debug.Log("[ComputerSystem] Панель блэк джека активирована!");
            }
            else
            {
                Debug.LogError("[ComputerSystem] BlackjackPanel НЕ НАЗНАЧЕН! Назначь панель блэк джека в Inspector!");
                return;
            }

            // Создаем контейнеры для карт если их нет
            CreateCardContainers();

            // Проверяем BlackJackCardManager
            if (BlackJackCardManager.Instance == null)
            {
                Debug.LogError("[ComputerSystem] BlackJackCardManager не найден! Создаем автоматически...");
                GameObject cardManagerObject = new GameObject("BlackJackCardManager");
                cardManagerObject.AddComponent<BlackJackCardManager>();
            }
            else
            {
                Debug.Log("[ComputerSystem] BlackJackCardManager найден и готов к работе");
            }

            // Обновляем UI
            UpdateBlackjackUI();
            ResetGame();

            Debug.Log("[ComputerSystem] Блэк джек показан успешно!");
        }

        /// <summary>
        /// Скрывает блэк джек
        /// </summary>
        public void HideBlackjack()
        {
            Debug.Log("[ComputerSystem] === СКРЫТИЕ BLACKJACK ===");

            // Сбрасываем игру
            ResetGame();

            // Скрываем блэк джек
            if (blackjackPanel != null)
                blackjackPanel.SetActive(false);

            // Показываем казино меню
            if (casinoPanel != null)
                casinoPanel.SetActive(true);

            Debug.Log("[ComputerSystem] Блэк джек скрыт");
        }

        /// <summary>
        /// Обновляет UI блэк джека
        /// </summary>
        void UpdateBlackjackUI()
        {
            // Обновляем баланс
            if (blackjackBalanceText != null)
                blackjackBalanceText.text = $"BALANCE: {FormatCurrency(blackjackBalance)}";

            // Обновляем ставку
            if (blackjackBetInputField != null)
                blackjackBetInputField.text = blackjackCurrentBet.ToString();

            // Обновляем суммы карт
            if (dealerSumText != null)
            {
                if (gameInProgress && !gameEnded)
                {
                    // Показываем только первую карту крупье во время игры
                    if (dealerCards.Count > 0)
                    {
                        int dealerVisibleSum = CalculateCardValue(dealerCards[0]);
                        dealerSumText.text = dealerVisibleSum.ToString();
                        Debug.Log($"[ComputerSystem] Обновлен счет крупье (видимый): {dealerVisibleSum}");
                    }
                    else
                    {
                        dealerSumText.text = "0";
                        Debug.Log("[ComputerSystem] У крупье нет карт!");
                    }
                }
                else
                {
                    // Игра закончена или ход дилера - показываем полную сумму
                    int dealerTotal = CalculateHandValue(dealerCards);
                    dealerSumText.text = dealerTotal.ToString();
                    Debug.Log($"[ComputerSystem] Обновлен счет крупье (полный): {dealerTotal}");
                }
            }

            if (playerSumText != null)
            {
                int playerValue = CalculateHandValue(playerCards);
                playerSumText.text = playerValue.ToString();
                Debug.Log($"[ComputerSystem] Обновлен счет игрока: {playerValue}");
            }

            // Обновляем состояние кнопок
            UpdateBlackjackButtons();
            
            // Обновляем отображение карт
            UpdateCardDisplay();
        }

        /// <summary>
        /// Обновляет состояние кнопок блэк джека
        /// </summary>
        void UpdateBlackjackButtons()
        {
            if (hitButton != null)
            {
                bool canHit = gameInProgress && !dealerTurn && !gameEnded;
                hitButton.interactable = canHit;
                Debug.Log($"[ComputerSystem] Кнопка HIT: {canHit} (gameInProgress: {gameInProgress}, dealerTurn: {dealerTurn}, gameEnded: {gameEnded})");
            }

            if (standButton != null)
            {
                bool canStand = gameInProgress && !dealerTurn && !gameEnded;
                standButton.interactable = canStand;
                Debug.Log($"[ComputerSystem] Кнопка STAND: {canStand} (gameInProgress: {gameInProgress}, dealerTurn: {dealerTurn}, gameEnded: {gameEnded})");
            }

            if (playButton != null)
            {
                bool canPlay = !gameInProgress && !gameEnded;
                playButton.interactable = canPlay;
                Debug.Log($"[ComputerSystem] Кнопка PLAY: {canPlay} (gameInProgress: {gameInProgress}, gameEnded: {gameEnded})");
            }

            if (blackjackBetInputField != null)
                blackjackBetInputField.interactable = !gameInProgress;
        }

        /// <summary>
        /// Тянет карту
        /// </summary>
        int DrawCard()
        {
            // Простая колода: 1-10 (1 = туз, 2-10 = цифры, 11-13 = валет, дама, король = 10)
            int card = Random.Range(1, 14);
            if (card > 10) card = 10; // Валет, дама, король = 10
            return card;
        }

        /// <summary>
        /// Сбрасывает игру
        /// </summary>
        void ResetGame()
        {
            Debug.Log("[ComputerSystem] Сброс игры...");
            
            // Очищаем карты
            playerCards.Clear();
            dealerCards.Clear();
            
            // Сбрасываем состояние игры
            gameInProgress = false;
            dealerTurn = false;
            gameEnded = false;
            isDealingCards = false;
            
            // Очищаем UI карты
            ClearAllCardObjects();
            
            // Обновляем UI
            UpdateBlackjackUI();
            
            Debug.Log("[ComputerSystem] Игра сброшена");
        }

        /// <summary>
        /// Начинает новую игру
        /// </summary>
        public void StartBlackjackGame()
        {
            Debug.Log("[ComputerSystem] === НАЧАЛО НОВОЙ ИГРЫ ===");
            
            if (gameInProgress || isDealingCards) 
            {
                Debug.LogWarning("[ComputerSystem] Игра уже идет!");
                return;
            }

            int betAmount = GetBlackjackBetAmount();
            int available = MoneyManager.Instance != null ? MoneyManager.Instance.GetBalance() : blackjackBalance;
            if (betAmount > available)
            {
                Debug.LogWarning("[ComputerSystem] Недостаточно средств для ставки!");
                return;
            }

            if (betAmount <= 0)
            {
                Debug.LogWarning("[ComputerSystem] Ставка должна быть больше 0!");
                return;
            }

            // Снимаем ставку из общей системы денег
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.Spend(betAmount);
                blackjackBalance = MoneyManager.Instance.GetBalance();
            }
            else
            {
                blackjackBalance -= betAmount;
            }
            blackjackCurrentBet = betAmount;
            Debug.Log($"[ComputerSystem] Ставка принята: {betAmount}");

            // Начинаем раздачу карт
            StartCoroutine(DealCardsAnimation());
        }

        /// <summary>
        /// Раздача карт
        /// </summary>
        System.Collections.IEnumerator DealCardsAnimation()
        {
            Debug.Log("[ComputerSystem] === НАЧАЛО РАЗДАЧИ КАРТ ===");
            
            isDealingCards = true;
            gameInProgress = false; // станет true после раздачи, если не блэкджек
            dealerTurn = false;
            gameEnded = false;

            // Полный сброс колоды на новое раздавание
            ClearAllCardObjects();
            playerCards.Clear();
            dealerCards.Clear();
            playerCardNames.Clear();
            dealerCardNames.Clear();

            // Первая карта игроку
            yield return new WaitForSeconds(0.1f);
            int card1 = DrawCard();
            playerCards.Add(card1);
            playerCardNames.Add(GetCardName(card1));
            UpdateCardDisplay();
            UpdateBlackjackUI();
            Debug.Log($"[ComputerSystem] Игроку раздана карта: {card1}");

            // Первая карта крупье
            yield return new WaitForSeconds(0.1f);
            int card2 = DrawCard();
            dealerCards.Add(card2);
            dealerCardNames.Add(GetCardName(card2));
            UpdateCardDisplay();
            UpdateBlackjackUI();
            Debug.Log($"[ComputerSystem] Крупье раздана карта: {card2}");

            // Вторая карта игроку
            yield return new WaitForSeconds(0.1f);
            int card3 = DrawCard();
            playerCards.Add(card3);
            playerCardNames.Add(GetCardName(card3));
            UpdateCardDisplay();
            UpdateBlackjackUI();
            Debug.Log($"[ComputerSystem] Игроку раздана вторая карта: {card3}");

            // Вторая карта крупье (скрытая)
            yield return new WaitForSeconds(0.1f);
            int card4 = DrawCard();
            dealerCards.Add(card4);
            dealerCardNames.Add(GetCardName(card4));
            UpdateCardDisplay();
            UpdateBlackjackUI();
            Debug.Log($"[ComputerSystem] Крупье раздана вторая карта (скрытая): {card4}");

            // Проверяем блэк джек
            int playerValue = CalculateHandValue(playerCards);
            int dealerValue = CalculateHandValue(dealerCards);
            Debug.Log($"[ComputerSystem] Сумма игрока после раздачи: {playerValue}");
            Debug.Log($"[ComputerSystem] Сумма крупье после раздачи: {dealerValue}");
            
            if (playerValue == 21)
            {
                Debug.Log("[ComputerSystem] БЛЭК ДЖЕК У ИГРОКА!");
                // Немедленно покажем корректные суммы и раскроем карты для ясности
                gameEnded = true;
                gameInProgress = false;
                dealerTurn = false;
                UpdateCardDisplay();

                // Принудительно обновляем счетчики
                if (playerSumText != null) playerSumText.text = playerValue.ToString();
                if (dealerSumText != null) dealerSumText.text = CalculateHandValue(dealerCards).ToString();

                yield return new WaitForSeconds(0.5f);
                EndGameWithBlackjack();
            }
            else
            {
                gameInProgress = true;
                isDealingCards = false;
                
                // Принудительно обновляем UI
                UpdateBlackjackUI();
                UpdateBlackjackButtons();
                
                // Дополнительная проверка обновления счета
                if (playerSumText != null)
                {
                    playerSumText.text = playerValue.ToString();
                    Debug.Log($"[ComputerSystem] Принудительно обновлен счет игрока: {playerValue}");
                }
                
                if (dealerSumText != null)
                {
                    // Показываем только первую карту дилера во время игры
                    int dealerVisibleSum = CalculateCardValue(dealerCards[0]);
                    dealerSumText.text = dealerVisibleSum.ToString();
                    Debug.Log($"[ComputerSystem] Принудительно обновлен счет дилера (видимый): {dealerVisibleSum}");
                }
                
                Debug.Log("[ComputerSystem] Раздача завершена, ход игрока");
            }
        }

        /// <summary>
        /// Игрок берет карту
        /// </summary>
        public void PlayerHit()
        {
            Debug.Log("[ComputerSystem] === ИГРОК БЕРЕТ КАРТУ ===");
            
            if (!gameInProgress || dealerTurn || gameEnded || isDealingCards) 
            {
                Debug.LogWarning("[ComputerSystem] PlayerHit заблокирован!");
                return;
            }

            StartCoroutine(HitCardAnimation());
        }

        /// <summary>
        /// Анимация взятия карты
        /// </summary>
        System.Collections.IEnumerator HitCardAnimation()
        {
            isDealingCards = true;

            // Берем карту
            yield return new WaitForSeconds(0.1f);
            int newCard = DrawCard();
            playerCards.Add(newCard);
            playerCardNames.Add(GetCardName(newCard));
            UpdateCardDisplay();

            int playerValue = CalculateHandValue(playerCards);
            Debug.Log($"[ComputerSystem] Игрок взял карту: {newCard}, новая сумма: {playerValue}");

            // Принудительно обновляем счет
            if (playerSumText != null)
            {
                playerSumText.text = playerValue.ToString();
                Debug.Log($"[ComputerSystem] Принудительно обновлен счет игрока после взятия карты: {playerValue}");
            }

            yield return new WaitForSeconds(0.2f);

            if (playerValue > 21)
            {
                Debug.Log("[ComputerSystem] ПЕРЕБОР! Игрок проиграл");
                yield return new WaitForSeconds(0.3f);
                EndGameWithBust();
            }
            else if (playerValue == 21)
            {
                Debug.Log("[ComputerSystem] Игрок набрал 21! BLACKJACK!");
                yield return new WaitForSeconds(0.3f);
                EndGameWithBlackjack();
            }
            else
            {
                UpdateBlackjackUI();
            }

            isDealingCards = false;
        }

        /// <summary>
        /// Игрок останавливается
        /// </summary>
        public void PlayerStand()
        {
            Debug.Log("[ComputerSystem] === ИГРОК ОСТАНОВИЛСЯ ===");
            
            if (!gameInProgress || gameEnded) 
            {
                Debug.LogWarning("[ComputerSystem] PlayerStand заблокирован!");
                return;
            }

            dealerTurn = true;
            Debug.Log("[ComputerSystem] Начинается ход крупье");
            Debug.Log($"[ComputerSystem] Состояние перед ходом дилера: gameInProgress={gameInProgress}, gameEnded={gameEnded}, dealerTurn={dealerTurn}");
            Debug.Log($"[ComputerSystem] Карты дилера перед ходом: [{string.Join(", ", dealerCards)}]");

            // Ход крупье
            StartCoroutine(DealerTurn());
        }

        /// <summary>
        /// Ход крупье
        /// </summary>
        System.Collections.IEnumerator DealerTurn()
        {
            Debug.Log("[ComputerSystem] === ХОД КРУПЬЕ ===");
            
            // Показываем скрытую карту крупье
            yield return new WaitForSeconds(0.3f);
            gameEnded = true; // Временно устанавливаем gameEnded чтобы показать скрытую карту
            UpdateCardDisplay();
            Debug.Log("[ComputerSystem] Скрытая карта крупье открыта");

            yield return new WaitForSeconds(0.2f);

            // ОБНОВЛЯЕМ СЧЕТ ДИЛЕРА ПОСЛЕ РАСКРЫТИЯ СКРЫТОЙ КАРТЫ
            int dealerValue = CalculateHandValue(dealerCards);
            Debug.Log($"[ComputerSystem] Счет дилера после раскрытия скрытой карты: {dealerValue}");
            Debug.Log($"[ComputerSystem] Карты крупье: [{string.Join(", ", dealerCards)}]");
            
            // Принудительно обновляем UI счет дилера
            if (dealerSumText != null)
            {
                dealerSumText.text = dealerValue.ToString();
                Debug.Log($"[ComputerSystem] Обновлен UI счет дилера после раскрытия: {dealerValue}");
            }
            
            yield return new WaitForSeconds(0.3f);
            
            // Проверяем условие для добирания карт
            Debug.Log($"[ComputerSystem] Дилер должен добирать карты? {dealerValue < 17} (сумма {dealerValue} < 17)");
            
            int cardsDrawn = 0;
            while (dealerValue < 17)
            {
                yield return new WaitForSeconds(0.3f);
                int newCard = DrawCard();
                dealerCards.Add(newCard);
                dealerCardNames.Add(GetCardName(newCard));
                cardsDrawn++;
                
                dealerValue = CalculateHandValue(dealerCards);
                Debug.Log($"[ComputerSystem] Крупье взял карту #{cardsDrawn}: {newCard}, новая сумма: {dealerValue}");
                Debug.Log($"[ComputerSystem] Все карты крупье: [{string.Join(", ", dealerCards)}]");
                
                // Обновляем отображение карт
                UpdateCardDisplay();
                
                // Принудительно обновляем счет крупье
                if (dealerSumText != null)
                {
                    dealerSumText.text = dealerValue.ToString();
                    Debug.Log($"[ComputerSystem] Обновлен UI счет крупье: {dealerValue}");
                }
            }

            Debug.Log($"[ComputerSystem] Крупье закончил брать карты. Финальная сумма: {dealerValue}");
            Debug.Log($"[ComputerSystem] Всего карт у крупье: {dealerCards.Count}");
            yield return new WaitForSeconds(0.3f);
            EndGame();
        }

        /// <summary>
        /// Завершает игру с блэк джеком
        /// </summary>
        void EndGameWithBlackjack()
        {
            Debug.Log("[ComputerSystem] === БЛЭК ДЖЕК ===");
            gameEnded = true;
            StartCoroutine(BlackjackAnimation());
        }

        /// <summary>
        /// Анимация блэк джека
        /// </summary>
        System.Collections.IEnumerator BlackjackAnimation()
        {
            yield return new WaitForSeconds(0.3f);
            
            int winnings = blackjackCurrentBet + (blackjackCurrentBet * 3 / 2);
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.Add(winnings);
                blackjackBalance = MoneyManager.Instance.GetBalance();
            }
            else
            {
                blackjackBalance += winnings;
            }
            Debug.Log($"[ComputerSystem] БЛЭК ДЖЕК! Выигрыш: {winnings}");
            
            yield return new WaitForSeconds(gameEndDelay);
            UpdateBlackjackUI();
            ResetGame();
        }

        /// <summary>
        /// Завершает игру с перебором
        /// </summary>
        void EndGameWithBust()
        {
            Debug.Log("[ComputerSystem] === ПЕРЕБОР ===");
            gameEnded = true;
            StartCoroutine(BustAnimation());
        }

        /// <summary>
        /// Анимация перебора
        /// </summary>
        System.Collections.IEnumerator BustAnimation()
        {
            yield return new WaitForSeconds(0.3f);
            Debug.Log("[ComputerSystem] ПЕРЕБОР! Игрок проиграл");
            
            yield return new WaitForSeconds(gameEndDelay);
            UpdateBlackjackUI();
            ResetGame();
        }

        /// <summary>
        /// Завершает игру
        /// </summary>
        void EndGame()
        {
            Debug.Log("[ComputerSystem] === ЗАВЕРШЕНИЕ ИГРЫ ===");
            gameEnded = true;
            int playerValue = CalculateHandValue(playerCards);
            int dealerValue = CalculateHandValue(dealerCards);

            Debug.Log($"[ComputerSystem] Финальный счет: Игрок {playerValue} vs Крупье {dealerValue}");
            Debug.Log($"[ComputerSystem] Карты игрока: [{string.Join(", ", playerCards)}]");
            Debug.Log($"[ComputerSystem] Карты крупье: [{string.Join(", ", dealerCards)}]");

            StartCoroutine(EndGameAnimation(playerValue, dealerValue));
        }

        /// <summary>
        /// Анимация завершения игры
        /// </summary>
        System.Collections.IEnumerator EndGameAnimation(int playerValue, int dealerValue)
        {
            yield return new WaitForSeconds(0.5f);

            Debug.Log($"[ComputerSystem] === РЕЗУЛЬТАТ ИГРЫ ===");
            Debug.Log($"[ComputerSystem] Игрок: {playerValue}, Крупье: {dealerValue}");
            Debug.Log($"[ComputerSystem] Текущая ставка: {blackjackCurrentBet}₽");

            if (dealerValue > 21)
            {
                int winnings = blackjackCurrentBet * 2;
                if (MoneyManager.Instance != null)
                {
                    MoneyManager.Instance.Add(winnings);
                    blackjackBalance = MoneyManager.Instance.GetBalance();
                }
                else
                {
                    blackjackBalance += winnings;
                }
                Debug.Log($"[ComputerSystem] Крупье перебрал! Выигрыш: {winnings}");
            }
            else if (playerValue > dealerValue)
            {
                int winnings = blackjackCurrentBet * 2;
                if (MoneyManager.Instance != null)
                {
                    MoneyManager.Instance.Add(winnings);
                    blackjackBalance = MoneyManager.Instance.GetBalance();
                }
                else
                {
                    blackjackBalance += winnings;
                }
                Debug.Log($"[ComputerSystem] Игрок выиграл! Выигрыш: {winnings}");
            }
            else if (playerValue < dealerValue)
            {
                Debug.Log("[ComputerSystem] Крупье выиграл");
            }
            else
            {
                if (MoneyManager.Instance != null)
                {
                    MoneyManager.Instance.Add(blackjackCurrentBet);
                    blackjackBalance = MoneyManager.Instance.GetBalance();
                }
                else
                {
                    blackjackBalance += blackjackCurrentBet;
                }
                Debug.Log($"[ComputerSystem] Ничья! Ставка возвращена: {blackjackCurrentBet}");
            }

            yield return new WaitForSeconds(gameEndDelay);
            UpdateBlackjackUI();
            ResetGame();
        }

        /// <summary>
        /// Вычисляет значение карты
        /// </summary>
        int CalculateCardValue(int card)
        {
            if (card == 1) return 11; // Туз = 11
            return card;
        }

        /// <summary>
        /// Вычисляет значение руки
        /// </summary>
        int CalculateHandValue(List<int> cards)
        {
            if (cards == null || cards.Count == 0)
            {
                Debug.LogWarning("[ComputerSystem] Попытка рассчитать пустую руку!");
                return 0;
            }

            int sum = 0;
            int aces = 0;

            foreach (int card in cards)
            {
                Debug.Log($"[ComputerSystem] Обрабатываем карту: {card}");
                if (card == 1)
                {
                    aces++;
                    sum += 11;
                    Debug.Log($"[ComputerSystem] Туз найден! Сумма: {sum}, Тузов: {aces}");
                }
                else if (card >= 11 && card <= 13)
                {
                    // Валет, Дама, Король = 10
                    sum += 10;
                    Debug.Log($"[ComputerSystem] Фигура {card} = 10. Сумма: {sum}");
                }
                else
                {
                    sum += card;
                    Debug.Log($"[ComputerSystem] Обычная карта {card}. Сумма: {sum}");
                }
            }

            // Корректируем тузы если перебор
            while (sum > 21 && aces > 0)
            {
                sum -= 10;
                aces--;
                Debug.Log($"[ComputerSystem] Корректируем туз! Новая сумма: {sum}, Осталось тузов: {aces}");
            }

            Debug.Log($"[ComputerSystem] Финальный расчет руки: карты [{string.Join(", ", cards)}], сумма: {sum}, тузов: {aces}");
            return sum;
        }

        /// <summary>
        /// Получает сумму ставки в блэк джеке
        /// </summary>
        int GetBlackjackBetAmount()
        {
            if (blackjackBetInputField != null && int.TryParse(blackjackBetInputField.text, out int amount))
            {
                return Mathf.Clamp(amount, 10, 5000); // Мин 10₽, макс 5000₽
            }
            return blackjackCurrentBet;
        }

        // ==================== BLACKJACK UI КАРТЫ ====================

        /// <summary>
        /// Создает карту в UI
        /// </summary>
        GameObject CreateCardUI(int cardValue, bool isDealer = false, bool isHidden = false, int index = -1)
        {
            if (cardPrefab == null)
            {
                Debug.LogError("[ComputerSystem] CardPrefab не назначен!");
                return null;
            }

            GameObject container = isDealer ? dealerCardsContainer : playerCardsContainer;
            if (container == null)
            {
                Debug.LogError($"[ComputerSystem] {(isDealer ? "DealerCardsContainer" : "PlayerCardsContainer")} не назначен!");
                return null;
            }

            // Создаем карту
            GameObject cardObject = Instantiate(cardPrefab, container.transform);
            Image cardImage = cardObject.GetComponent<Image>();
            
            if (cardImage == null)
            {
                Debug.LogError("[ComputerSystem] CardPrefab должен содержать Image компонент!");
                Destroy(cardObject);
                return null;
            }

            // Устанавливаем спрайт карты
            if (isHidden)
            {
                // Скрытая карта (рубашка)
                if (BlackJackCardManager.Instance != null)
                {
                    Sprite cardBackSprite = BlackJackCardManager.Instance.GetCardSprite("cardBack");
                    if (cardBackSprite != null)
                    {
                        cardImage.sprite = cardBackSprite;
                        Debug.Log("[ComputerSystem] Установлена рубашка карты");
                    }
                    else
                    {
                        Debug.LogError("[ComputerSystem] Не удалось загрузить спрайт cardBack!");
                    }
                }
                else
                {
                    Debug.LogWarning("[ComputerSystem] BlackJackCardManager не найден!");
                }
            }
            else
            {
                // Видимая карта
                string cardName;
                if (index >= 0)
                {
                    // Берём заранее выбранное имя, чтобы масть/цвет не прыгали между перерисовками
                    if (isDealer)
                    {
                        if (index < dealerCardNames.Count) cardName = dealerCardNames[index];
                        else { cardName = GetCardName(cardValue); if (index == dealerCardNames.Count) dealerCardNames.Add(cardName); }
                    }
                    else
                    {
                        if (index < playerCardNames.Count) cardName = playerCardNames[index];
                        else { cardName = GetCardName(cardValue); if (index == playerCardNames.Count) playerCardNames.Add(cardName); }
                    }
                }
                else
                {
                    cardName = GetCardName(cardValue);
                }
                Debug.Log($"[ComputerSystem] Пытаемся загрузить карту: {cardName} для значения {cardValue}");
                
                if (BlackJackCardManager.Instance != null)
                {
                    Sprite cardSprite = BlackJackCardManager.Instance.GetCardSprite(cardName);
                    if (cardSprite != null)
                    {
                        cardImage.sprite = cardSprite;
                        Debug.Log($"[ComputerSystem] Успешно установлен спрайт карты: {cardName}");
                    }
                    else
                    {
                        Debug.LogError($"[ComputerSystem] Не удалось загрузить спрайт карты: {cardName}");
                    }
                }
                else
                {
                    Debug.LogWarning("[ComputerSystem] BlackJackCardManager не найден!");
                }
            }

            // Позиционируем карту
            PositionCard(cardObject, isDealer ? dealerCardObjects.Count : playerCardObjects.Count, isDealer);

            return cardObject;
        }

        /// <summary>
        /// Получает имя карты по значению
        /// </summary>
        string GetCardName(int cardValue)
        {
            // Выбираем случайную масть для разнообразия
            string[] suits = { "H", "D", "C", "S" }; // Черви, Бубны, Трефы, Пики
            string suit = suits[Random.Range(0, suits.Length)];
            
            // Преобразуем числовое значение в имя спрайта
            string cardName;
            if (cardValue == 1) 
                cardName = $"a{suit}"; // Туз
            else if (cardValue == 10) 
                cardName = $"t{suit}"; // Десятка (используем "t" вместо "10")
            else if (cardValue == 11) 
                cardName = $"j{suit}"; // Валет (если есть)
            else if (cardValue == 12) 
                cardName = $"q{suit}"; // Дама (если есть)
            else if (cardValue == 13) 
                cardName = $"k{suit}"; // Король (если есть)
            else if (cardValue >= 2 && cardValue <= 9)
                cardName = $"{cardValue}{suit}"; // Обычные карты 2-9
            else
                cardName = $"2{suit}"; // Fallback
            
            Debug.Log($"[ComputerSystem] Генерируем имя карты: {cardName} для значения {cardValue}");
            return cardName;
        }

        /// <summary>
        /// Позиционирует карту в контейнере
        /// </summary>
        void PositionCard(GameObject card, int cardIndex, bool isDealer)
        {
            RectTransform rectTransform = card.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            // Простое позиционирование - карты рядом друг с другом
            float cardWidth = 80f; // Ширина карты
            float spacing = 20f; // Отступ между картами
            float startX = -((cardWidth + spacing) * 2); // Начинаем с центра

            float posX = startX + (cardIndex * (cardWidth + spacing));
            rectTransform.anchoredPosition = new Vector2(posX, 0);
        }

        /// <summary>
        /// Очищает все карты в UI
        /// </summary>
        void ClearAllCardObjects()
        {
            // Очищаем карты игрока
            foreach (GameObject card in playerCardObjects)
            {
                if (card != null)
                    Destroy(card);
            }
            playerCardObjects.Clear();

            // Очищаем карты крупье
            foreach (GameObject card in dealerCardObjects)
            {
                if (card != null)
                    Destroy(card);
            }
            dealerCardObjects.Clear();
        }

        /// <summary>
        /// Обновляет отображение карт
        /// </summary>
        void UpdateCardDisplay()
        {
            // Очищаем старые карты
            ClearAllCardObjects();

            // Создаем карты игрока (перерисовываем в исходном порядке выдачи)
            for (int i = 0; i < playerCards.Count; i++)
            {
                int card = playerCards[i];
                GameObject cardObject = CreateCardUI(card, false, false, i);
                if (cardObject != null)
                    playerCardObjects.Add(cardObject);
            }

            // Создаем карты крупье (перерисовываем в исходном порядке выдачи)
            for (int i = 0; i < dealerCards.Count; i++)
            {
                bool isHidden = (i == 1 && gameInProgress && !gameEnded); // Вторая карта скрыта во время игры
                GameObject cardObject = CreateCardUI(dealerCards[i], true, isHidden, i);
                if (cardObject != null)
                    dealerCardObjects.Add(cardObject);
            }

            Debug.Log($"[ComputerSystem] Обновлено отображение карт: Игрок {playerCards.Count} карт, Крупье {dealerCards.Count} карт");
            Debug.Log($"[ComputerSystem] Состояние игры: gameInProgress={gameInProgress}, gameEnded={gameEnded}, dealerTurn={dealerTurn}");
        }

        /// <summary>
        /// Создает контейнеры для карт в блэк джеке, если они не существуют
        /// </summary>
        void CreateCardContainers()
        {
            // Создаем BlackJackCardManager если его нет
            if (BlackJackCardManager.Instance == null)
            {
                GameObject cardManagerObject = new GameObject("BlackJackCardManager");
                cardManagerObject.AddComponent<BlackJackCardManager>();
                Debug.Log("[ComputerSystem] BlackJackCardManager создан автоматически");
            }

            // Проверяем загрузку спрайтов
            if (BlackJackCardManager.Instance != null)
            {
                // Тестируем загрузку карт
                Sprite testCard = BlackJackCardManager.Instance.GetCardSprite("2H");
                if (testCard != null)
                {
                    Debug.Log("[ComputerSystem] Спрайты карт загружены успешно");
                }
                else
                {
                    Debug.LogError("[ComputerSystem] Спрайты карт НЕ загружены! Проверь папку Resources/BlackJack");
                }

                Sprite testBack = BlackJackCardManager.Instance.GetCardSprite("cardBack");
                if (testBack != null)
                {
                    Debug.Log("[ComputerSystem] Спрайт рубашки загружен успешно");
                }
                else
                {
                    Debug.LogError("[ComputerSystem] Спрайт рубашки НЕ загружен!");
                }

                // Тестируем разные карты (используем реальные имена из твоей папки)
                string[] testCards = { "2H", "aH", "tH", "3H", "4H", "5H", "6H", "7H", "8H", "9H" };
                foreach (string cardName in testCards)
                {
                    Sprite card = BlackJackCardManager.Instance.GetCardSprite(cardName);
                    if (card != null)
                    {
                        Debug.Log($"[ComputerSystem] Карта {cardName} загружена успешно");
                    }
                    else
                    {
                        Debug.LogError($"[ComputerSystem] Карта {cardName} НЕ загружена!");
                    }
                }
            }

            // Проверяем назначение контейнеров
            if (playerCardsContainer == null)
            {
                Debug.LogError("[ComputerSystem] PlayerCardsContainer не назначен! Назначь контейнер для карт игрока в Inspector.");
            }

            if (dealerCardsContainer == null)
            {
                Debug.LogError("[ComputerSystem] DealerCardsContainer не назначен! Назначь контейнер для карт крупье в Inspector.");
            }

            if (cardPrefab == null)
            {
                Debug.LogError("[ComputerSystem] CardPrefab не назначен! Назначь префаб карты в Inspector.");
            }
        }

        /// <summary>
        /// Загружает данные из склада
        /// </summary>
        void LoadWarehouseData()
        {
            Debug.Log("[ComputerSystem] === ЗАГРУЗКА ДАННЫХ ИЗ СКЛАДА ===");
            
            // Очищаем старые данные
            allItems.Clear();
            
            if (WarehouseSystem.Instance == null)
            {
                Debug.LogWarning("[ComputerSystem] WarehouseSystem не найден!");
                return;
            }
            
            // Получаем все предметы со склада
            List<LostAndFound.Systems.Item> warehouseItems = WarehouseSystem.Instance.GetAllItems();
            
            Debug.Log($"[ComputerSystem] Найдено предметов на складе: {warehouseItems.Count}");
            
            // Конвертируем в DatabaseItem
            foreach (LostAndFound.Systems.Item item in warehouseItems)
            {
                if (item != null && !string.IsNullOrEmpty(item.itemName))
                {
                    string id = GenerateUniqueID();
                    string date = System.DateTime.Now.ToString("dd.MM.yyyy");
                    
                    DatabaseItem dbItem = new DatabaseItem(id, item.itemName, item.itemDescription, date, item.gameObject);
                    allItems.Add(dbItem);
                    
                    Debug.Log($"[ComputerSystem] Добавлен предмет: {item.itemName} (ID: {id})");
                }
            }
            
            // Вычисляем количество страниц
            totalPages = Mathf.CeilToInt((float)allItems.Count / itemsPerPage);
            if (totalPages == 0) totalPages = 1;
            
            Debug.Log($"[ComputerSystem] Загружено предметов в базу: {allItems.Count}, страниц: {totalPages}");
        }

        /// <summary>
        /// Загружает данные Черного рынка (только объекты с тегом contraband)
        /// </summary>
        void LoadBlackMarketData()
        {
            Debug.Log("[ComputerSystem] === ЗАГРУЗКА ДАННЫХ ЧЕРНОГО РЫНКА ===");

            blackMarketItems.Clear();

            if (WarehouseSystem.Instance == null)
            {
                Debug.LogWarning("[ComputerSystem] WarehouseSystem не найден для Черного рынка!");
                // Даже если склада нет, покажем только лоты игрока
                RebuildBlackMarketFromListed();
                return;
            }

            List<LostAndFound.Systems.Item> warehouseItems = WarehouseSystem.Instance.GetAllItems();
            Debug.Log($"[ComputerSystem] Найдено предметов на складе (для ЧР): {warehouseItems.Count}");

            foreach (LostAndFound.Systems.Item item in warehouseItems)
            {
                TryAddContrabandToBlackMarket(item);
            }

            // Добавляем лоты, выставленные игроком напрямую (без склада)
            foreach (var lot in blackMarketListedItems)
            {
                if (lot != null) blackMarketItems.Add(lot);
            }

            blackMarketTotalPages = Mathf.CeilToInt((float)blackMarketItems.Count / blackMarketItemsPerPage);
            if (blackMarketTotalPages == 0) blackMarketTotalPages = 1;

            Debug.Log($"[ComputerSystem] [ЧР] Загружено: {blackMarketItems.Count}, страниц: {blackMarketTotalPages}");
        }

        // Добавляет предмет в список Черного рынка, если он подходит по тегу
        void TryAddContrabandToBlackMarket(LostAndFound.Systems.Item item)
        {
            if (item == null) return;
            bool isContraband = false;
            try { isContraband = item.CompareTag("contraband") || item.gameObject.CompareTag("contraband"); } catch { }
            if (!isContraband) return;

            string id = GenerateUniqueID();
            string date = System.DateTime.Now.ToString("dd.MM.yyyy");
            DatabaseItem dbItem = new DatabaseItem(id, item.itemName, item.itemDescription, date, item.gameObject);
            blackMarketItems.Add(dbItem);
            Debug.Log($"[ComputerSystem] [ЧР] Добавлен: {item.itemName} (ID: {id})");
        }

        // Переcобирает список Черного рынка только из вручную выставленных лотов
        void RebuildBlackMarketFromListed()
        {
            blackMarketItems.Clear();
            foreach (var lot in blackMarketListedItems)
            {
                if (lot != null) blackMarketItems.Add(lot);
            }
            blackMarketTotalPages = Mathf.CeilToInt((float)blackMarketItems.Count / blackMarketItemsPerPage);
            if (blackMarketTotalPages == 0) blackMarketTotalPages = 1;
        }

        /// <summary>
        /// Показ страницы Черного рынка
        /// </summary>
        void ShowBlackMarketPage(int pageNumber)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > blackMarketTotalPages) pageNumber = blackMarketTotalPages;

            blackMarketCurrentPage = pageNumber;

            ClearBlackMarketCurrentPageCards();

            // На случай изменений списка между вызовами (например, добавили 7-й предмет),
            // страхуемся от выхода за пределы и пересчитываем totalPages
            blackMarketTotalPages = Mathf.CeilToInt((float)blackMarketItems.Count / blackMarketItemsPerPage);
            if (blackMarketTotalPages == 0) blackMarketTotalPages = 1;
            if (blackMarketCurrentPage > blackMarketTotalPages) blackMarketCurrentPage = blackMarketTotalPages;

            int startIndex = (blackMarketCurrentPage - 1) * blackMarketItemsPerPage;

            // Верхний ряд (до 6)
            for (int i = 0; i < 6; i++)
            {
                int itemIndex = startIndex + i;
                if (itemIndex < blackMarketItems.Count)
                {
                    CreateBlackMarketItemCardInRow(blackMarketItems[itemIndex], i, true);
                }
            }

            // Нижний ряд (следующие 6)
            for (int i = 0; i < 6; i++)
            {
                int itemIndex = startIndex + 6 + i;
                if (itemIndex < blackMarketItems.Count)
                {
                    CreateBlackMarketItemCardInRow(blackMarketItems[itemIndex], i, false);
                }
            }

            UpdateBlackMarketNavigationUI();
        }

        /// <summary>
        /// Создание карточки предмета Черного рынка с 3D превью
        /// </summary>
        void CreateBlackMarketItemCard(DatabaseItem item, int cardPosition)
        {
            GameObject prefab = blackMarketItemCardPrefab != null ? blackMarketItemCardPrefab : itemCardPrefab;
            if (prefab == null || blackMarketCardsContainer == null)
            {
                Debug.LogError("[ComputerSystem] [ЧР] Не назначены префаб карточки или контейнер!");
                return;
            }

            GameObject cardObject = Instantiate(prefab, blackMarketCardsContainer);
            blackMarketCurrentPageCards.Add(cardObject);

            PositionCard(cardObject, cardPosition); // та же сетка 5x2

            // Поддержка двух вариантов: BlackMarketItemCard или (fallback) ItemCard
            if (!TryFillBlackMarketCard(cardObject, item))
            {
                var itemCard = cardObject.GetComponent<ItemCard>();
                if (itemCard != null) { itemCard.FillCard(item); }
                else Debug.LogWarning("[ComputerSystem] [ЧР] На префабе нет компонента карточки (BlackMarketItemCard/ItemCard)!");
            }
        }

        void CreateBlackMarketItemCardInRow(DatabaseItem item, int indexInRow, bool isTopRow)
        {
            GameObject prefab = blackMarketItemCardPrefab != null ? blackMarketItemCardPrefab : itemCardPrefab;
            if (prefab == null)
            {
                Debug.LogError("[ComputerSystem] [ЧР] Префаб карточки не назначен!");
                return;
            }

            RectTransform rowContainer = isTopRow ? blackMarketRowTopContainer : blackMarketRowBottomContainer;
            if (rowContainer == null)
            {
                // fallback на общий контейнер
                CreateBlackMarketItemCard(item, isTopRow ? indexInRow : indexInRow + 6);
                return;
            }

            GameObject cardObject = Instantiate(prefab, rowContainer);
            blackMarketCurrentPageCards.Add(cardObject);
            PositionCardInRow(cardObject, indexInRow);

            if (!TryFillBlackMarketCard(cardObject, item))
            {
                var itemCard = cardObject.GetComponent<ItemCard>();
                if (itemCard != null) { itemCard.FillCard(item); }
            }
        }

        // Универсальная попытка заполнить карточку Черного рынка без прямой зависимости от типа
        bool TryFillBlackMarketCard(GameObject cardObject, DatabaseItem item)
        {
            if (cardObject == null) return false;
            var component = cardObject.GetComponent("BlackMarketItemCard");
            if (component == null) return false;

            var fillMethod = component.GetType().GetMethod("FillCard", new System.Type[] { typeof(DatabaseItem) });
            if (fillMethod == null) return false;

            fillMethod.Invoke(component, new object[] { item });
            return true;
        }

        void ClearBlackMarketCurrentPageCards()
        {
            foreach (GameObject card in blackMarketCurrentPageCards)
            {
                if (card != null) Destroy(card);
            }
            blackMarketCurrentPageCards.Clear();
        }

        void UpdateBlackMarketNavigationUI()
        {
            if (blackMarketPageInfoText != null)
                blackMarketPageInfoText.text = blackMarketCurrentPage.ToString();

            if (blackMarketPrevPageButton != null)
                blackMarketPrevPageButton.interactable = (blackMarketCurrentPage > 1);

            if (blackMarketNextPageButton != null)
                blackMarketNextPageButton.interactable = (blackMarketCurrentPage < blackMarketTotalPages);
        }

        // Публичный метод: выставить текущий предмет из инвентаря на Черный рынок
        public void ListCurrentInventoryItemOnBlackMarket()
        {
            if (PlayerInventory.Instance == null || !PlayerInventory.Instance.HasItem())
            {
                Debug.LogWarning("[ComputerSystem] [ЧР] В инвентаре нет предмета для выставления");
                return;
            }

            Item item = PlayerInventory.Instance.GetCurrentItem();
            if (item == null)
            {
                Debug.LogWarning("[ComputerSystem] [ЧР] Текущий предмет инвентаря null");
                return;
            }

            // Только с тегом contraband
            bool isContraband = false;
            try { isContraband = item.CompareTag("contraband") || item.gameObject.CompareTag("contraband"); } catch { }
            if (!isContraband)
            {
                Debug.LogWarning("[ComputerSystem] [ЧР] Предмет не имеет тег contraband — выставление запрещено");
                return;
            }

            // Удаляем предмет из инвентаря, но не уничтожаем — он нужен для превью
            PlayerInventory.Instance.RemoveItem();

            string id = GenerateUniqueID();
            string date = System.DateTime.Now.ToString("dd.MM.yyyy");
            DatabaseItem lot = new DatabaseItem(id, item.itemName, item.itemDescription, date, item.gameObject);
            blackMarketListedItems.Add(lot);
            Debug.Log($"[ComputerSystem] [ЧР] Лот выставлен: {item.itemName} (ID: {id})");

            // Обновляем UI если панель Черного рынка открыта
            if (blackMarketPanel != null && blackMarketPanel.activeInHierarchy)
            {
                // Пересобираем полный список (склад + лоты игрока), чтобы сразу увидеть 7-ю карточку во 2-м ряду
                LoadBlackMarketData();
                // Остаемся на текущей странице
                ShowBlackMarketPage(blackMarketCurrentPage);
            }
        }

        /// <summary>
        /// Генерирует уникальный ID
        /// </summary>
        string GenerateUniqueID()
        {
            string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int letter1 = Random.Range(0, 26);
            int letter2 = Random.Range(0, 26);
            int numbers = Random.Range(1000, 10000);
            
            return $"{letters[letter1]}{letters[letter2]}{numbers}";
        }

        /// <summary>
        /// Показывает указанную страницу
        /// </summary>
        void ShowPage(int pageNumber)
        {
            Debug.Log($"[ComputerSystem] === ПОКАЗ СТРАНИЦЫ {pageNumber} ===");
            
            // Проверяем корректность номера страницы
            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > totalPages) pageNumber = totalPages;
            
            currentPage = pageNumber;
            
            // Очищаем текущие карточки
            ClearCurrentPageCards();
            
            // Вычисляем какие предметы показать
            int startIndex = (currentPage - 1) * itemsPerPage;
            int endIndex = Mathf.Min(startIndex + itemsPerPage, allItems.Count);
            
            Debug.Log($"[ComputerSystem] Показываем предметы с {startIndex} по {endIndex - 1}");
            
            // Создаем карточки для текущей страницы (6 сверху + 6 снизу)
            int created = 0;
            for (int i = 0; i < 6; i++)
            {
                int itemIndex = startIndex + i;
                if (itemIndex < allItems.Count)
                {
                    CreateItemCardInRow(allItems[itemIndex], i, true);
                    created++;
                }
            }
            for (int i = 0; i < 6; i++)
            {
                int itemIndex = startIndex + 6 + i;
                if (itemIndex < allItems.Count)
                {
                    CreateItemCardInRow(allItems[itemIndex], i, false);
                    created++;
                }
            }
            
            // Обновляем UI навигации
            UpdateNavigationUI();
            
            Debug.Log($"[ComputerSystem] Страница {currentPage} показана, создано карточек: {currentPageCards.Count}");
        }

        /// <summary>
        /// Создает карточку предмета
        /// </summary>
        void CreateItemCard(DatabaseItem item, int cardPosition)
        {
            if (itemCardPrefab == null || cardsContainer == null)
            {
                Debug.LogError("[ComputerSystem] itemCardPrefab или cardsContainer не назначены!");
                return;
            }
            
            // Создаем карточку из префаба
            GameObject cardObject = Instantiate(itemCardPrefab, cardsContainer);
            currentPageCards.Add(cardObject);
            
            // Позиционируем карточку по сетке 5x2
            PositionCard(cardObject, cardPosition);
            
            // Получаем компонент ItemCard и заполняем данными
            var itemCard = cardObject.GetComponent<ItemCard>();
            if (itemCard != null)
            {
                itemCard.FillCard(item);
            }
            else
            {
                Debug.LogWarning("[ComputerSystem] ItemCard компонент не найден в префабе!");
            }
            
            Debug.Log($"[ComputerSystem] Создана карточка для: {item.name} в позиции {cardPosition}");
        }

        // Создание карточки в верхнем/нижнем ряду (6 в ряд)
        void CreateItemCardInRow(DatabaseItem item, int indexInRow, bool isTopRow)
        {
            if (itemCardPrefab == null)
            {
                Debug.LogError("[ComputerSystem] itemCardPrefab не назначен!");
                return;
            }

            RectTransform rowContainer = isTopRow ? databaseRowTopContainer : databaseRowBottomContainer;
            if (rowContainer == null)
            {
                // fallback на общий контейнер
                CreateItemCard(item, isTopRow ? indexInRow : indexInRow + 6);
                return;
            }

            GameObject cardObject = Instantiate(itemCardPrefab, rowContainer);
            currentPageCards.Add(cardObject);

            PositionCardInRow(cardObject, indexInRow);

            var itemCard = cardObject.GetComponent<ItemCard>();
            if (itemCard != null)
            {
                itemCard.FillCard(item);
            }
        }

        // Позиционирование внутри ряда: 6 карточек 300x430 с равными отступами, динамически по ширине ряда
        void PositionCardInRow(GameObject card, int indexInRow)
        {
            const int columns = 6;
            const float cardWidth = 300f;
            const float cardHeight = 430f;

            RectTransform parentRect = card.transform.parent as RectTransform;
            float containerWidth = parentRect != null ? parentRect.rect.width : 1920f;
            float containerHeight = parentRect != null ? parentRect.rect.height : 430f;

            // Равномерный горизонтальный отступ между карточками и по краям
            float paddingX = Mathf.Max(0f, Mathf.Floor((containerWidth - columns * cardWidth) / (columns + 1)));
            float startX = paddingX;
            float posX = startX + indexInRow * (cardWidth + paddingX);
            // По вертикали центрируем карточку в ряду (или 0, если высота совпадает)
            float paddingTop = Mathf.Max(0f, Mathf.Floor((containerHeight - cardHeight) * 0.5f));

            RectTransform rect = card.GetComponent<RectTransform>();
            if (rect != null)
            {
                // жестко фиксируем якоря и pivot на левый-верх контейнера ряда
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(posX, -paddingTop);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardWidth);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 430f);
            }
        }

        /// <summary>
        /// Позиционирует карточку по сетке 5x2
        /// </summary>
        void PositionCard(GameObject card, int cardPosition)
        {
            // Твоя сетка 5 столбцов, 2 ряда
            int row = cardPosition / 5;        // 0-1 (ряд)
            int col = cardPosition % 5;        // 0-4 (столбец)
            
            // Твои точные координаты
            float startX = 35f;
            float startY = -56f;
            float spacingX = 357f;    // отступ между столбцами
            float spacingY = 496f;    // отступ между рядами
            
            float posX = startX + (col * spacingX);
            float posY = startY - (row * spacingY);
            
            RectTransform rectTransform = card.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(posX, posY);
                Debug.Log($"[ComputerSystem] Карточка {cardPosition} позиционирована: ({posX}, {posY})");
            }
        }

        /// <summary>
        /// Очищает карточки текущей страницы
        /// </summary>
        void ClearCurrentPageCards()
        {
            foreach (GameObject card in currentPageCards)
            {
                if (card != null)
                {
                    Destroy(card);
                }
            }
            currentPageCards.Clear();
            
            Debug.Log("[ComputerSystem] Карточки текущей страницы очищены");
        }

        /// <summary>
        /// Настраивает навигацию
        /// </summary>
        void SetupNavigation()
        {
            // Настройка кнопок базы данных
            if (prevPageButton != null)
                prevPageButton.onClick.AddListener(() => ShowPage(currentPage - 1));
            
            if (nextPageButton != null)
                nextPageButton.onClick.AddListener(() => ShowPage(currentPage + 1));
            
            if (closeDatabaseButton != null)
                closeDatabaseButton.onClick.AddListener(() => HideDatabase());

            // Настройка кнопок казино
            if (casinoButton != null)
                casinoButton.onClick.AddListener(() => ShowCasino());

            // Кнопка Черного рынка на рабочем столе
            if (blackMarketButton != null)
                blackMarketButton.onClick.AddListener(() => ShowBlackMarket());

            if (blackjackButton != null)
                blackjackButton.onClick.AddListener(() => Debug.Log("[ComputerSystem] Запуск Блэк Джека"));

            if (rouletteButton != null)
                rouletteButton.onClick.AddListener(() => {
                    Debug.Log("[ComputerSystem] КНОПКА РУЛЕТКИ НАЖАТА!");
                    ShowRoulette();
                });

            if (closeCasinoButton != null)
                closeCasinoButton.onClick.AddListener(() => HideCasino());

            // Настройка кнопок рулетки
            if (spinButton != null)
                spinButton.onClick.AddListener(() => SpinRoulette());

            if (closeRouletteButton != null)
                closeRouletteButton.onClick.AddListener(() => HideRoulette());

            // Настройка кнопок блэк джека
            if (blackjackButton != null)
                blackjackButton.onClick.AddListener(() => ShowBlackjack());

            if (hitButton != null)
                hitButton.onClick.AddListener(() => PlayerHit());

            if (playButton != null)
                playButton.onClick.AddListener(() => StartBlackjackGame());

            if (standButton != null)
                standButton.onClick.AddListener(() => PlayerStand());

            if (closeBlackjackButton != null)
                closeBlackjackButton.onClick.AddListener(() => HideBlackjack());

            // Навигация Черного рынка
            if (blackMarketPrevPageButton != null)
                blackMarketPrevPageButton.onClick.AddListener(() => ShowBlackMarketPage(blackMarketCurrentPage - 1));

            if (blackMarketNextPageButton != null)
                blackMarketNextPageButton.onClick.AddListener(() => ShowBlackMarketPage(blackMarketCurrentPage + 1));

            if (blackMarketCloseButton != null)
                blackMarketCloseButton.onClick.AddListener(() => HideBlackMarket());

            // Кнопка "Выставить на Черный рынок" из инвентаря (если добавишь её на UI компьютера)
            if (blackMarketListButton != null)
                blackMarketListButton.onClick.AddListener(ListCurrentInventoryItemOnBlackMarket);

            // Настройка кнопок чисел (0-36)
            for (int i = 0; i < numberButtons.Length; i++)
            {
                if (numberButtons[i] != null)
                {
                    int number = i; // Локальная копия для замыкания
                    numberButtons[i].onClick.AddListener(() => BetOnNumber(number));
                }
            }

            // Настройка кнопок столбцов (2to1)
            for (int i = 0; i < column2to1Buttons.Length; i++)
            {
                if (column2to1Buttons[i] != null)
                {
                    int columnIndex = i; // Локальная копия для замыкания
                    column2to1Buttons[i].onClick.AddListener(() => BetOnColumn(columnIndex));
                }
            }

            // Настройка кнопок дюжин (1st12, 2nd12, 3rd12)
            for (int i = 0; i < dozen1st12Buttons.Length; i++)
            {
                if (dozen1st12Buttons[i] != null)
                {
                    int dozenIndex = i; // Локальная копия для замыкания
                    dozen1st12Buttons[i].onClick.AddListener(() => BetOnDozen(dozenIndex));
                }
            }

            // Настройка специальных кнопок
            if (button1to18 != null)
                button1to18.onClick.AddListener(() => BetOn1to18());

            if (buttonEven != null)
                buttonEven.onClick.AddListener(() => BetOnEven());

            if (buttonRed != null)
                buttonRed.onClick.AddListener(() => BetOnRed());

            if (buttonBlack != null)
                buttonBlack.onClick.AddListener(() => BetOnBlack());

            if (buttonOdd != null)
                buttonOdd.onClick.AddListener(() => BetOnOdd());

            if (button19to36 != null)
                button19to36.onClick.AddListener(() => BetOn19to36());
            
            Debug.Log("[ComputerSystem] Навигация, казино и рулетка настроены");
        }

        /// <summary>
        /// Обновляет UI навигации
        /// </summary>
        void UpdateNavigationUI()
        {
            // Обновляем текст страницы - только цифра
            if (pageInfoText != null)
                pageInfoText.text = currentPage.ToString();
            
            // Обновляем кнопки
            if (prevPageButton != null)
                prevPageButton.interactable = (currentPage > 1);
            
            if (nextPageButton != null)
                nextPageButton.interactable = (currentPage < totalPages);
            
            Debug.Log($"[ComputerSystem] UI навигации обновлен: {currentPage}/{totalPages}");
        }

        // Унифицированное форматирование валюты через MoneyManager.uiFormat
        string FormatCurrency(int amount)
        {
            if (MoneyManager.Instance == null)
                return amount.ToString();
            try
            {
                return string.Format(MoneyManager.Instance.uiFormat, amount);
            }
            catch
            {
                return amount.ToString();
            }
        }

        /// <summary>
        /// Обработчик события добавления предмета на склад
        /// </summary>
        void OnItemAddedToWarehouse(LostAndFound.Systems.Item item)
        {
            Debug.Log($"[ComputerSystem] Предмет добавлен на склад: {item.itemName}");
            
            // Если база данных открыта - обновляем её
            if (databasePanel != null && databasePanel.activeInHierarchy)
            {
                LoadWarehouseData();
                ShowPage(currentPage); // Обновляем текущую страницу
            }
        }

        void OnDestroy()
        {
            // Отписываемся от событий
            if (WarehouseSystem.Instance != null)
            {
                WarehouseSystem.Instance.OnItemStored -= OnItemAddedToWarehouse;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
    }
} 
