using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LostAndFound.Systems
{
    /// <summary>
    /// Компонент карточки предмета в стиле Wildberries
    /// Показывает 3D предмет с мини-камерой + информация
    /// </summary>
    public class ItemCard : MonoBehaviour
    {
        [Header("UI Элементы")]
        public RawImage itemPreviewImage;        // RawImage для показа 3D предмета
        public TextMeshProUGUI itemIDText;       // ID предмета
        public TextMeshProUGUI itemNameText;     // Название предмета
        public TextMeshProUGUI itemDescriptionText; // Описание предмета
        public TextMeshProUGUI itemDateText;     // Дата находки

        [Header("3D Превью система")]
        public Transform itemPreviewSlot;        // Слот где будет 3D предмет
        public Camera previewCamera;             // Мини-камера для предмета
        public RenderTexture previewRenderTexture; // Рендер текстура
        public Light previewLight;               // Освещение для предмета

        [Header("Настройки")]
        public int previewLayer = 30;            // Индекс слоя для предметов (ItemPreview)
        public float cameraDistance = 2f;        // Расстояние камеры от предмета
        public float rotationSpeed = 20f;        // Скорость вращения предмета
        
        // Приватные переменные
        private GameObject currentPreviewObject;
        private bool isRotating = true;

        void Start()
        {
            // Камера настраивается в FillCard() для каждой карточки индивидуально
        }

        void Update()
        {
            // Медленное вращение предмета для красоты
            if (currentPreviewObject != null && isRotating)
            {
                currentPreviewObject.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
            }
        }

        /// <summary>
        /// Заполняет карточку данными предмета
        /// </summary>
        public void FillCard(ComputerSystem.DatabaseItem item)
        {
            Debug.Log($"[ItemCard] Заполнение карточки для: {item.name}");

            // Заполняем текстовые поля
            if (itemIDText != null)
                itemIDText.text = item.id;

            if (itemNameText != null)
                itemNameText.text = item.name;

            if (itemDescriptionText != null)
                itemDescriptionText.text = item.description;

            if (itemDateText != null)
                itemDateText.text = item.date;

            // Настраиваем уникальную камеру для этой карточки
            SetupPreviewCamera();

            // Создаем 3D превью предмета
            Create3DPreview(item.original3DObject);

            Debug.Log($"[ItemCard] Карточка {item.name} заполнена с уникальной камерой");
        }

        /// <summary>
        /// Создает 3D превью предмета
        /// </summary>
        void Create3DPreview(GameObject originalObject)
        {
            if (originalObject == null)
            {
                Debug.LogWarning("[ItemCard] Оригинальный объект для превью не найден!");
                return;
            }

            Debug.Log($"[ItemCard] Создание 3D превью для: {originalObject.name}");

            // Удаляем старый превью объект если есть
            if (currentPreviewObject != null)
            {
                Destroy(currentPreviewObject);
            }

            // Клонируем объект для превью
            currentPreviewObject = Instantiate(originalObject, itemPreviewSlot);
            
            // Переносим на слой превью
            SetLayerRecursively(currentPreviewObject, previewLayer);

            // Убираем ненужные компоненты (коллайдеры, скрипты)
            RemoveUnnecessaryComponents(currentPreviewObject);

            // Позиционируем объект в центре слота
            currentPreviewObject.transform.localPosition = Vector3.zero;
            
            // Подгоняем размер объекта
            AdjustObjectScale(currentPreviewObject);

            // Настраиваем камеру для этого объекта
            SetupCameraForObject(currentPreviewObject);

            Debug.Log($"[ItemCard] 3D превью создано для: {originalObject.name}");
        }

        /// <summary>
        /// Настраивает мини-камеру
        /// </summary>
        void SetupPreviewCamera()
        {
            if (previewCamera == null)
            {
                Debug.LogWarning("[ItemCard] Мини-камера не назначена!");
                return;
            }

            // ВСЕГДА создаем УНИКАЛЬНЫЙ RenderTexture для этой карточки
            if (previewRenderTexture != null)
            {
                previewRenderTexture.Release();
                Destroy(previewRenderTexture);
            }
            
            previewRenderTexture = new RenderTexture(256, 256, 16);
            previewRenderTexture.name = $"ItemPreview_{gameObject.name}_{GetInstanceID()}";
            previewRenderTexture.Create();

            // Настраиваем камеру
            previewCamera.targetTexture = previewRenderTexture;
            previewCamera.cullingMask = 1 << previewLayer;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0f); // Прозрачный серый фон
            previewCamera.orthographic = false;
            previewCamera.fieldOfView = 30f;

            // Применяем УНИКАЛЬНЫЙ RenderTexture к RawImage
            if (itemPreviewImage != null)
            {
                itemPreviewImage.texture = previewRenderTexture;
            }

            Debug.Log($"[ItemCard] Мини-камера настроена с уникальным RenderTexture: {previewRenderTexture.name}");
        }

        /// <summary>
        /// Настраивает камеру для конкретного объекта
        /// </summary>
        void SetupCameraForObject(GameObject obj)
        {
            if (previewCamera == null || obj == null) return;

            // Получаем границы объекта
            Bounds bounds = GetObjectBounds(obj);
            
            // Позиционируем камеру чтобы объект был в кадре
            Vector3 cameraPosition = bounds.center + Vector3.back * cameraDistance;
            cameraPosition.y = bounds.center.y + bounds.size.y * 0.3f; // Немного сверху
            
            previewCamera.transform.position = cameraPosition;
            previewCamera.transform.LookAt(bounds.center);

            Debug.Log($"[ItemCard] Камера настроена для объекта: {obj.name}");
        }

        /// <summary>
        /// Получает границы объекта
        /// </summary>
        Bounds GetObjectBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(obj.transform.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        /// <summary>
        /// Подгоняет размер объекта под слот
        /// </summary>
        void AdjustObjectScale(GameObject obj)
        {
            Bounds bounds = GetObjectBounds(obj);
            float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            
            if (maxSize > 0)
            {
                float targetSize = 1f; // Желаемый размер
                float scaleFactor = targetSize / maxSize;
                obj.transform.localScale *= scaleFactor;
            }
        }

        /// <summary>
        /// Устанавливает слой рекурсивно
        /// </summary>
        void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        /// <summary>
        /// Удаляет ненужные компоненты с превью объекта
        /// </summary>
        void RemoveUnnecessaryComponents(GameObject obj)
        {
            // Удаляем коллайдеры (не нужны для превью)
            Collider[] colliders = obj.GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                Destroy(collider);
            }

            // Удаляем Rigidbody (не нужны для превью)
            Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rb in rigidbodies)
            {
                Destroy(rb);
            }

            // Оставляем только Renderer компоненты
            Debug.Log($"[ItemCard] Ненужные компоненты удалены с: {obj.name}");
        }

        /// <summary>
        /// Включает/выключает вращение предмета
        /// </summary>
        public void SetRotation(bool rotate)
        {
            isRotating = rotate;
        }

        void OnDestroy()
        {
            // Очищаем RenderTexture
            if (previewRenderTexture != null)
            {
                previewRenderTexture.Release();
                Destroy(previewRenderTexture);
            }

            // Удаляем превью объект
            if (currentPreviewObject != null)
            {
                Destroy(currentPreviewObject);
            }
        }
    }
}