using UnityEngine;
using UnityEngine.AI;
using LostAndFound.NPCs;

namespace LostAndFound.Systems
{
    /// <summary>
    /// КНОПКА ВЫЗОВА ПОЛИЦЕЙСКОГО - повесить на любой объект для вызова полицейского по E
    /// </summary>
    public class PoliceCallButton : MonoBehaviour
    {
        [Header("Настройки кнопки")]
        public KeyCode callKey = KeyCode.E;
        public float interactionDistance = 3f;
        public bool showDebugInfo = true;
        
        [Header("Текст взаимодействия")]
        [TextArea(1, 2)] public string interactionText = "Вызвать полицейского [E]";
        
        [Header("Префаб полицейского")]
        public GameObject policeOfficerPrefab;
        
        private bool playerInRange = false;
        
        void Start()
        {
            if (policeOfficerPrefab == null)
            {
                Debug.LogError("[PoliceCallButton] Не задан префаб полицейского!");
                Debug.Log("[PoliceCallButton] Создаю автоматический префаб полицейского...");
                CreatePoliceOfficerPrefab();
            }
        }
        
        void CreatePoliceOfficerPrefab()
        {
            // Создаем GameObject для полицейского
            GameObject policeObject = new GameObject("PoliceOfficer");
            
            // Добавляем необходимые компоненты
            policeObject.AddComponent<PoliceOfficer>();
            policeObject.AddComponent<NavMeshAgent>();
            
            // Настраиваем NavMeshAgent
            NavMeshAgent navAgent = policeObject.GetComponent<NavMeshAgent>();
            if (navAgent != null)
            {
                navAgent.speed = 3.5f;
                navAgent.angularSpeed = 120f;
                navAgent.acceleration = 8f;
                navAgent.stoppingDistance = 2f;
            }
            
            // Создаем визуальную модель (простой куб)
            GameObject visualModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualModel.transform.SetParent(policeObject.transform);
            visualModel.transform.localPosition = Vector3.zero;
            visualModel.transform.localScale = new Vector3(0.5f, 1.8f, 0.3f);
            
            // Меняем цвет на синий (полицейский)
            Renderer renderer = visualModel.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.blue;
            }
            
            // Убираем коллайдер у визуальной модели
            Collider visualCollider = visualModel.GetComponent<Collider>();
            if (visualCollider != null)
            {
                DestroyImmediate(visualCollider);
            }
            
            // Добавляем коллайдер к основному объекту
            CapsuleCollider mainCollider = policeObject.AddComponent<CapsuleCollider>();
            mainCollider.height = 1.8f;
            mainCollider.radius = 0.3f;
            mainCollider.center = Vector3.up * 0.9f;
            
            // Назначаем созданный объект как префаб
            policeOfficerPrefab = policeObject;
            
            Debug.Log("[PoliceCallButton] Автоматический префаб полицейского создан!");
        }
        
        void Update()
        {
            CheckPlayerDistance();
        }
        
        void CheckPlayerDistance()
        {
            if (PlayerController.Instance == null) return;
            
            float distance = Vector3.Distance(transform.position, PlayerController.Instance.transform.position);
            bool wasInRange = playerInRange;
            playerInRange = distance <= interactionDistance;
            
            // Обновляем текст прицела если игрок вошёл/вышел из зоны
            if (playerInRange != wasInRange && CrosshairSystem.Instance != null)
            {
                if (playerInRange)
                {
                    // Игрок вошёл в зону - показываем текст
                    if (showDebugInfo)
                        Debug.Log("[PoliceCallButton] Игрок в зоне вызова полицейского");
                }
                else
                {
                    // Игрок вышел из зоны
                    if (showDebugInfo)
                        Debug.Log("[PoliceCallButton] Игрок вышел из зоны вызова полицейского");
                }
            }
        }
        
        public void CallPolice()
        {
            Debug.Log("[PoliceCallButton] Попытка вызвать полицейского...");

            // Находим SimpleNPCSpawner
            SimpleNPCSpawner spawner = FindObjectOfType<SimpleNPCSpawner>();
            if (spawner == null)
            {
                Debug.LogError("[PoliceCallButton] Не найден SimpleNPCSpawner!");
                return;
            }

            // Проверяем настройки полицейского
            if (spawner.policeOfficerSettings.spawnPoint == null || spawner.policeOfficerSettings.targetPoint == null)
            {
                Debug.LogError("[PoliceCallButton] Не заданы точки спавна/цели для полицейского в SimpleNPCSpawner!");
                return;
            }

            // Проверяем, не занят ли уже полицейский
            PoliceOfficer currentPolice = FindObjectOfType<PoliceOfficer>();
            if (currentPolice != null && currentPolice.GetCurrentState() == PoliceOfficer.PoliceState.Called)
            {
                Debug.Log("[PoliceCallButton] Полицейский уже вызван, вызов отменён!");
                return;
            }

            // Создаем полицейского на spawnPoint из SimpleNPCSpawner
            GameObject policeObject = Instantiate(spawner.policeOfficerPrefab, spawner.policeOfficerSettings.spawnPoint.position, Quaternion.identity);
            PoliceOfficer policeOfficer = policeObject.GetComponent<PoliceOfficer>();

            if (policeOfficer != null)
            {
                // ВАЖНО: spawnPoint в скриптах NPC означает точку ВОЗВРАТА (exitPoint)
                policeOfficer.SetSpawnPoint(spawner.policeOfficerSettings.exitPoint);
                policeOfficer.SetTargetPosition(spawner.policeOfficerSettings.targetPoint);
                policeOfficer.SetDoorTrigger(spawner.policeOfficerSettings.doorTrigger);
                Debug.Log("[PoliceCallButton] Полицейский создан и получил точки маршрута из SimpleNPCSpawner!");
            }
            else
            {
                Debug.LogError("[PoliceCallButton] Не удалось создать полицейского!");
            }
        }
        
        // Метод для получения текста взаимодействия (используется CrosshairSystem)
        public string GetInteractionText()
        {
            return playerInRange ? interactionText : "";
        }
        
        // Проверка, можно ли взаимодействовать с кнопкой
        public bool CanInteract()
        {
            PoliceOfficer currentPolice = FindObjectOfType<PoliceOfficer>();
            if (currentPolice == null) return true;
            
            // Проверяем, не занят ли полицейский
            PoliceOfficer.PoliceState state = currentPolice.GetCurrentState();
            return state != PoliceOfficer.PoliceState.Called && 
                   state != PoliceOfficer.PoliceState.InDialog;
        }
        
        void OnDrawGizmosSelected()
        {
            // Показываем зону взаимодействия
            Gizmos.color = playerInRange ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionDistance);
        }
    }
} 