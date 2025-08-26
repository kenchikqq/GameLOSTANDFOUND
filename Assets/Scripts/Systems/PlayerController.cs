using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LostAndFound.NPCs;

namespace LostAndFound.Systems
{
    /// <summary>
    /// КОНТРОЛЛЕР ИГРОКА - движение WASD, камера мышью, блокировка управления
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Движение")]
        public float walkSpeed = 5f;
        public float runSpeed = 8f;
        public float jumpForce = 8f;
        public float gravity = -20f;
        
        [Header("Камера")]
        public Camera playerCamera;
        public float mouseSensitivity = 2f;
        public bool invertY = false;
        public float maxLookAngle = 80f;
        
        [Header("Настройки")]
        public bool isBlocked = false;
        
        private CharacterController controller;
        private Vector3 velocity;
        private bool isGrounded;
        private float xRotation = 0f;
        
        // Input переменные
        private Vector2 moveInput;
        private Vector2 mouseInput;
        private bool isRunning;
        private bool jumpInput;
        
        public static PlayerController Instance { get; private set; }
        
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
            controller = GetComponent<CharacterController>();
            
            if (!playerCamera)
            {
                playerCamera = Camera.main;
                if (!playerCamera)
                    playerCamera = FindObjectOfType<Camera>();
            }
            
            // Блокируем курсор
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            Debug.Log("[PlayerController] Инициализация завершена");
        }
        
        void Update()
        {
            if (isBlocked) return;
            
            HandleInput();
            HandleGroundCheck();
            HandleMovement();
            HandleMouseLook();
            HandleInteraction();

            // ESC для разблокировки курсора
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
        
        void HandleInput()
        {
            // Движение WASD
            moveInput.x = Input.GetAxis("Horizontal");
            moveInput.y = Input.GetAxis("Vertical");
            
            // Мышь
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                mouseInput.x = Input.GetAxis("Mouse X");
                mouseInput.y = Input.GetAxis("Mouse Y");
            }
            else
            {
                mouseInput = Vector2.zero;
            }
            
            // Бег
            isRunning = Input.GetKey(KeyCode.LeftShift);
            
            // Прыжок
            jumpInput = Input.GetKeyDown(KeyCode.Space);
        }
        
        void HandleGroundCheck()
        {
            isGrounded = controller.isGrounded;
            
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // Небольшая сила для прилипания к земле
            }
        }
        
        void HandleMovement()
        {
            // Скорость движения
            float currentSpeed = isRunning ? runSpeed : walkSpeed;
            
            // Направление движения
            Vector3 direction = transform.right * moveInput.x + transform.forward * moveInput.y;
            
            // Применяем движение
            controller.Move(direction * currentSpeed * Time.deltaTime);
            
            // Прыжок
            if (jumpInput && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            }
            
            // Гравитация
            velocity.y += gravity * Time.deltaTime;
            
            // Применяем вертикальное движение
            controller.Move(velocity * Time.deltaTime);
        }
        
        void HandleMouseLook()
        {
            if (!playerCamera) return;
            
            // Поворот камеры по Y (вверх/вниз)
            xRotation -= mouseInput.y * mouseSensitivity * (invertY ? -1 : 1);
            xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            
            // Поворот тела по X (влево/вправо)
            transform.Rotate(Vector3.up * mouseInput.x * mouseSensitivity);
        }
        
        void HandleInteraction()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("[PlayerController] Нажата клавиша E");
                
                // Проверяем, наведён ли прицел на интерактивный объект
                if (CrosshairSystem.Instance != null && CrosshairSystem.Instance.CanInteract())
                {
                    GameObject target = CrosshairSystem.Instance.GetCurrentTarget();
                    Debug.Log($"[PlayerController] Найден интерактивный объект: {target?.name}");
                    
                    if (target != null)
                    {
                        // Проверяем NPCs (через родителя, если клик по дочернему коллайдеру)
                        WhiteNPC whiteNPC = target.GetComponentInParent<WhiteNPC>();
                        if (whiteNPC != null)
                        {
                            Debug.Log("[PlayerController] Найден WhiteNPC, запускаю диалог");
                            whiteNPC.StartDialog();
                            return;
                        }
                        
                        BlackNPC blackNPC = target.GetComponentInParent<BlackNPC>();
                        if (blackNPC != null)
                        {
                            Debug.Log("[PlayerController] Найден BlackNPC, запускаю диалог");
                            blackNPC.TryOpenDialog();
                            return;
                        }
                        
                        // Проверяем полицейского
                        PoliceOfficer policeOfficer = target.GetComponentInParent<PoliceOfficer>();
                        if (policeOfficer != null)
                        {
                            Debug.Log("[PlayerController] Найден PoliceOfficer, проверяю состояние");
                            PoliceOfficer.PoliceState policeState = policeOfficer.GetCurrentState();
                            Debug.Log($"[PlayerController] Состояние полицейского: {policeState}, IsInDialog: {policeOfficer.IsInDialog()}");
                            
                            // Активируем диалог если полицейский готов к разговору
                            if (policeState == PoliceOfficer.PoliceState.Called && !policeOfficer.IsInDialog())
                            {
                                Debug.Log("[PlayerController] Активирую диалог с полицейским (Called)");
                                policeOfficer.ActivateDialog();
                            }
                            else if (policeState == PoliceOfficer.PoliceState.Called && policeOfficer.IsWaitingForItem())
                            {
                                Debug.Log("[PlayerController] Открываю диалог проверки предмета (Called + Waiting)");
                                policeOfficer.OpenItemCheckDialog();
                            }
                            else if (policeState == PoliceOfficer.PoliceState.InDialog && !policeOfficer.IsInDialog())
                            {
                                Debug.Log("[PlayerController] Активирую диалог с полицейским (InDialog)");
                                policeOfficer.ActivateDialog();
                            }
                            else if (policeState == PoliceOfficer.PoliceState.InDialog && policeOfficer.IsWaitingForItem())
                            {
                                Debug.Log("[PlayerController] Открываю диалог проверки предмета (InDialog + Waiting)");
                                policeOfficer.OpenItemCheckDialog();
                            }
                            else if (policeState == PoliceOfficer.PoliceState.InDialog && policeOfficer.IsInDialog())
                            {
                                Debug.Log("[PlayerController] Диалог уже активен");
                            }
                            else
                            {
                                Debug.Log($"[PlayerController] Полицейский не готов к диалогу: {policeState}, IsWaitingForItem: {policeOfficer.IsWaitingForItem()}");
                            }
                            return;
                        }
                        
                        // Проверяем кнопку вызова полицейского
                        PoliceCallButton policeButton = target.GetComponent<PoliceCallButton>();
                        if (policeButton != null)
                        {
                            if (policeButton.CanInteract())
                            {
                                policeButton.CallPolice();
                            }
                            return;
                        }
                    }
                }
                
                // Нет интерактивного объекта под прицелом
            }
        }
        
        // === ПУБЛИЧНЫЕ МЕТОДЫ ===
        public void Block()
        {
            isBlocked = true;
            Debug.Log("[PlayerController] Управление заблокировано");
        }
        
        public void Unblock()
        {
            isBlocked = false;
            Debug.Log("[PlayerController] Управление разблокировано");
        }
        
        public void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = sensitivity;
        }
        
        public void SetInvertY(bool invert)
        {
            invertY = invert;
        }
        
        public void TeleportTo(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            velocity = Vector3.zero;
        }
        
        public void SetRotation(Vector3 eulerAngles)
        {
            transform.rotation = Quaternion.Euler(0, eulerAngles.y, 0);
            xRotation = eulerAngles.x;
            if (playerCamera)
                playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        
        // === ГЕТТЕРЫ ===
        public bool IsGrounded() => isGrounded;
        public bool IsRunning() => isRunning && moveInput.magnitude > 0.1f;
        public bool IsMoving() => moveInput.magnitude > 0.1f;
        public Vector3 GetVelocity() => controller.velocity;
        public bool IsBlocked() => isBlocked;
        
        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        
        void OnDrawGizmosSelected()
        {
            // Показываем зону контроллера
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
} 