using UnityEngine;
using LostAndFound.NPCs;
using System.Collections.Generic;

namespace LostAndFound.Systems
{
    public class SimpleNPCSpawner : MonoBehaviour
    {
        [Header("Префабы NPCs")]
        public GameObject whiteNPCPrefab;
        public GameObject blackNPCPrefab;
        public GameObject policeOfficerPrefab;

        [System.Serializable]
        public class NPCSettings
        {
            public Transform spawnPoint;
            public Transform targetPoint;
            public Transform exitPoint;
            [Tooltip("Точка-триггер двери: NPC сначала идёт сюда, ждёт и только потом к targetPoint")] public Transform doorTrigger;
            public bool isActive = true;
        }

        [Header("Настройки NPCs")]
        public NPCSettings whiteNPCSettings = new NPCSettings();
        public NPCSettings blackNPCSettings = new NPCSettings();
        public NPCSettings policeOfficerSettings = new NPCSettings();

        [Header("Настройки спавна")]
        public int maxNPCsAtTarget = 3;
        public float minSpawnDelay = 3f;
        public float maxSpawnDelay = 15f;
        [Tooltip("Включить автоматический спавн NPC (белые/чёрные). Полицейский управляется кнопкой")] public bool enableAutoSpawn = true;
        [Tooltip("Разрешить включать полицейского в авто-спавн")] public bool autoSpawnPolice = false;

        [Header("Вероятности")]
        [Range(0, 100)] public int whiteNPCChance = 75;
        [Range(0, 100)] public int blackNPCChance = 20;
        [Range(0, 100)] public int policeChance = 5;

        private float nextSpawnTime;
        private List<WhiteNPC> activeWhiteNPCs = new List<WhiteNPC>();
        private List<BlackNPC> activeBlackNPCs = new List<BlackNPC>();
        private List<PoliceOfficer> activePoliceOfficers = new List<PoliceOfficer>();

        void Start()
        {
            // Начинаем с хаотичного спавна только если включено
            if (enableAutoSpawn)
            {
                ScheduleNextSpawn();
            }
        }

        void Update()
        {
            if (!enableAutoSpawn) return;
            if (Time.time >= nextSpawnTime)
            {
                TrySpawnRandomNPC();
                ScheduleNextSpawn();
            }
        }

        void ScheduleNextSpawn()
        {
            // Хаотичный спавн с случайной задержкой
            float randomDelay = Random.Range(minSpawnDelay, maxSpawnDelay);
            nextSpawnTime = Time.time + randomDelay;
            Debug.Log($"[SimpleNPCSpawner] Следующий спавн через {randomDelay:F1} секунд");
        }

        void TrySpawnRandomNPC()
        {
            if (Time.time < nextSpawnTime) return;

            // Проверяем, есть ли доступные места для спавна
            if (!HasAvailableSpawnPoint()) return;

            GameObject prefabToSpawn = GetRandomNPCPrefab();
            if (prefabToSpawn == null)
            {
                // Нет доступных типов сейчас — отложим попытку
                ScheduleNextSpawn();
                return;
            }

            // Получаем индивидуальные настройки для этого типа NPC
            NPCSettings settings = GetRandomNPCSettings(prefabToSpawn);
            if (settings == null || settings.spawnPoint == null)
            {
                Debug.LogWarning("[SimpleNPCSpawner] Нет доступных настроек для спавна NPC");
                return;
            }

            // Спавним NPC в индивидуальной точке
            GameObject npc = Instantiate(prefabToSpawn, settings.spawnPoint.position, settings.spawnPoint.rotation);
            SetupNPC(npc);
            
            Debug.Log($"[SimpleNPCSpawner] Создан NPC: {prefabToSpawn.name} в точке: {settings.spawnPoint.name}");
        }

        bool HasAvailableSpawnPoint()
        {
            // Проверяем, есть ли активные настройки с spawn points
            if (whiteNPCSettings.isActive && whiteNPCSettings.spawnPoint != null) return true;
            if (blackNPCSettings.isActive && blackNPCSettings.spawnPoint != null) return true;
            if (policeOfficerSettings.isActive && policeOfficerSettings.spawnPoint != null) return true;
            
            return false;
        }

        GameObject GetRandomNPCPrefab()
        {
            // Строим список доступных типов с весами
            int whiteWeight = (whiteNPCSettings.isActive && whiteNPCSettings.spawnPoint != null && whiteNPCPrefab != null) ? whiteNPCChance : 0;
            int blackWeight = (blackNPCSettings.isActive && blackNPCSettings.spawnPoint != null && blackNPCPrefab != null) ? blackNPCChance : 0;
            int policeWeight = 0;
            if (autoSpawnPolice)
            {
                policeWeight = (policeOfficerSettings.isActive && policeOfficerSettings.spawnPoint != null && policeOfficerPrefab != null) ? policeChance : 0;
            }

            // Если достигнут предел в очереди для типа - не учитываем его
            if (!CanSpawnMoreAtTarget(whiteNPCSettings.targetPoint, activeWhiteNPCs)) whiteWeight = 0;
            if (!CanSpawnMoreAtTarget(blackNPCSettings.targetPoint, activeBlackNPCs)) blackWeight = 0;

            int total = whiteWeight + blackWeight + policeWeight;
            if (total <= 0)
            {
                // Ничего спавнить нельзя
                return null;
            }

            int roll = Random.Range(0, total);
            if (roll < whiteWeight) return whiteNPCPrefab;
            roll -= whiteWeight;
            if (roll < blackWeight) return blackNPCPrefab;
            return policeOfficerPrefab; // к этому моменту policeWeight > 0
        }

        bool CanSpawnMoreAtTarget<T>(Transform targetPoint, List<T> list) where T : MonoBehaviour
        {
            if (targetPoint == null) return false;

            int count = 0;
            foreach (var comp in list)
            {
                if (comp == null) continue;

                // Определяем тип и состояние
                if (comp is WhiteNPC wn)
                {
                    if (wn.GetTargetPosition() == targetPoint && wn.GetCurrentState() != WhiteNPC.NPCState.Finished) count++;
                }
                else if (comp is BlackNPC bn)
                {
                    if (bn.GetTargetPosition() == targetPoint && bn.GetCurrentState() != BlackNPC.NPCState.Finished) count++;
                }
            }
            return count < maxNPCsAtTarget;
        }

        void SetupNPC(GameObject npc)
        {
            // Получаем индивидуальные настройки для этого NPC
            NPCSettings settings = GetNPCSettings(npc);
            
            if (settings == null)
            {
                Debug.LogError("[SimpleNPCSpawner] Не найдены настройки для NPC!");
                return;
            }

            // Получаем позицию в очереди
            int queuePosition = GetNextQueuePosition(settings.targetPoint);
            Debug.Log($"[SimpleNPCSpawner] Создаю NPC для target point: {settings.targetPoint?.name}, позиция в очереди: {queuePosition}");

            // Настраиваем WhiteNPC
            WhiteNPC whiteNPC = npc.GetComponent<WhiteNPC>();
            if (whiteNPC != null)
            {
                whiteNPC.SetTargetPosition(settings.targetPoint);
                whiteNPC.SetSpawnPoint(settings.exitPoint);
                whiteNPC.SetDoorTrigger(settings.doorTrigger);
                whiteNPC.SetQueuePosition(queuePosition);
                
                // Даем белому NPC случайный предмет для ношения
                Item randomItem = GetRandomItem();
                if (randomItem != null)
                {
                    whiteNPC.HoldItem(randomItem);
                    Debug.Log($"[SimpleNPCSpawner] WhiteNPC получил предмет для ношения: {randomItem.itemName}");
                }
                
                whiteNPC.OnNPCFinished += OnWhiteNPCFinished;
                activeWhiteNPCs.Add(whiteNPC);
                Debug.Log($"[SimpleNPCSpawner] WhiteNPC создан с позицией в очереди: {queuePosition} для target: {settings.targetPoint?.name}");
                return;
            }

            // Настраиваем BlackNPC
            BlackNPC blackNPC = npc.GetComponent<BlackNPC>();
            if (blackNPC != null)
            {
                blackNPC.SetTargetPosition(settings.targetPoint);
                blackNPC.SetSpawnPoint(settings.exitPoint);
                blackNPC.SetDoorTrigger(settings.doorTrigger);
                blackNPC.SetQueuePosition(queuePosition);
                blackNPC.OnNPCFinished += OnBlackNPCFinished;
                activeBlackNPCs.Add(blackNPC);
                Debug.Log($"[SimpleNPCSpawner] BlackNPC создан с позицией в очереди: {queuePosition} для target: {settings.targetPoint?.name}");
                return;
            }

            // Настраиваем PoliceOfficer
            PoliceOfficer policeOfficer = npc.GetComponent<PoliceOfficer>();
            if (policeOfficer != null)
            {
                policeOfficer.SetTargetPosition(settings.targetPoint);
                policeOfficer.SetSpawnPoint(settings.exitPoint);
                policeOfficer.SetDoorTrigger(settings.doorTrigger);
                activePoliceOfficers.Add(policeOfficer);
                Debug.Log($"[SimpleNPCSpawner] PoliceOfficer создан для target: {settings.targetPoint?.name}");
                return;
            }
        }

        NPCSettings GetNPCSettings(GameObject npc)
        {
            // Определяем тип NPC и возвращаем соответствующие настройки
            if (npc.GetComponent<WhiteNPC>() != null)
            {
                return whiteNPCSettings;
            }
            else if (npc.GetComponent<BlackNPC>() != null)
            {
                return blackNPCSettings;
            }
            else if (npc.GetComponent<PoliceOfficer>() != null)
            {
                return policeOfficerSettings;
            }
            
            return null;
        }

        int GetNextQueuePosition(Transform targetPoint)
        {
            int maxQueuePosition = -1;
            
            Debug.Log($"[SimpleNPCSpawner] Вычисляю позицию в очереди для target: {targetPoint?.name}");
            
            // Находим максимальную позицию в очереди для этого target point
            foreach (WhiteNPC npc in activeWhiteNPCs)
            {
                if (npc != null && npc.GetTargetPosition() == targetPoint && 
                    npc.GetCurrentState() != WhiteNPC.NPCState.Finished)
                {
                    int pos = npc.GetQueuePosition();
                    maxQueuePosition = Mathf.Max(maxQueuePosition, pos);
                    Debug.Log($"[SimpleNPCSpawner] WhiteNPC на позиции {pos}");
                }
            }
            
            foreach (BlackNPC npc in activeBlackNPCs)
            {
                if (npc != null && npc.GetTargetPosition() == targetPoint && 
                    npc.GetCurrentState() != BlackNPC.NPCState.Finished)
                {
                    int pos = npc.GetQueuePosition();
                    maxQueuePosition = Mathf.Max(maxQueuePosition, pos);
                    Debug.Log($"[SimpleNPCSpawner] BlackNPC на позиции {pos}");
                }
            }
            
            int nextPosition = maxQueuePosition + 1;
            Debug.Log($"[SimpleNPCSpawner] Следующая позиция в очереди: {nextPosition}");
            return nextPosition;
        }

        void OnWhiteNPCFinished(WhiteNPC npc)
        {
            activeWhiteNPCs.Remove(npc);
            Debug.Log($"[SimpleNPCSpawner] WhiteNPC завершил работу, осталось: {activeWhiteNPCs.Count}");
        }

        void OnBlackNPCFinished(BlackNPC npc)
        {
            activeBlackNPCs.Remove(npc);
            Debug.Log($"[SimpleNPCSpawner] BlackNPC завершил работу, осталось: {activeBlackNPCs.Count}");
        }

        // Методы для ручного спавна
        [ContextMenu("Создать белого NPC")]
        public void SpawnWhiteNPC()
        {
            if (whiteNPCPrefab != null && HasAvailableSpawnPoint())
            {
                Transform spawnPoint = whiteNPCSettings.spawnPoint; // Assuming whiteNPC1 is the first active spawn point
                GameObject npc = Instantiate(whiteNPCPrefab, spawnPoint.position, spawnPoint.rotation);
                SetupNPC(npc);
            }
        }

        [ContextMenu("Создать черного NPC")]
        public void SpawnBlackNPC()
        {
            if (blackNPCPrefab != null && HasAvailableSpawnPoint())
            {
                Transform spawnPoint = blackNPCSettings.spawnPoint; // Assuming blackNPC1 is the first active spawn point
                GameObject npc = Instantiate(blackNPCPrefab, spawnPoint.position, spawnPoint.rotation);
                SetupNPC(npc);
            }
        }

        [ContextMenu("Создать полицейского")]
        public void SpawnPoliceOfficer()
        {
            if (policeOfficerPrefab != null && HasAvailableSpawnPoint())
            {
                Transform spawnPoint = policeOfficerSettings.spawnPoint; // Assuming policeOfficer1 is the first active spawn point
                GameObject npc = Instantiate(policeOfficerPrefab, spawnPoint.position, spawnPoint.rotation);
                SetupNPC(npc);
            }
        }

        Item GetRandomItem()
        {
            // Загружаем все предметы из папки Resources/Items
            Item[] allItems = Resources.LoadAll<Item>("Items");
            
            if (allItems != null && allItems.Length > 0)
            {
                // Выбираем случайный предмет из Resources (шаблон для невидимого ношения)
                Item templateItem = allItems[Random.Range(0, allItems.Length)];
                
                Debug.Log($"[SimpleNPCSpawner] Выбран шаблон предмета для невидимого ношения: {templateItem.itemName}");
                return templateItem;
            }
            else
            {
                Debug.LogWarning("[SimpleNPCSpawner] Не найдены предметы в папке Resources/Items");
                return null;
            }
        }

        NPCSettings GetRandomNPCSettings(GameObject prefab)
        {
            // Определяем тип NPC и возвращаем случайные настройки
            if (prefab.GetComponent<WhiteNPC>() != null)
            {
                return whiteNPCSettings;
            }
            else if (prefab.GetComponent<BlackNPC>() != null)
            {
                return blackNPCSettings;
            }
            else if (prefab.GetComponent<PoliceOfficer>() != null)
            {
                return policeOfficerSettings;
            }
            
            return null;
        }
    }
} 