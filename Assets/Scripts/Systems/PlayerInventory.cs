using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LostAndFound.NPCs;

namespace LostAndFound.Systems
{
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Настройки подбора")]
        public float pickupRange = 3f;
        public LayerMask itemLayerMask = -1;
        public Transform dropPosition;
        [Header("Позиция осмотра")]
        public Transform holdPoint;
        [Header("Настройки осмотра")]
        public float rotationSpeed = 100f;
        public float zoomSpeed = 2f;
        public float minZoomDistance = 0.5f;
        public float maxZoomDistance = 3f;
        [Header("UI Инвентаря - настрой сам")]
        public Canvas inventoryCanvas;
        public Image slotImage;
        public TextMeshProUGUI itemNameText;
        [Header("Цвета слота")]
        public Color emptySlotColor = Color.gray;
        public Color filledSlotColor = Color.white;
        private Item currentItem = null;
        private Item nearbyItem = null;
        private WhiteNPC nearbyNPC = null;
        private bool isInspecting = false;
        private PlayerController playerController;
        private Vector3 originalItemPosition;
        private Quaternion originalItemRotation;
        private bool originalItemActiveState;
        private Rigidbody originalRigidbody;
        private bool originalRigidbodyKinematic;
        private bool originalRigidbodyUseGravity;
        private Collider[] originalColliders;
        private bool[] originalCollidersEnabled;
        private float currentZoomDistance = 1.5f;
        private Vector3 baseHoldPosition;
        public System.Action<Item> OnItemPickedUp;
        public System.Action<Item> OnItemDropped;
        public System.Action<Item> OnItemInspected;
        public static PlayerInventory Instance { get; private set; }
        void Awake() { if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); } else { Destroy(gameObject); return; } }
        void Start() { Initialize(); }
        void Initialize() {
            playerController = GetComponent<PlayerController>();
            if (inventoryCanvas) inventoryCanvas.gameObject.SetActive(true);
            UpdateSlotUI();
        }
        void Update() {
            if (isInspecting) { HandleInspectionControls(); return; }
            HandleInput();
            CheckForNearbyItems();
        }
        void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.E)) TryPickupItem();
            if (Input.GetKeyDown(KeyCode.Q)) TryDropItem();
            if (Input.GetKeyDown(KeyCode.F)) TryInspectItem();
        }
        void HandleInspectionControls() {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseInspection();
            // Блокируем поворот предмета, если пользователь тянет крышку кейса
            bool blockRotate = LostAndFound.Systems.InteractiveCaseLid.IsAnyDragging;
            if (!blockRotate && Input.GetMouseButton(0) && currentItem != null) {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");
                currentItem.transform.Rotate(Vector3.up, mouseX * rotationSpeed * Time.deltaTime, Space.World);
                currentItem.transform.Rotate(Vector3.right, -mouseY * rotationSpeed * Time.deltaTime, Space.World);
            }
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f) {
                currentZoomDistance -= scroll * zoomSpeed;
                currentZoomDistance = Mathf.Clamp(currentZoomDistance, minZoomDistance, maxZoomDistance);
                UpdateItemInspectionPosition();
            }
        }
        void CheckForNearbyItems() {
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, pickupRange, itemLayerMask);
            Item closestItem = null; WhiteNPC closestNPC = null; float closestDistance = float.MaxValue;
            foreach (Collider col in nearbyColliders) {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                Item item = col.GetComponent<Item>();
                if (item != null && distance < closestDistance) { closestDistance = distance; closestItem = item; }
                WhiteNPC npc = col.GetComponent<WhiteNPC>();
                if (npc != null && npc.GetCurrentState() == WhiteNPC.NPCState.Idle && distance < closestDistance) { closestDistance = distance; closestNPC = npc; closestItem = null; }
            }
            nearbyItem = closestItem; nearbyNPC = closestNPC;
        }
        void TryPickupItem()
        {
            // Проверяем, есть ли уже предмет в инвентаре
            if (currentItem != null)
            {
                Debug.Log("[PlayerInventory] Инвентарь полон! Сначала выбросьте текущий предмет.");
                return;
            }
            
            // Проверяем, наведён ли прицел на предмет
            if (CrosshairSystem.Instance != null && CrosshairSystem.Instance.CanInteract())
            {
                GameObject target = CrosshairSystem.Instance.GetCurrentTarget();
                if (target != null)
                {
                    var item = target.GetComponent<Item>();
                    if (item != null)
                    {
                        PickupItem(item);
                        return;
                    }
                }
            }
            
            Debug.Log("[PlayerInventory] Нет предмета под прицелом");
        }
        void PickupItem(Item item) {
            currentItem = item;
            item.gameObject.SetActive(false);
            UpdateSlotUI();
            OnItemPickedUp?.Invoke(item);
            item.OnPickup();
        }
        void TryDropItem() { if (currentItem != null && !isInspecting) DropItem(); }
        void DropItem() {
            if (currentItem == null) return;
            Vector3 dropPos = dropPosition ? dropPosition.position : transform.position + transform.forward * 2f;
            currentItem.transform.position = dropPos;
            currentItem.transform.rotation = Quaternion.identity;
            currentItem.gameObject.SetActive(true);
            OnItemDropped?.Invoke(currentItem);
            currentItem.OnDrop();
            currentItem = null;
            UpdateSlotUI();
        }
        void TryInspectItem() { if (currentItem != null && !isInspecting) StartInspection(); }
        void StartInspection() {
            if (currentItem == null) return;
            isInspecting = true;
            if (playerController) playerController.Block();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // Скрываем прицел во время осмотра
            if (CrosshairSystem.Instance != null)
                CrosshairSystem.Instance.HideCrosshairForDialog();
            
            SaveItemOriginalState();
            DisableItemPhysics();
            PlaceItemForInspection();
            OnItemInspected?.Invoke(currentItem);
            currentItem.OnInspect();
        }
        void SaveItemOriginalState() {
            originalItemPosition = currentItem.transform.position;
            originalItemRotation = currentItem.transform.rotation;
            originalItemActiveState = currentItem.gameObject.activeInHierarchy;
        }
        void DisableItemPhysics() {
            if (currentItem == null) return;
            originalRigidbody = currentItem.GetComponent<Rigidbody>();
            if (originalRigidbody != null) {
                originalRigidbodyKinematic = originalRigidbody.isKinematic;
                originalRigidbodyUseGravity = originalRigidbody.useGravity;
                originalRigidbody.isKinematic = true;
                originalRigidbody.useGravity = false;
                originalRigidbody.linearVelocity = Vector3.zero;
                originalRigidbody.angularVelocity = Vector3.zero;
            }
            originalColliders = currentItem.GetComponentsInChildren<Collider>();
            originalCollidersEnabled = new bool[originalColliders.Length];
            int dustLayer = LayerMask.NameToLayer("Dust");
            for (int i = 0; i < originalColliders.Length; i++) {
                Collider col = originalColliders[i];
                originalCollidersEnabled[i] = col.enabled;
                bool keepEnabled = false;
                // Во время осмотра оставляем включённым MeshCollider пылевого оверлея,
                // чтобы луч мог получать UV и стирать пыль
                if (col is MeshCollider) {
                    if (col.gameObject.layer == dustLayer) keepEnabled = true;
                    else if (col.GetComponentInParent<WorldDustPainter>() != null) keepEnabled = true;
                }
                col.enabled = keepEnabled;
            }
        }
        void PlaceItemForInspection() {
            if (holdPoint != null) baseHoldPosition = holdPoint.position;
            else {
                Camera playerCamera = Camera.main;
                if (playerCamera) baseHoldPosition = playerCamera.transform.position + playerCamera.transform.forward * currentZoomDistance;
                else baseHoldPosition = transform.position + transform.forward * currentZoomDistance;
            }
            currentItem.transform.position = baseHoldPosition;
            currentItem.gameObject.SetActive(true);
            currentZoomDistance = 1.5f;
        }
        void UpdateItemInspectionPosition() {
            if (currentItem == null) return;
            Vector3 direction = (baseHoldPosition - Camera.main.transform.position).normalized;
            currentItem.transform.position = Camera.main.transform.position + direction * currentZoomDistance;
        }
        void CloseInspection() {
            if (!isInspecting || currentItem == null) return;
            currentItem.HideCustomUI();
            RestoreItemPhysics();
            RestoreItemOriginalState();
            if (playerController) playerController.Unblock();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // Показываем прицел после осмотра
            if (CrosshairSystem.Instance != null)
                CrosshairSystem.Instance.ShowCrosshairAfterDialog();
            
            isInspecting = false;
        }
        void RestoreItemPhysics() {
            if (currentItem == null) return;
            if (originalRigidbody != null) {
                originalRigidbody.isKinematic = originalRigidbodyKinematic;
                originalRigidbody.useGravity = originalRigidbodyUseGravity;
            }
            if (originalColliders != null && originalCollidersEnabled != null) {
                for (int i = 0; i < originalColliders.Length && i < originalCollidersEnabled.Length; i++)
                    if (originalColliders[i] != null) originalColliders[i].enabled = originalCollidersEnabled[i];
            }
        }
        void RestoreItemOriginalState() { currentItem.gameObject.SetActive(false); }
        void UpdateSlotUI() {
            if (currentItem != null) {
                if (slotImage) slotImage.color = filledSlotColor;
                if (itemNameText) itemNameText.text = currentItem.itemName;
            } else {
                if (slotImage) slotImage.color = emptySlotColor;
                if (itemNameText) itemNameText.text = "";
            }
        }
        public bool HasItem() { return currentItem != null; }
        public Item GetCurrentItem() { return currentItem; }
        public bool AddItem(Item item) { if (currentItem != null) return false; PickupItem(item); return true; }
        public Item RemoveItem() { Item removedItem = currentItem; if (currentItem != null) { currentItem = null; UpdateSlotUI(); } return removedItem; }
        public bool IsInspecting() { return isInspecting; }
        void StartNPCDialog() { if (nearbyNPC != null && nearbyNPC.GetCurrentState() == WhiteNPC.NPCState.Idle) { nearbyNPC.StartDialog(); } }
        public WhiteNPC GetNearbyNPC() { return nearbyNPC; }
        
        // Методы для управления UI инвентаря
        public void HideInventoryUI()
        {
            if (inventoryCanvas != null)
            {
                inventoryCanvas.gameObject.SetActive(false);
                Debug.Log("[PlayerInventory] UI инвентаря скрыт");
            }
        }
        
        public void ShowInventoryUI()
        {
            if (inventoryCanvas != null)
            {
                inventoryCanvas.gameObject.SetActive(true);
                Debug.Log("[PlayerInventory] UI инвентаря показан");
            }
        }
        
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
} 