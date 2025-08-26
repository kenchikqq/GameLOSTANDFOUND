using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AI;
using LostAndFound.Systems;
using System.Collections.Generic;

namespace LostAndFound.NPCs
{
    public class WhiteNPC : MonoBehaviour
    {
        public enum NPCState { Idle, InDialog, Finished }
        public enum DialogState { Greeting, Accept, Reject, Closed }

        private NPCState npcState = NPCState.Idle;
        private DialogState currentDialogState = DialogState.Closed;
        private bool dialogWasInterrupted = false;

        [Header("Предмет")]
        public Item broughtItem; // Предмет, который принес NPC (невидимый)
        public string itemDescription = "Найденный предмет"; // Описание предмета
        private Item itemTemplate; // Шаблон предмета для создания рабочей копии

        [Header("NavMesh")]
        private NavMeshAgent navAgent;
        private Transform targetPosition; // Задается спавнером автоматически
        private Transform spawnPoint; // Задается спавнером автоматически
        private Transform doorTrigger; // Промежуточная точка у двери
        private bool doorEntryCompleted = false; // прошли дверь на входе
        public float stoppingDistance = 2f;
        [Tooltip("На каком расстоянии от точки возврата NPC считается прибывшим и исчезает")] public float despawnThreshold = 0.3f;

        [Header("Система очереди")]
        private int queuePosition = -1; // Позиция в очереди (-1 = не в очереди)
        private Vector3 targetQueuePosition; // Конкретная позиция в очереди
        private bool isInQueue = false;

        [Header("Система расстояний")]
        public float minDistanceToOtherNPCs = 2f; // Минимальное расстояние до других NPCs
        private bool isStoppedByDistance = false;

        [Header("Анимация")]
        private Animator animator;
        private bool isMoving = false;
        public string walkAnimationName = "Walk";
        public string idleAnimationName = "Idle";

        [Header("UI - Диалог (упрощенный)")]
        public GameObject dialogPanel;            // Основная панель диалога
        public TextMeshProUGUI dialogText;        // Единое поле текста
        public Button acceptButton;               // "Принять"
        public Button rejectButton;               // "Отказать"

        [Header("Фразы")]
        [TextArea] public string greetingPhrase = "Здравствуйте! Я нашел предмет. Примете его?";
        [TextArea] public string acceptedPhrase = "Спасибо! До свидания.";
        [TextArea] public string rejectedPhrase = "Очень жаль. До свидания.";

        private bool isInDialog = false;
        public System.Action<WhiteNPC> OnNPCFinished;

        // Методы для спавнера
        public void SetTargetPosition(Transform target) { targetPosition = target; }
        public void SetSpawnPoint(Transform spawn) { spawnPoint = spawn; }
        public void SetDoorTrigger(Transform door) { doorTrigger = door; }
        public Transform GetTargetPosition() { return targetPosition; }
        public void SetQueuePosition(int position) { queuePosition = position; }
        public int GetQueuePosition() { return queuePosition; }

        // Методы для управления предметом
        public void HoldItem(Item item)
        {
            if (item == null) return;
            
            // Сохраняем шаблон предмета (невидимый)
            itemTemplate = item;
            broughtItem = item;
            
            Debug.Log($"[WhiteNPC] Теперь ношу невидимый предмет: {broughtItem?.itemName}");
        }

        public void GiveItemToPlayer()
        {
            if (itemTemplate != null)
            {
                // Создаем полноценный рабочий предмет в мире
                GameObject newItemObject = Instantiate(itemTemplate.gameObject);
                Item newItem = newItemObject.GetComponent<Item>();
                
                // Инициализируем предмет
                if (newItem != null)
                {
                    newItem.InitializeItem();
                    
                    // Добавляем в инвентарь игрока
                    bool added = PlayerInventory.Instance.AddItem(newItem);
                    if (added)
                    {
                        Debug.Log($"[WhiteNPC] Передал рабочий предмет игроку: {newItem.itemName}");
                    }
                    else
                    {
                        Debug.Log("[WhiteNPC] Не удалось передать предмет игроку (инвентарь полный)");
                        // Если инвентарь полный, выбрасываем предмет в мире
                        newItemObject.transform.position = transform.position + transform.forward * 2f;
                    }
                }
                
                // Очищаем ссылки на невидимый предмет
                broughtItem = null;
                itemTemplate = null;
            }
        }

        public void ClearHeldItem()
        {
            // Очищаем невидимый предмет
            broughtItem = null;
            itemTemplate = null;
            Debug.Log("[WhiteNPC] Невидимый предмет очищен");
        }

        void Start()
        {
            Debug.Log("[WhiteNPC] NPC создан");
            SetupAnimator();
            SetupNavMeshAgent();
            SetupDialogUI();
            StartCoroutine(CheckDistanceToOtherNPCs());
        }

        void SetupAnimator()
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("[WhiteNPC] Animator не найден! Добавьте Animator компонент вручную.");
            }
            else
            {
                Debug.Log("[WhiteNPC] Animator найден и настроен");
            }
        }

        void SetWalkingAnimation(bool isWalking)
        {
            if (animator != null)
            {
                animator.SetBool(walkAnimationName, isWalking);
                animator.SetBool(idleAnimationName, !isWalking);
                isMoving = isWalking;
                
                // Убираем спам-логи которые вызываются каждый кадр
                // if (isWalking)
                //     Debug.Log("[WhiteNPC] Включаю анимацию ходьбы");
                // else
                //     Debug.Log("[WhiteNPC] Включаю анимацию ожидания");
            }
        }

        void SetupNavMeshAgent()
        {
            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                Debug.LogError("[WhiteNPC] NavMeshAgent не найден! Добавьте NavMeshAgent компонент вручную.");
                return;
            }
            
            // Настройки агента
            navAgent.speed = 3.5f;
            navAgent.angularSpeed = 120f;
            navAgent.acceleration = 8f;
            navAgent.stoppingDistance = stoppingDistance;
            
            // Начинаем движение: сначала к триггеру двери, затем к цели
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
                Debug.Log("[WhiteNPC] ESC нажат - закрываем диалог");
                dialogWasInterrupted = true;
                EndDialog();
                return;
            }

            HandleMovement();
        }

        void HandleMovement()
        {
            // Проверяем, что агент активен и на NavMesh
            if (navAgent == null || !navAgent.isOnNavMesh) 
            {
                SetWalkingAnimation(false);
                return;
            }
            
            // Если остановлен из-за расстояния, не двигаемся
            if (isStoppedByDistance) 
            {
                SetWalkingAnimation(false);
                return;
            }
            
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
                    // Debug.Log($"[WhiteNPC] Target point не имеет forward, использую Vector3.forward");
                }
                
                float queueSpacing = 2f; // Расстояние между NPCs в очереди
                
                targetQueuePosition = queueStart + (queueDirection * queueSpacing * queuePosition);
                
                // Идем к позиции в очереди
                if (Vector3.Distance(transform.position, targetQueuePosition) > 0.3f)
                {
                    navAgent.SetDestination(targetQueuePosition);
                    SetWalkingAnimation(true);
                    // Debug.Log($"[WhiteNPC] Иду к позиции в очереди {queuePosition}: {targetQueuePosition}");
                }
                else
                {
                    // Поворачиваемся к стойке
                    Vector3 direction = (targetPosition.position - transform.position).normalized;
                    if (direction != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
                    }
                    
                    SetWalkingAnimation(false);
                    // Debug.Log($"[WhiteNPC] Встал на позицию в очереди {queuePosition}");
                }
                return;
            }
            
            // Этап 1: доходим до триггера двери → ждём 1 секунду и ставим цель на стойку
            if (doorTrigger != null && !doorEntryCompleted)
            {
                if (Vector3.Distance(transform.position, doorTrigger.position) <= Mathf.Max(navAgent.stoppingDistance, 0.6f))
                {
                    doorEntryCompleted = true; // проход на входе завершён, но ссылку сохраняем для выхода
                    StartCoroutine(WaitAndGoToTarget(1f));
                    return;
                }
            }

            // Проверяем, достигли ли цели
            if (navAgent.remainingDistance <= navAgent.stoppingDistance)
            {
                // Встаем в очередь
                if (!isInQueue && targetPosition != null)
                {
                    isInQueue = true;
                    SetWalkingAnimation(false);
                    Debug.Log($"[WhiteNPC] Достиг target point, встаю в очередь на позицию {queuePosition}");
                }
                return;
            }
            
            // Двигаемся: приоритет — doorTrigger, затем целевая стойка
            if (doorTrigger != null && !doorEntryCompleted)
            {
                navAgent.SetDestination(doorTrigger.position);
                SetWalkingAnimation(true);
            }
            else if (targetPosition != null)
            {
                navAgent.SetDestination(targetPosition.position);
                SetWalkingAnimation(true);
            }
        }

        System.Collections.IEnumerator CheckDistanceToOtherNPCs()
        {
            while (npcState != NPCState.Finished)
            {
                if (npcState == NPCState.Idle && navAgent != null)
                {
                    bool shouldStop = false;
                    
                    // Находим всех белых NPCs в сцене
                    WhiteNPC[] allWhiteNPCs = FindObjectsOfType<WhiteNPC>();
                    
                    foreach (WhiteNPC otherNPC in allWhiteNPCs)
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
                    
                    // Проверяем также черных NPCs
                    BlackNPC[] allBlackNPCs = FindObjectsOfType<BlackNPC>();
                    foreach (BlackNPC otherNPC in allBlackNPCs)
                    {
                        if (otherNPC != null && otherNPC.GetCurrentState() != BlackNPC.NPCState.Finished)
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
                        // Debug.Log($"[WhiteNPC] Остановлен из-за близости к другому NPC");
                    }
                    else if (!shouldStop && isStoppedByDistance)
                    {
                        navAgent.isStopped = false;
                        isStoppedByDistance = false;
                        // Debug.Log($"[WhiteNPC] Возобновлено движение");
                    }
                }
                
                yield return new WaitForSeconds(0.5f);
            }
        }

        void SetupDialogUI()
        {
            if (acceptButton) acceptButton.onClick.RemoveAllListeners();
            if (rejectButton) rejectButton.onClick.RemoveAllListeners();

            if (acceptButton) acceptButton.onClick.AddListener(AcceptItem);
            if (rejectButton) rejectButton.onClick.AddListener(RejectItem);
            if (dialogPanel) dialogPanel.SetActive(false);
        }

        void HideAllTexts() { }

        void ShowGreetingText()
        {
            if (dialogText) dialogText.text = greetingPhrase;
            currentDialogState = DialogState.Greeting;
        }

        void ShowAcceptText()
        {
            if (dialogText) dialogText.text = acceptedPhrase;
            currentDialogState = DialogState.Accept;
        }

        void ShowRejectText()
        {
            if (dialogText) dialogText.text = rejectedPhrase;
            currentDialogState = DialogState.Reject;
        }

        void RestoreDialogState()
        {
            switch (currentDialogState)
            {
                case DialogState.Greeting:
                    ShowGreetingText();
                    break;
                case DialogState.Accept:
                    ShowAcceptText();
                    break;
                case DialogState.Reject:
                    ShowRejectText();
                    break;
                default:
                    ShowGreetingText();
                    break;
            }
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

            // Останавливаем движение и анимацию
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = true;
            }
            SetWalkingAnimation(false);

            // Показываем курсор
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Показываем диалог
            if (dialogPanel) dialogPanel.SetActive(true);

            // Восстанавливаем состояние диалога или показываем приветствие
            if (dialogWasInterrupted)
            {
                RestoreDialogState();
                Debug.Log("[WhiteNPC] Диалог восстановлен с состояния: " + currentDialogState);
            }
            else
            {
                ShowGreetingText();
                currentDialogState = DialogState.Greeting;
                Debug.Log("[WhiteNPC] Диалог начат с приветствия");
            }

            // Проверяем, есть ли уже предмет у игрока и деактивируем кнопку Accept
            if (acceptButton != null)
            {
                bool playerHasItem = PlayerInventory.Instance != null && PlayerInventory.Instance.HasItem();
                acceptButton.interactable = !playerHasItem;
                
                if (playerHasItem)
                {
                    Debug.Log("[WhiteNPC] У игрока уже есть предмет - кнопка Accept деактивирована");
                }
            }

            Debug.Log("[WhiteNPC] Диалог начат");
        }

        public void TryOpenDialog()
        {
            if ((npcState == NPCState.Idle || dialogWasInterrupted) && !isInDialog)
            {
                StartDialog();
            }
        }

        void AcceptItem()
        {
            Debug.Log("[WhiteNPC] Кнопка AcceptItem нажата - принимаем предмет у NPC");
            
            // Проверяем, есть ли уже предмет у игрока
            if (PlayerInventory.Instance != null && PlayerInventory.Instance.HasItem())
            {
                Debug.Log("[WhiteNPC] У игрока уже есть предмет в инвентаре! Нельзя взять еще один.");
                ShowRejectText();
                StartCoroutine(CloseDialogAfterDelay(2f));
                return;
            }
            
            // Передаем предмет от NPC к игроку
            if (broughtItem != null)
            {
                GiveItemToPlayer();
                ShowAcceptText();
                Debug.Log("[WhiteNPC] Предмет успешно передан игроку");
            }
            else
            {
                // Если невидимого предмета нет, пробуем взять случайный из Resources/Items
                Item fallback = GetRandomResourceItem();
                if (fallback != null)
                {
                    itemTemplate = fallback;
                    broughtItem = fallback;
                    GiveItemToPlayer();
                    ShowAcceptText();
                    Debug.Log("[WhiteNPC] Использован предмет из Resources/Items");
                }
                else
                {
                    ShowRejectText();
                    Debug.LogError("[WhiteNPC] У NPC нет предмета для сдачи и ничего не найдено в Resources/Items!");
                }
            }
            
            StartCoroutine(CloseDialogAfterDelay(2f));
        }

        void RejectItem()
        {
            Debug.Log("[WhiteNPC] Кнопка RejectItem нажата - отказываемся от предмета NPC");
            
            // Отказываемся от предмета NPC
            ShowRejectText();
            Debug.Log("[WhiteNPC] Отказались принять предмет у NPC");
            StartCoroutine(CloseDialogAfterDelay(2f));
        }

        System.Collections.IEnumerator CloseDialogAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            EndDialog();
        }

        void EndDialog()
        {
            if (dialogPanel) dialogPanel.SetActive(false);
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
            
            // Очищаем предмет если он не был передан
            if (broughtItem != null)
            {
                ClearHeldItem();
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
            
            // Если диалог не был прерван, идем к spawn point и исчезаем
            if (!dialogWasInterrupted)
            {
                // Возобновляем движение и уходим через дверь к exit point
                if (navAgent != null && navAgent.isOnNavMesh)
                {
                    navAgent.isStopped = false;
                    SetWalkingAnimation(true);
                    StartCoroutine(ExitThroughDoorAndDespawn());
                }
                else
                {
                    Debug.LogError("[WhiteNPC] NavMeshAgent недоступен при завершении диалога");
                }
            }
            else
            {
                // Если диалог был прерван, сбрасываем флаг и остаемся на месте
                dialogWasInterrupted = false;
                // Debug.Log("[WhiteNPC] Диалог прерван, остаемся на месте");
            }
        }

        Item GetRandomResourceItem()
        {
            Item[] all = Resources.LoadAll<Item>("Items");
            if (all != null && all.Length > 0)
                return all[Random.Range(0, all.Length)];
            return null;
        }

        System.Collections.IEnumerator GoToSpawnPoint()
        {
            // Debug.Log("[WhiteNPC] Начинаем движение к exit point");
            
            // Ждем пока агент будет на NavMesh
            while (navAgent != null && !navAgent.isOnNavMesh)
            {
                yield return null;
            }
            
            // Ждем прокладки пути
            while (navAgent != null && navAgent.isOnNavMesh && navAgent.pathPending)
                yield return null;

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
            
            // Останавливаем анимацию перед исчезновением
            SetWalkingAnimation(false);
            
            // Пропадаем только когда дошли
            // Debug.Log("[WhiteNPC] Достиг exit point, пропадаем");
            OnNPCFinished?.Invoke(this);
            Destroy(gameObject);
        }

        System.Collections.IEnumerator ExitThroughDoorAndDespawn()
        {
            // 1) Идём к doorTrigger, если он задан
            if (doorTrigger != null && navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.ResetPath();
                navAgent.SetDestination(doorTrigger.position);
                while (navAgent.pathPending) yield return null;
                while (navAgent.isOnNavMesh && navAgent.remainingDistance > Mathf.Max(navAgent.stoppingDistance, 0.6f))
                    yield return null;
                // Пауза для открытия двери
                navAgent.isStopped = true; SetWalkingAnimation(false);
                yield return new WaitForSeconds(0.5f);
                navAgent.isStopped = false; SetWalkingAnimation(true);
                // Отпускаем триггер, чтобы больше не целиться обратно в него
                doorEntryCompleted = true;
            }

            // 2) Идём к exitPoint
            if (spawnPoint != null && navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.ResetPath();
                navAgent.SetDestination(spawnPoint.position);
                StartCoroutine(GoToSpawnPoint());
                yield break;
            }

            // Если exitPoint не задан — удаляемся сразу
            OnNPCFinished?.Invoke(this);
            Destroy(gameObject);
        }

        System.Collections.IEnumerator WaitAndGoToTarget(float delay)
        {
            navAgent.isStopped = true;
            SetWalkingAnimation(false);
            yield return new WaitForSeconds(delay);
            navAgent.isStopped = false;
            if (targetPosition != null)
            {
                navAgent.SetDestination(targetPosition.position);
                SetWalkingAnimation(true);
            }
        }

        public bool IsInDialog()
        {
            return isInDialog;
        }

        public NPCState GetCurrentState()
        {
            return npcState;
        }
    }
} 