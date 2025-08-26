using UnityEngine;

/// <summary>
/// Компонент для поворота объекта к камере (Billboard эффект)
/// Используется для текста, чтобы он всегда был читаемым
/// </summary>
public sealed class BillboardText : MonoBehaviour
{
    [Header("Настройки Billboard")]
    [SerializeField] private bool lockYAxis = true;
    [SerializeField] private bool smoothRotation = true;
    [SerializeField] private float rotationSpeed = 5f;
    
    private Camera targetCamera;
    private Vector3 originalRotation;
    
    private void Start()
    {
        // Ищем основную камеру
        targetCamera = Camera.main;
        if (targetCamera == null)
        {
            targetCamera = FindObjectOfType<Camera>();
        }
        
        // Сохраняем оригинальный поворот
        originalRotation = transform.localEulerAngles;
    }
    
    private void Update()
    {
        if (targetCamera == null) return;
        
        // Вычисляем направление к камере
        Vector3 directionToCamera = targetCamera.transform.position - transform.position;
        
        if (lockYAxis)
        {
            // Блокируем Y ось - текст не наклоняется вверх/вниз
            directionToCamera.y = 0;
        }
        
        // Вычисляем поворот
        Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera);
        
        if (lockYAxis)
        {
            // Восстанавливаем Y поворот
            Vector3 targetEuler = targetRotation.eulerAngles;
            targetEuler.y = originalRotation.y;
            targetRotation = Quaternion.Euler(targetEuler);
        }
        
        // Применяем поворот
        if (smoothRotation)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            transform.rotation = targetRotation;
        }
    }
    
    // Методы для настройки в инспекторе
    [ContextMenu("Включить плавный поворот")]
    private void EnableSmoothRotation()
    {
        smoothRotation = true;
    }
    
    [ContextMenu("Отключить плавный поворот")]
    private void DisableSmoothRotation()
    {
        smoothRotation = false;
    }
    
    [ContextMenu("Заблокировать Y ось")]
    private void LockYAxis()
    {
        lockYAxis = true;
    }
    
    [ContextMenu("Разблокировать Y ось")]
    private void UnlockYAxis()
    {
        lockYAxis = false;
    }
}
