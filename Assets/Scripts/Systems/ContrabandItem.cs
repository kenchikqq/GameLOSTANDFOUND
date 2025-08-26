using UnityEngine;

namespace LostAndFound.Systems
{
    /// <summary>
    /// Компонент для маркировки предметов как контрабанды
    /// </summary>
    public class ContrabandItem : MonoBehaviour
    {
        [Header("Настройки контрабанды")]
        public float contrabandValue = 100f;
        public string contrabandType = "General";
        
        void Start()
        {
            // Автоматически добавляем тег (если он существует)
            // Пока тег не создан, просто логируем
            Debug.Log($"[ContrabandItem] Контрабанда создана: {gameObject.name}");
        }
    }
} 