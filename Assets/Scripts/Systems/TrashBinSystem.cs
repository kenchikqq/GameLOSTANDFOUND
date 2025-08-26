using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace LostAndFound.Systems
{
    /// <summary>
    /// СИСТЕМА МУСОРКИ - утилизация предметов с анимированным прогресс-кругом
    /// Клавиши: X (зажать) = утилизировать предмет из инвентаря
    /// </summary>
    public class TrashBinSystem : MonoBehaviour
    {
        [Header("Настройки мусорки")]
        public float interactionRange = 3f;
        public LayerMask playerLayer = 1;
        public float disposalTime = 3f;
        
        [Header("UI Прогресса - настрой сам")]
        [Tooltip("Canvas для UI прогресса")]
        public Canvas progressCanvas;
        
        [Tooltip("Изображение прогресс-круга (тип Image должен быть Filled)")]
        public Image progressCircle;
        
        [Tooltip("Текст подсказки")]
        public Text hintText;
        
        [Header("Настройки анимации")]
        [Tooltip("Скорость вращения прогресс-круга")]
        public float animationSpeed = 2f;
        
        [Tooltip("Цвет прогресс-круга")]
        public Color progressColor = Color.red;
        
        [Tooltip("Включить отладочные логи")]
        public bool enableDebugLogs = true;
        
        // Внутренние переменные
        private bool isPlayerNearby = false;
        private bool isDisposing = false;
        private float currentDisposalProgress = 0f;
        private Item itemToDispose = null;
        private PlayerController playerController;
        
        // Корутина утилизации
        private Coroutine disposalCoroutine = null;
        
        void Start()
        {
            InitializeTrashBin();
        }
        
        void Update()
        {
            HandlePlayerProximity();
            HandleInput();
            UpdateUI();
        }
        
        void InitializeTrashBin()
        {
            // Скрываем UI прогресса по умолчанию
            if (progressCanvas) progressCanvas.gameObject.SetActive(false);
            
            // Настраиваем прогресс-круг
            if (progressCircle)
            {
                progressCircle.color = progressColor;
                progressCircle.fillAmount = 0f;
                progressCircle.type = Image.Type.Filled;
            }
            
            if (enableDebugLogs)
                Debug.Log("[TrashBinSystem] Система мусорки инициализирована");
        }
        
        void HandlePlayerProximity()
        {
            // Ищем игрока
            GameObject player = GameObject.FindWithTag("Player");
            if (player)
            {
                float distance = Vector3.Distance(player.transform.position, transform.position);
                bool wasNearby = isPlayerNearby;
                isPlayerNearby = distance <= interactionRange;
                
                if (isPlayerNearby && !wasNearby)
                {
                    ShowInteractionHint();
                }
                else if (!isPlayerNearby && wasNearby)
                {
                    HideInteractionHint();
                    CancelDisposal(); // Отменяем утилизацию если игрок ушел
                }
            }
        }
        
        void HandleInput()
        {
            if (!isPlayerNearby) return;
            
            // X - начать/продолжить утилизацию
            if (Input.GetKeyDown(KeyCode.X))
            {
                StartDisposal();
            }
            
            // Отпускание X - отменить утилизацию
            if (Input.GetKeyUp(KeyCode.X))
            {
                CancelDisposal();
            }
        }
        
        void StartDisposal()
        {
            // Проверяем есть ли предмет в инвентаре игрока
            if (PlayerInventory.Instance && PlayerInventory.Instance.HasItem())
            {
                itemToDispose = PlayerInventory.Instance.GetCurrentItem();
                
                if (itemToDispose != null && !isDisposing)
                {
                    isDisposing = true;
                    currentDisposalProgress = 0f;
                    
                    // Показываем UI прогресса
                    if (progressCanvas) progressCanvas.gameObject.SetActive(true);
                    
                    // Запускаем корутину утилизации
                    disposalCoroutine = StartCoroutine(DisposalProcess());
                    
                    if (enableDebugLogs)
                        Debug.Log($"[TrashBinSystem] Начата утилизация предмета: {itemToDispose.itemName}");
                }
            }
            else
            {
                ShowNoItemMessage();
            }
        }
        
        void CancelDisposal()
        {
            if (isDisposing)
            {
                isDisposing = false;
                currentDisposalProgress = 0f;
                itemToDispose = null;
                
                // Останавливаем корутину
                if (disposalCoroutine != null)
                {
                    StopCoroutine(disposalCoroutine);
                    disposalCoroutine = null;
                }
                
                // Скрываем UI прогресса
                if (progressCanvas) progressCanvas.gameObject.SetActive(false);
                
                if (enableDebugLogs)
                    Debug.Log("[TrashBinSystem] Утилизация отменена");
            }
        }
        
        IEnumerator DisposalProcess()
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < disposalTime && Input.GetKey(KeyCode.X))
            {
                elapsedTime += Time.deltaTime;
                currentDisposalProgress = elapsedTime / disposalTime;
                
                // Обновляем прогресс-круг
                if (progressCircle)
                {
                    progressCircle.fillAmount = currentDisposalProgress;
                    
                    // Вращаем прогресс-круг
                    progressCircle.transform.Rotate(0, 0, animationSpeed * Time.deltaTime * 360f);
                }
                
                yield return null;
            }
            
            // Проверяем завершилась ли утилизация успешно
            if (currentDisposalProgress >= 1f && Input.GetKey(KeyCode.X))
            {
                CompleteDisposal();
            }
            else
            {
                CancelDisposal();
            }
        }
        
        void CompleteDisposal()
        {
            if (itemToDispose != null)
            {
                // Убираем предмет из инвентаря игрока
                PlayerInventory.Instance.RemoveItem();
                
                // ПОЛНОСТЬЮ УНИЧТОЖАЕМ предмет
                if (enableDebugLogs)
                    Debug.Log($"[TrashBinSystem] Предмет {itemToDispose.itemName} полностью утилизирован!");
                    
                DestroyImmediate(itemToDispose.gameObject);
                
                // Сбрасываем состояние
                isDisposing = false;
                currentDisposalProgress = 0f;
                itemToDispose = null;
                
                // Скрываем UI с задержкой для показа завершения
                StartCoroutine(HideUIAfterDelay(0.5f));
                
                // Показываем сообщение об успехе
                ShowSuccessMessage();
            }
        }
        
        IEnumerator HideUIAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (progressCanvas) progressCanvas.gameObject.SetActive(false);
        }
        
        void UpdateUI()
        {
            // Обновляем текст подсказки
            if (hintText)
            {
                if (!isPlayerNearby)
                {
                    hintText.text = "";
                }
                else if (isDisposing)
                {
                    hintText.text = $"Утилизация... {(currentDisposalProgress * 100f):F0}%";
                }
                else if (PlayerInventory.Instance && PlayerInventory.Instance.HasItem())
                {
                    hintText.text = "Зажми X для утилизации предмета";
                }
                else
                {
                    hintText.text = "Нет предмета в инвентаре";
                }
            }
        }
        
        void ShowInteractionHint()
        {
            if (enableDebugLogs)
                Debug.Log("[TrashBinSystem] Рядом с мусоркой - X для утилизации");
        }
        
        void HideInteractionHint()
        {
            // Скрываем подсказку
        }
        
        void ShowNoItemMessage()
        {
            if (enableDebugLogs)
                Debug.Log("[TrashBinSystem] Нет предмета для утилизации");
        }
        
        void ShowSuccessMessage()
        {
            if (enableDebugLogs)
                Debug.Log("[TrashBinSystem] Предмет успешно утилизирован!");
        }
        
        // === ПУБЛИЧНЫЕ МЕТОДЫ ===
        
        /// <summary>
        /// Проверяет находится ли игрок рядом с мусоркой
        /// </summary>
        public bool IsPlayerNearby()
        {
            return isPlayerNearby;
        }
        
        /// <summary>
        /// Проверяет идет ли процесс утилизации
        /// </summary>
        public bool IsDisposing()
        {
            return isDisposing;
        }
        
        /// <summary>
        /// Получает прогресс утилизации (0-1)
        /// </summary>
        public float GetDisposalProgress()
        {
            return currentDisposalProgress;
        }
        
        /// <summary>
        /// Принудительно отменяет утилизацию
        /// </summary>
        public void ForceCancel()
        {
            CancelDisposal();
        }
        
        void OnDestroy()
        {
            // Останавливаем все корутины при уничтожении
            if (disposalCoroutine != null)
            {
                StopCoroutine(disposalCoroutine);
            }
        }
        
        void OnDrawGizmosSelected()
        {
            // Показываем радиус взаимодействия
            Gizmos.color = isPlayerNearby ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
            
            // Показываем направление мусорки
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, Vector3.one * 0.3f);
        }
    }
} 