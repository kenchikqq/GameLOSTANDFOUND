using UnityEngine;
using System.Collections;
using LostAndFound.NPCs;

namespace LostAndFound.Systems
{
    /// <summary>
    /// ТЕСТЕР ПОЛИЦЕЙСКОГО - быстрые команды для тестирования системы полицейского
    /// </summary>
    public class PoliceTestSystem : MonoBehaviour
    {
        [Header("Горячие клавиши")]
        public KeyCode testPoliceCallKey = KeyCode.F1;
        public KeyCode testContrabandSpawnKey = KeyCode.F3;
        public KeyCode testReputationKey = KeyCode.F4;
        public KeyCode testPatrolKey = KeyCode.F5;
        
        [Header("Настройки тестирования")]
        public GameObject contrabandPrefab;
        public Transform contrabandSpawnPoint;
        public float reputationTestAmount = 10f;
        
        [Header("Префаб полицейского")]
        public GameObject policeOfficerPrefab;
        
        private PoliceOfficer currentPolice;
        
        void Start()
        {
            InitializeTestSystem();
        }
        
        void Update()
        {
            HandleTestInput();
        }
        
        void InitializeTestSystem()
        {
            if (policeOfficerPrefab == null)
            {
                Debug.LogError("[PoliceTestSystem] Не задан префаб полицейского!");
            }
            
            Debug.Log("[PoliceTestSystem] Тестер полицейского инициализирован");
            Debug.Log("[PoliceTestSystem] F1 - Вызвать полицейского");
            Debug.Log("[PoliceTestSystem] F3 - Создать контрабанду");
            Debug.Log("[PoliceTestSystem] F4 - Изменить репутацию");
            Debug.Log("[PoliceTestSystem] F5 - Принудительный патруль");
        }
        
        void HandleTestInput()
        {
            if (Input.GetKeyDown(testPoliceCallKey))
            {
                TestPoliceCall();
            }
            
            if (Input.GetKeyDown(testContrabandSpawnKey))
            {
                TestContrabandSpawn();
            }
            
            if (Input.GetKeyDown(testReputationKey))
            {
                TestReputationChange();
            }
            
            if (Input.GetKeyDown(testPatrolKey))
            {
                TestForcePatrol();
            }
        }
        
        void TestPoliceCall()
        {
            if (policeOfficerPrefab == null)
            {
                Debug.LogError("[PoliceTestSystem] Не задан префаб полицейского!");
                return;
            }
            
            // Создаем полицейского
            Vector3 spawnPosition = transform.position + Vector3.up * 2f;
            GameObject policeObject = Instantiate(policeOfficerPrefab, spawnPosition, Quaternion.identity);
            currentPolice = policeObject.GetComponent<PoliceOfficer>();
            
            if (currentPolice != null)
            {
                Debug.Log("[PoliceTestSystem] Полицейский создан!");
            }
            else
            {
                Debug.LogError("[PoliceTestSystem] Не удалось создать полицейского!");
            }
        }
        
        void TestContrabandSpawn()
        {
            if (contrabandPrefab == null)
            {
                Debug.LogError("[PoliceTestSystem] Не задан префаб контрабанды!");
                return;
            }
            
            Vector3 spawnPosition = contrabandSpawnPoint != null ? 
                contrabandSpawnPoint.position : 
                PlayerController.Instance.transform.position + PlayerController.Instance.transform.forward * 2f;
            
            GameObject contraband = Instantiate(contrabandPrefab, spawnPosition, Quaternion.identity);
            contraband.name = "TestContraband";
            
            Debug.Log($"[PoliceTestSystem] Контрабанда создана в позиции: {spawnPosition}");
        }
        
        void TestContrabandDetection()
        {
            PoliceOfficer police = FindObjectOfType<PoliceOfficer>();
            if (police == null)
            {
                Debug.LogError("[PoliceTestSystem] Полицейский не найден!");
                return;
            }
            
            Debug.Log("[PoliceTestSystem] Тестирование обнаружения контрабанды...");
            // Здесь можно добавить логику тестирования обнаружения
        }
        
        void TestReputationChange()
        {
            PoliceOfficer police = FindObjectOfType<PoliceOfficer>();
            if (police == null)
            {
                Debug.LogError("[PoliceTestSystem] Полицейский не найден!");
                return;
            }
            
            int currentRep = police.GetReputation();
            Debug.Log($"[PoliceTestSystem] Текущая репутация: {currentRep}");
            // Здесь можно добавить логику изменения репутации
        }
        
        void TestForcePatrol()
        {
            PoliceOfficer police = FindObjectOfType<PoliceOfficer>();
            if (police == null)
            {
                Debug.LogError("[PoliceTestSystem] Полицейский не найден!");
                return;
            }
            
            Debug.Log("[PoliceTestSystem] Принудительный патруль отключен (система движения удалена)");
        }
        
        void TestContrabandCreation()
        {
            if (contrabandPrefab == null)
            {
                Debug.LogError("[PoliceTestSystem] Не задан префаб контрабанды!");
                return;
            }
            
            Vector3 spawnPosition = PlayerController.Instance.transform.position + 
                                  PlayerController.Instance.transform.forward * 3f;
            
            GameObject contraband = Instantiate(contrabandPrefab, spawnPosition, Quaternion.identity);
            contraband.name = "TestContraband_" + System.DateTime.Now.ToString("HHmmss");
            
            Debug.Log($"[PoliceTestSystem] Тестовая контрабанда создана: {contraband.name}");
        }
        
        void TestReputationSystem()
        {
            PoliceOfficer police = FindObjectOfType<PoliceOfficer>();
            if (police == null)
            {
                Debug.LogError("[PoliceTestSystem] Полицейский не найден!");
                return;
            }
            
            Debug.Log($"[PoliceTestSystem] Репутация полицейского: {police.GetReputation()}");
        }
        
        public void SpawnTestContraband(Vector3 position)
        {
            if (contrabandPrefab == null)
            {
                Debug.LogError("[PoliceTestSystem] Не задан префаб контрабанды!");
                return;
            }
            
            GameObject contraband = Instantiate(contrabandPrefab, position, Quaternion.identity);
            contraband.name = "TestContraband_" + System.DateTime.Now.ToString("HHmmss");
            
            Debug.Log($"[PoliceTestSystem] Контрабанда создана в позиции: {position}");
        }
        
        public void SetPoliceReputation(float reputation)
        {
            PoliceOfficer police = FindObjectOfType<PoliceOfficer>();
            if (police == null)
            {
                Debug.LogError("[PoliceTestSystem] Полицейский не найден!");
                return;
            }
            
            Debug.Log($"[PoliceTestSystem] Установка репутации: {reputation}");
            // Здесь можно добавить логику установки репутации
        }
        
        public void ForcePolicePatrol()
        {
            Debug.Log("[PoliceTestSystem] Принудительный патруль отключен (система движения удалена)");
        }
        
        public void CallTestPolice()
        {
            TestPoliceCall();
        }
        
        void OnDrawGizmosSelected()
        {
            // Показываем зону тестирования
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 5f);
        }
    }
} 