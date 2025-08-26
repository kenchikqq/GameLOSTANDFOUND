using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.AI;
using LostAndFound.NPCs;
using LostAndFound.Systems;

namespace LostAndFound.NPCs
{
    public class PoliceOfficer : MonoBehaviour
    {
        public enum PoliceState { Idle, Called, InDialog, Finished }
        public enum PanelState { None, Panel1, Panel2, Panel3, Panel4, Panel5, Panel6 }
        
        private PoliceState currentState = PoliceState.Idle;
        private PanelState currentPanelState = PanelState.None;
        private bool isInDialog = false;
        private bool dialogWasInterrupted = false;
        
        [Header("NavMesh")]
        private NavMeshAgent navAgent;
        private Transform targetPosition; // Задается спавнером автоматически
        private Transform spawnPoint; // Задается спавнером автоматически
        private Transform doorTrigger; // промежуточная точка у двери
        private bool doorEntryCompleted = false; // прошли дверь на входе
        public float stoppingDistance = 2f;
        [Tooltip("На каком расстоянии от точки возврата NPC считается прибывшим и исчезает")] public float despawnThreshold = 0.3f;
        
        [Header("Репутация")]
        public int currentReputation = 50;
        public int maxReputation = 100;
        public int minReputation = 0;
        public int contrabandHandoverBonus = 10;
        public int contrabandDetectionPenalty = -15;
        public int blackMarketPenalty = -20;
        
        [Header("Обнаружение")]
        public float detectionRange = 5f;
        public LayerMask contrabandLayerMask = -1;
        
        private bool isCalledByButton = false;
        private bool isWaitingForItem = false;
        private bool detectedContraband = false;
        private bool contrabandDetected = false;
        
        private bool playerInRange = false;
        public float interactionDistance = 2.5f;

        [Header("UI - Панель 1 (Начальный диалог)")]
        public GameObject панель1_НачальныйДиалог;
        public TextMeshProUGUI текст_Панель1;
        public Button кнопка1_Панель1; // "У меня есть контрабанда"
        public Button кнопка2_Панель1; // "Извините, ложный вызов"
        
        [Header("UI - Панель 2 (Принести контрабанду)")]
        public GameObject панель2_ПринестиКонтрабанду;
        public TextMeshProUGUI текст_Панель2;
        public Button кнопка1_Панель2; // "Хорошо"
        
        [Header("UI - Панель 3 (Вы принесли предмет?)")]
        public GameObject панель3_ВыПринеслиПредмет;
        public TextMeshProUGUI текст_Панель3;
        public Button кнопка1_Панель3; // "Да, вот предмет"
        public Button кнопка2_Панель3; // "Извините обознался"
        
        [Header("UI - Панель 4 (Результат проверки)")]
        public GameObject панель4_РезультатПроверки;
        public TextMeshProUGUI текст_Панель4;
        public Button кнопка1_Панель4; // "Сейчас принесу другой"
        public Button кнопка2_Панель4; // "У меня больше нету предметов"
        
        [Header("UI - Панель 5 (Не нашёл контрабанду)")]
        public GameObject панель5_НеНашелКонтрабанду;
        public TextMeshProUGUI текст_Панель5;
        public Button кнопка1_Панель5; // "Сейчас принесу другой"
        public Button кнопка2_Панель5; // "У меня больше нету предметов"
        
        [Header("UI - Панель 6 (Закрытие)")]
        public GameObject панель6_Закрытие;
        public TextMeshProUGUI текст_Панель6;
        public Button кнопка1_Панель6; // "Закрыть"
        
        [Header("Настройки штрафов")]
        public int contrabandFine = 1000;
        public int falseCallFine = 500;
        
        // События
        public System.Action<int> OnReputationChanged;
        public System.Action<int> OnFinePaid;
        public System.Action OnContrabandHandover;

        // Методы для спавнера
        public void SetTargetPosition(Transform target) { targetPosition = target; }
        public void SetSpawnPoint(Transform spawn) { spawnPoint = spawn; }
        public void SetDoorTrigger(Transform door) { doorTrigger = door; }
        public Transform GetTargetPosition() { return targetPosition; }
        
        void Start()
        {
            InitializePolice();
            SetupNavMeshAgent();
            Debug.Log("[PoliceOfficer] Полицейский создан");
        }

        void SetupNavMeshAgent()
        {
            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                Debug.LogError("[PoliceOfficer] NavMeshAgent не найден! Добавьте NavMeshAgent компонент вручную.");
                return;
            }
            
            // Настройки агента
            navAgent.speed = 3.5f;
            navAgent.angularSpeed = 120f;
            navAgent.acceleration = 8f;
            navAgent.stoppingDistance = stoppingDistance;
            
            // Старт: сначала к триггеру двери. Форсируем, даже если путь ещё строится
            if (doorTrigger != null)
            {
                navAgent.ResetPath();
                navAgent.SetDestination(doorTrigger.position);
            }
            else if (targetPosition != null)
            {
                navAgent.ResetPath();
                navAgent.SetDestination(targetPosition.position);
            }
        }

        void Update()
        {
            if (currentState == PoliceState.Finished) return;

            // Проверка дистанции до игрока
            if (PlayerController.Instance != null)
            {
                float dist = Vector3.Distance(transform.position, PlayerController.Instance.transform.position);
                playerInRange = dist <= interactionDistance;
            }

            // Обработка ESC для закрытия диалога
            if (isInDialog && Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("[PoliceOfficer] ESC нажат - закрываем диалог");
                dialogWasInterrupted = true;
                EndDialog();
                return;
            }

            // Взаимодействие по E
            if (!isInDialog && playerInRange && Input.GetKeyDown(KeyCode.E))
            {
                ActivateDialog();
            }

            HandleMovement();
        }

        void HandleMovement()
        {
            // Проверяем, что агент активен и на NavMesh
            if (navAgent == null || !navAgent.isOnNavMesh) return;
            
            // Пока не достигнут doorTrigger — держим цель на нём
            if (doorTrigger != null && !doorEntryCompleted)
            {
                if (!navAgent.hasPath || navAgent.destination != doorTrigger.position)
                {
                    navAgent.ResetPath();
                    navAgent.SetDestination(doorTrigger.position);
                }
            }

            // Этап двери: сначала до триггера → небольшая пауза → идём к цели
            if (doorTrigger != null && !doorEntryCompleted)
            {
                if (Vector3.Distance(transform.position, doorTrigger.position) <= Mathf.Max(navAgent.stoppingDistance, 0.6f))
                {
                    doorEntryCompleted = true; // сохраняем для выхода
                    StartCoroutine(WaitAndGoToTarget(1f));
                    return;
                }
            }

            // Проверяем, достигли ли цели
            if (navAgent.remainingDistance <= navAgent.stoppingDistance)
            {
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
        
        void InitializePolice()
        {
            // Настройка кнопок панели 1
            if (кнопка1_Панель1) кнопка1_Панель1.onClick.AddListener(OnPanel1Button1);
            if (кнопка2_Панель1) кнопка2_Панель1.onClick.AddListener(OnPanel1Button2);
            
            // Настройка кнопок панели 2
            if (кнопка1_Панель2) кнопка1_Панель2.onClick.AddListener(OnPanel2Button1);
            
            // Настройка кнопок панели 3
            if (кнопка1_Панель3) кнопка1_Панель3.onClick.AddListener(OnPanel3Button1);
            if (кнопка2_Панель3) кнопка2_Панель3.onClick.AddListener(OnPanel3Button2);
            
            // Настройка кнопок панели 4
            if (кнопка1_Панель4) кнопка1_Панель4.onClick.AddListener(OnPanel4Button1);
            
            // Настройка кнопок панели 4
            if (кнопка1_Панель4) кнопка1_Панель4.onClick.AddListener(OnPanel4Button1);
            
            // Настройка кнопок панели 5
            if (кнопка1_Панель5) кнопка1_Панель5.onClick.AddListener(OnPanel5Button1);
            if (кнопка2_Панель5) кнопка2_Панель5.onClick.AddListener(OnPanel5Button2);
            
            // Настройка кнопок панели 6
            if (кнопка1_Панель6) кнопка1_Панель6.onClick.AddListener(OnPanel6Button1);
            
            CloseAllPanels();
        }
        
        private PanelState lastPanelState = PanelState.None;

        public void ActivateDialog()
        {
            if (currentState != PoliceState.Idle && !dialogWasInterrupted) return;
            currentState = PoliceState.InDialog;
            isInDialog = true;
            if (PlayerController.Instance != null) PlayerController.Instance.Block();
            if (CrosshairSystem.Instance != null) CrosshairSystem.Instance.HideCrosshairForDialog();
            if (navAgent != null) navAgent.isStopped = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            // Восстанавливаем последнюю панель
            if (dialogWasInterrupted || lastPanelState != PanelState.None)
            {
                RestoreDialogState();
                Debug.Log("[PoliceOfficer] Диалог восстановлен с панели: " + lastPanelState);
            }
            else
            {
                ShowPanel1();
                Debug.Log("[PoliceOfficer] Диалог начат с панели 1");
            }
            Debug.Log("[PoliceOfficer] Диалог активирован");
        }
        
        public void OpenItemCheckDialog()
        {
            if (currentState != PoliceState.InDialog && currentState != PoliceState.Called) return;
            
            PlayerController.Instance.Block();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            ShowPanel3();
        }
        
        void ShowPanel1()
        {
            CloseAllPanels();
            currentPanelState = PanelState.Panel1;
            
            if (панель1_НачальныйДиалог) панель1_НачальныйДиалог.SetActive(true);
            if (текст_Панель1) текст_Панель1.text = "Здравствуйте, у вас что то случилось?";
        }
        
        void ShowPanel2()
        {
            CloseAllPanels();
            currentPanelState = PanelState.Panel2;
            
            if (панель2_ПринестиКонтрабанду) панель2_ПринестиКонтрабанду.SetActive(true);
            if (текст_Панель2) текст_Панель2.text = "Понятно, у вас есть контрабанда. Принесите её сюда.";
        }
        
        void ShowPanel3()
        {
            CloseAllPanels();
            currentPanelState = PanelState.Panel3;
            
            if (панель3_ВыПринеслиПредмет) панель3_ВыПринеслиПредмет.SetActive(true);
            if (текст_Панель3) текст_Панель3.text = "Вы принесли предмет?";
        }
        
        void ShowPanel4(bool isContraband)
        {
            CloseAllPanels();
            currentPanelState = PanelState.Panel4;
            if (панель4_РезультатПроверки) панель4_РезультатПроверки.SetActive(true);
            if (текст_Панель4)
            {
                if (isContraband)
                    текст_Панель4.text = "Спасибо за работу!";
                else
                    текст_Панель4.text = "Я не нашел контрабанды в этом предмете";
            }
        }
        
        void ShowPanel5()
        {
            CloseAllPanels();
            currentPanelState = PanelState.Panel5;
            
            if (панель5_НеНашелКонтрабанду) панель5_НеНашелКонтрабанду.SetActive(true);
            if (текст_Панель5) текст_Панель5.text = "Я не нашёл контрабанды в этом предмете";
        }
        
        void ShowPanel6()
        {
            CloseAllPanels();
            currentPanelState = PanelState.Panel6;
            
            if (панель6_Закрытие) панель6_Закрытие.SetActive(true);
            if (текст_Панель6) текст_Панель6.text = "В следующий раз будьте внимательнее.";
        }
        
        void CloseAllPanels()
        {
            // Запоминаем последнюю открытую панель
            if (currentPanelState != PanelState.None)
                lastPanelState = currentPanelState;

            if (панель1_НачальныйДиалог) панель1_НачальныйДиалог.SetActive(false);
            if (панель2_ПринестиКонтрабанду) панель2_ПринестиКонтрабанду.SetActive(false);
            if (панель3_ВыПринеслиПредмет) панель3_ВыПринеслиПредмет.SetActive(false);
            if (панель4_РезультатПроверки) панель4_РезультатПроверки.SetActive(false);
            if (панель5_НеНашелКонтрабанду) панель5_НеНашелКонтрабанду.SetActive(false);
            if (панель6_Закрытие) панель6_Закрытие.SetActive(false);
            currentPanelState = PanelState.None;
        }
        
        // Обработчики кнопок панели 1
        void OnPanel1Button1()
        {
            ShowPanel2();
        }
        
        void OnPanel1Button2()
        {
            ShowPanel6();
            StartCoroutine(CloseDialogAfterDelay(2f));
        }
        
        // Обработчики кнопок панели 2
        void OnPanel2Button1()
        {
            ShowPanel3();
        }
        
        // Обработчики кнопок панели 3
        void OnPanel3Button1()
        {
            // Проверяем предмет и показываем соответствующую панель
            Item playerItem = GetPlayerContrabandItem();
            if (playerItem != null)
            {
                if (playerItem.gameObject.CompareTag("contraband") || playerItem.GetComponent<ContrabandItem>() != null)
                {
                    PlayerInventory.Instance.RemoveItem();
                    ChangeReputation(contrabandDetectionPenalty);
                    OnContrabandHandover?.Invoke();
                    ShowPanel4(true); // Спасибо за работу!
                }
                else
                {
                    ShowPanel4(false); // Не нашёл контрабанду
                }
            }
            else
            {
                ShowPanel4(false); // Нет предмета — тоже не нашёл
            }
        }
        
        void OnPanel3Button2()
        {
            ShowPanel6();
            StartCoroutine(CloseDialogAfterDelay(2f));
        }
        
        // Обработчики кнопок панели 4
        void OnPanel4Button1()
        {
            EndDialog();
        }
        
        // Обработчики кнопок панели 5
        void OnPanel5Button1()
        {
            ShowPanel3(); // Возвращаемся к панели 3 для нового предмета
        }
        
        void OnPanel5Button2()
        {
            ShowPanel6();
            StartCoroutine(CloseDialogAfterDelay(2f));
        }
        
        // Обработчики кнопок панели 6
        void OnPanel6Button1()
        {
            EndDialog();
        }
        
        Item GetPlayerContrabandItem()
        {
            return PlayerInventory.Instance.GetCurrentItem();
        }
        
        void ChangeReputation(int amount)
        {
            currentReputation = Mathf.Clamp(currentReputation + amount, minReputation, maxReputation);
            OnReputationChanged?.Invoke(currentReputation);
        }
        
        IEnumerator CloseDialogAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            EndDialog();
        }
        
        void EndDialog()
        {
            CloseAllPanels();
            isInDialog = false;
            
            // Если диалог был прерван, не переходим в состояние Finished
            if (!dialogWasInterrupted)
            {
                currentState = PoliceState.Finished;
            }
            else
            {
                // Если диалог был прерван, возвращаемся в Idle для возможности повторного открытия
                currentState = PoliceState.Idle;
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
                Debug.Log("[PoliceOfficer] Диалог завершен, уходим через дверь к spawn point");
            }
            else
            {
                // Если диалог был прерван, сбрасываем флаг и остаемся на месте
                dialogWasInterrupted = false;
                Debug.Log("[PoliceOfficer] Диалог прерван, остаемся на месте");
            }
        }
        
        void RestoreDialogState()
        {
            // Восстанавливаем именно ту панель, которую закрыли последней
            switch (lastPanelState)
            {
                case PanelState.Panel1:
                    ShowPanel1(); break;
                case PanelState.Panel2:
                    ShowPanel2(); break;
                case PanelState.Panel3:
                    ShowPanel3(); break;
                case PanelState.Panel4:
                    ShowPanel4(false); break; // Default to false for restoration
                case PanelState.Panel5:
                    ShowPanel5(); break;
                case PanelState.Panel6:
                    ShowPanel6(); break;
                default:
                    ShowPanel1(); break;
            }
        }
        
        public bool IsInDialog()
        {
            return isInDialog;
        }
        
        public bool IsWaitingForItem()
        {
            return isWaitingForItem;
        }
        
        public PoliceState GetCurrentState()
        {
            return currentState;
        }
        
        System.Collections.IEnumerator GoToSpawnPoint()
        {
            if (spawnPoint == null)
            {
                Debug.LogWarning("[PoliceOfficer] Exit point (spawnPoint) не задан — не могу уйти");
                yield break;
            }

            // Гарантируем цель
            if (navAgent != null)
            {
                navAgent.ResetPath();
                navAgent.SetDestination(spawnPoint.position);
            }

            // Ждём валидного пути
            while (navAgent != null && navAgent.isOnNavMesh)
            {
                if (navAgent.pathPending)
                {
                    yield return null;
                    continue;
                }

                if (!navAgent.hasPath || navAgent.pathStatus != NavMeshPathStatus.PathComplete)
                {
                    // Путь не построен — пробуем ещё раз через короткую паузу
                    navAgent.ResetPath();
                    navAgent.SetDestination(spawnPoint.position);
                    yield return new WaitForSeconds(0.1f);
                    continue;
                }

                // Двигаемся до точки
                if (navAgent.remainingDistance > Mathf.Max(navAgent.stoppingDistance, despawnThreshold) + 0.05f)
                {
                    yield return null;
                    continue;
                }

                break;
            }

            Debug.Log("[PoliceOfficer] Достиг exit point, пропадаем");
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
                doorEntryCompleted = true;
            }

            // 2) Exit point
            if (spawnPoint != null && navAgent != null)
            {
                navAgent.ResetPath();
                navAgent.SetDestination(spawnPoint.position);
                StartCoroutine(GoToSpawnPoint());
                yield break;
            }

            // Фолбэк: если exit не задан — не удаляемся на триггере
            yield return new WaitForSeconds(1f);
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

        public int GetReputation()
        {
            return currentReputation;
        }
    }
}