using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PrimeTween;
using LostAndFound.NPCs;

namespace LostAndFound.Systems
{
    /// <summary>
    /// СИСТЕМА ПРИЦЕЛА - красивые анимации и блокировка взаимодействия без наведения
    /// </summary>
    public class CrosshairSystem : MonoBehaviour
    {
        [Header("UI Прицела")]
        public Image crosshairImage;
        public TextMeshProUGUI interactionText;
        
        [Header("Настройки прицела")]
        public float raycastDistance = 3f;
        public LayerMask interactableLayers = -1;
        public Color normalColor = Color.white;
        public Color highlightColor = Color.green;
        public Color blockedColor = Color.red;
        
        [Header("Настройки пульсации")]
        public float pulseSpeed = 2f;
        public float pulseAmount = 0.3f;
        public bool enablePulse = true;
        
        [Header("Анимации (PrimeTween)")]
        public float fadeInDuration = 0.2f;
        public float fadeOutDuration = 0.3f;
        public float scaleAnimationDuration = 0.15f;
        public float pulseScale = 1.2f;
        
        [Header("Тексты взаимодействия")]
        public string pickupText = "Подобрать [E]";
        public string talkText = "Поговорить [E]";
        public string inspectText = "Осмотреть [F]";
        
        private Camera playerCamera;
        private bool isCrosshairVisible = false;
        private bool canInteract = false;
        private GameObject currentTarget = null;
        private string currentInteractionText = "";
        
        // PrimeTween анимации
        private Tween crosshairFadeTween;
        private Tween crosshairScaleTween;
        private Tween textFadeTween;
        
        public static CrosshairSystem Instance { get; private set; }
        
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
            }
        }
        
        void Start()
        {
            Initialize();
        }
        
        void Initialize()
        {
            playerCamera = Camera.main;
            if (!playerCamera)
                playerCamera = FindObjectOfType<Camera>();
                
            if (crosshairImage)
            {
                crosshairImage.color = normalColor;
                crosshairImage.enabled = true; // Прицел всегда виден
                isCrosshairVisible = true; // Устанавливаем как видимый
                Debug.Log("[CrosshairSystem] Прицел найден и активирован");
            }
            else
            {
                Debug.LogError("[CrosshairSystem] crosshairImage не назначен!");
            }
                
            if (interactionText)
            {
                interactionText.text = "";
                Debug.Log("[CrosshairSystem] Текст взаимодействия найден");
            }
            else
            {
                Debug.LogWarning("[CrosshairSystem] interactionText не назначен!");
            }
                
            Debug.Log("[CrosshairSystem] Система прицела инициализирована");
        }
        
        void Update()
        {
            // Во время осмотра предмета не показываем подсказки и не логируем ошибки "инвентарь полон"
            if (PlayerInventory.Instance != null && PlayerInventory.Instance.IsInspecting())
            {
                canInteract = false;
                currentTarget = null;
                if (interactionText) interactionText.text = "";
                if (crosshairImage) crosshairImage.color = normalColor;
                return;
            }
            HandleCrosshairRaycast();
            if (enablePulse)
                UpdateCrosshairPulse();
        }
        
        void UpdateCrosshairPulse()
        {
            if (crosshairImage && isCrosshairVisible && canInteract) // Пульсация только при наведении на интерактивные объекты
            {
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                crosshairImage.transform.localScale = Vector3.one * pulse;
            }
            else if (crosshairImage)
            {
                // Возвращаем нормальный размер когда не наведено
                crosshairImage.transform.localScale = Vector3.one;
            }
        }
        
        void HandleCrosshairRaycast()
        {
            if (!playerCamera) 
            {
                Debug.LogWarning("[CrosshairSystem] playerCamera не найден!");
                return;
            }
            
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, raycastDistance, interactableLayers))
            {
                GameObject hitObject = hit.collider.gameObject;
                
                // Проверяем, можно ли взаимодействовать с объектом
                bool isInteractable = IsInteractable(hitObject);
                string interactionText = GetInteractionText(hitObject);
                
                if (isInteractable && hitObject != currentTarget)
                {
                    // Новый интерактивный объект
                    ShowCrosshair(hitObject, interactionText, true);
                    Debug.Log($"[CrosshairSystem] Наведен на интерактивный объект: {hitObject.name}");
                }
                else if (!isInteractable && hitObject != currentTarget)
                {
                    // Неинтерактивный объект
                    ShowCrosshair(hitObject, "", false);
                }
                else if (hitObject == currentTarget)
                {
                    // Тот же объект - проверяем, не изменился ли статус интерактивности
                    if (isInteractable != canInteract)
                    {
                        ShowCrosshair(hitObject, interactionText, isInteractable);
                    }
                    else if (interactionText != currentInteractionText)
                    {
                        UpdateInteractionText(interactionText);
                    }
                }
            }
            else
            {
                // Ничего не наведено - сбрасываем состояние
                if (currentTarget != null || canInteract)
                {
                    ShowNormalCrosshair();
                }
            }
        }
        
        bool IsInteractable(GameObject obj)
        {
            // Проверяем компоненты для взаимодействия (учитываем попадание в дочерний коллайдер)
            bool hasItem = obj.GetComponentInParent<Item>() != null;
            bool hasWhiteNPC = obj.GetComponentInParent<WhiteNPC>() != null;
            bool hasBlackNPC = obj.GetComponentInParent<BlackNPC>() != null;
            bool hasPoliceCallButton = obj.GetComponentInParent<PoliceCallButton>() != null;
            bool hasTrashBin = obj.GetComponentInParent<TrashBinSystem>() != null;
            bool hasWarehouse = obj.GetComponentInParent<WarehouseSystem>() != null;
            bool hasComputerSystem = obj.GetComponentInParent<ComputerSystem>() != null;
            
            // Проверяем полицейского отдельно - он интерактивен только когда готов к диалогу
            bool hasPoliceOfficer = false;
            PoliceOfficer officer = obj.GetComponentInParent<PoliceOfficer>();
            if (officer != null)
            {
                PoliceOfficer.PoliceState state = officer.GetCurrentState();
                hasPoliceOfficer = (state == PoliceOfficer.PoliceState.Called || state == PoliceOfficer.PoliceState.InDialog);
            }
            
            // Проверяем предметы отдельно - они не интерактивны если инвентарь полон
            if (hasItem)
            {
                bool inventoryFull = PlayerInventory.Instance != null && PlayerInventory.Instance.HasItem();
                // Во время осмотра не спамим логами
                if (inventoryFull)
                {
                    return false;
                }
            }
            
            bool isInteractable = hasItem || hasWhiteNPC || hasBlackNPC || hasPoliceOfficer || 
                                 hasPoliceCallButton || hasTrashBin || hasWarehouse || hasComputerSystem;
            
            return isInteractable;
        }
        
        string GetInteractionText(GameObject obj)
        {
            if (obj.GetComponent<Item>() != null)
            {
                // Проверяем, не полон ли инвентарь
                bool inventoryFull = PlayerInventory.Instance != null && PlayerInventory.Instance.HasItem();
                if (inventoryFull)
                {
                    return ""; // Не показываем текст подбора если инвентарь полон
                }
                return pickupText;
            }
            else if (obj.GetComponent<WhiteNPC>() != null || obj.GetComponent<BlackNPC>() != null)
                return talkText;
            else if (obj.GetComponent<PoliceOfficer>() != null)
            {
                PoliceOfficer officer = obj.GetComponent<PoliceOfficer>();
                PoliceOfficer.PoliceState state = officer.GetCurrentState();
                
                // Показываем текст только если полицейский готов к диалогу
                if (state == PoliceOfficer.PoliceState.Called || state == PoliceOfficer.PoliceState.InDialog)
                    return talkText;
                else
                    return ""; // Не показываем текст если полицейский не готов
            }
            else if (obj.GetComponent<PoliceCallButton>() != null)
            {
                PoliceCallButton button = obj.GetComponent<PoliceCallButton>();
                return button.CanInteract() ? button.GetInteractionText() : "";
            }
            else if (obj.GetComponent<TrashBinSystem>() != null || obj.GetComponent<WarehouseSystem>() != null)
                return inspectText;
            else if (obj.GetComponent<ComputerSystem>() != null)
                return "Открыть компьютер [E]";
            else
                return "";
        }
        
        void ShowCrosshair(GameObject target, string text, bool canInteract)
        {
            currentTarget = target;
            currentInteractionText = text;
            this.canInteract = canInteract;
            
            if (!isCrosshairVisible)
            {
                // Анимация появления только цвета
                if (crosshairImage)
                {
                    crosshairImage.color = new Color(crosshairImage.color.r, crosshairImage.color.g, crosshairImage.color.b, 0f);
                    crosshairFadeTween = Tween.Alpha(crosshairImage, 1f, fadeInDuration);
                    crosshairScaleTween = Tween.Scale(crosshairImage.transform, Vector3.one * pulseScale, scaleAnimationDuration, Ease.OutBack);
                }
                
                isCrosshairVisible = true;
            }
            
            // Обновляем цвет и текст
            UpdateCrosshairAppearance();
        }
        
        void UpdateCrosshairAppearance()
        {
            if (!crosshairImage) return;
            
            Color targetColor = canInteract ? highlightColor : blockedColor;
            crosshairImage.color = targetColor;
            
            if (interactionText)
            {
                interactionText.text = currentInteractionText;
                if (textFadeTween.IsAlive)
                    textFadeTween.Stop();
                textFadeTween = Tween.Alpha(interactionText, 1f, fadeInDuration);
            }
        }
        
        void UpdateInteractionText(string newText)
        {
            currentInteractionText = newText;
            if (interactionText)
            {
                interactionText.text = newText;
            }
        }
        
        public void HideCrosshair()
        {
            if (crosshairImage != null)
                crosshairImage.enabled = false;
            if (interactionText != null)
                interactionText.text = "";
            isCrosshairVisible = false;
            currentTarget = null;
            canInteract = false;
        }
        
        public void ShowCrosshair()
        {
            if (crosshairImage != null)
            {
                crosshairImage.enabled = true;
                crosshairImage.color = normalColor;
                crosshairImage.transform.localScale = Vector3.one;
            }
            if (interactionText != null)
                interactionText.text = "";
            isCrosshairVisible = true;
            currentTarget = null;
            canInteract = false;
            currentInteractionText = "";
            Debug.Log("[CrosshairSystem] Прицел показан");
        }
        
        void ShowNormalCrosshair()
        {
            if (crosshairImage != null)
            {
                crosshairImage.enabled = true;
                crosshairImage.color = normalColor;
                crosshairImage.transform.localScale = Vector3.one;
            }
            if (interactionText != null)
                interactionText.text = "";
            isCrosshairVisible = true;
            currentTarget = null;
            canInteract = false;
            currentInteractionText = "";
        }
        
        // Публичные методы для внешнего доступа
        public bool IsCrosshairVisible() => isCrosshairVisible;
        public bool CanInteract() 
        {
            Debug.Log($"[CrosshairSystem] CanInteract() вызван: canInteract={canInteract}, currentTarget={currentTarget?.name}");
            return canInteract;
        }
        public GameObject GetCurrentTarget() => currentTarget;
        
        public void ForceHideCrosshair()
        {
            HideCrosshair();
        }
        
        public void HideCrosshairForDialog()
        {
            if (crosshairImage)
            {
                crosshairImage.enabled = false;
            }
            if (interactionText)
            {
                interactionText.text = "";
            }
            Debug.Log("[CrosshairSystem] Прицел скрыт для диалога");
        }
        
        public void ShowCrosshairAfterDialog()
        {
            if (crosshairImage)
            {
                crosshairImage.enabled = true;
                crosshairImage.color = normalColor;
            }
            if (interactionText)
            {
                interactionText.text = "";
            }
            isCrosshairVisible = true;
            currentTarget = null;
            canInteract = false;
            currentInteractionText = "";
            Debug.Log("[CrosshairSystem] Прицел восстановлен после диалога");
        }
        
        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
} 