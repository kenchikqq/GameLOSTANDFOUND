using UnityEngine;

namespace LostAndFound.Systems
{
    /// <summary>
    /// Интерактивная крышка кейса для режима осмотра:
    /// - Тяни мышкой (ПКМ по умолчанию) вверх/вниз по крышке, чтобы открыть/закрыть
    /// - В инспекции PlayerInventory: работает только когда предмет осматривается
    /// Требования к модели: крышка должна быть отдельным Transform с pivot на петлях.
    /// </summary>
    public class InteractiveCaseLid : MonoBehaviour
    {
        [Header("Ссылки")]
        public Transform lidTransform; // Крышка (отдельный чилд с pivot на петлях)
        public Collider lidCollider;   // Коллайдер на крышке для наведения

        [Header("Параметры крышки")]
        public Vector3 localHingeAxis = Vector3.right; // Ось вращения в локальных координатах крышки
        public float minAngle = 0f;    // Закрыто (в градусах)
        public float maxAngle = 110f;  // Открыто (в градусах)
        public float dragSensitivity = 200f; // Чем больше, тем быстрее угол меняется от движения мыши

        [Header("Сглаживание")]
        public float smoothTime = 0.06f; // сглаживание ротации

        [Header("Управление")]
        public int mouseButton = 0; // 0=ЛКМ, 1=ПКМ. По умолчанию ЛКМ по запросу
        public bool invertDrag = false; // Инвертировать направление движения мыши

        private float currentAngle;
        private float targetAngle;
        private float angleVelocity;
        private bool isDragging;
        private Quaternion closedLocalRotation;

        // Глобальный флаг, чтобы PlayerInventory не вращал предмет, когда тянем крышку ЛКМ
        public static bool IsAnyDragging { get; private set; }

        void Awake()
        {
            if (lidTransform == null)
                lidTransform = transform;

            closedLocalRotation = lidTransform.localRotation;
            currentAngle = 0f;
            targetAngle = 0f;

            if (lidCollider == null)
                lidCollider = lidTransform.GetComponent<Collider>();
        }

        void Update()
        {
            // Разрешаем управление только в режиме осмотра (если система инвентаря есть)
            if (PlayerInventory.Instance != null && !PlayerInventory.Instance.IsInspecting())
            {
                isDragging = false;
                return;
            }

            // В режиме осмотра инвентарь отключает все коллайдеры предмета.
            // Гарантируем, что коллайдер крышки включен, чтобы ловить луч.
            if (lidCollider != null && !lidCollider.enabled)
                lidCollider.enabled = true;

            HandleMouseDrag();
            ApplyRotation();
        }

        private void HandleMouseDrag()
        {
            Camera cam = Camera.main;
            if (cam == null || lidCollider == null) return;

            // Начало перетаскивания: нажата нужная кнопка и курсор над крышкой
            if (!isDragging && Input.GetMouseButtonDown(mouseButton))
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (lidCollider.Raycast(ray, out RaycastHit hit, 100f))
                {
                    isDragging = true;
                    IsAnyDragging = true;
                }
            }

            // Завершение перетаскивания
            if (isDragging && Input.GetMouseButtonUp(mouseButton))
            {
                isDragging = false;
                IsAnyDragging = false;
            }

            // Перемещение: по вертикали мыши меняем целевой угол
            if (isDragging)
            {
                float mouseY = Input.GetAxis("Mouse Y");
                float dir = invertDrag ? -1f : 1f;
                targetAngle += dir * mouseY * dragSensitivity * Time.deltaTime;
                targetAngle = Mathf.Clamp(targetAngle, minAngle, maxAngle);
            }
        }

        void OnDisable()
        {
            if (isDragging) { isDragging = false; IsAnyDragging = false; }
        }

        private void ApplyRotation()
        {
            currentAngle = Mathf.SmoothDamp(currentAngle, targetAngle, ref angleVelocity, smoothTime);
            Quaternion rot = Quaternion.AngleAxis(currentAngle, localHingeAxis.normalized);
            lidTransform.localRotation = closedLocalRotation * rot;
        }

        void OnDrawGizmosSelected()
        {
            if (lidTransform == null) return;
            Gizmos.color = Color.cyan;
            Vector3 p = lidTransform.position;
            Vector3 dir = lidTransform.TransformDirection(localHingeAxis.normalized) * 0.3f;
            Gizmos.DrawLine(p - dir, p + dir);
        }
    }
}

