using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LostAndFound.Systems
{
    /// <summary>
    /// Карточка предмета для Черного рынка.
    /// Похожа на ItemCard, но БЕЗ полей ID и ОПИСАНИЯ.
    /// Показывает 3D-превью + имя и дату.
    /// </summary>
    public class BlackMarketItemCard : MonoBehaviour
    {
        [Header("UI Элементы")]
        public RawImage itemPreviewImage;
        public TextMeshProUGUI itemNameText;
        public TextMeshProUGUI itemDateText;

        [Header("3D Превью система")]
        public Transform itemPreviewSlot;
        public Camera previewCamera;
        public RenderTexture previewRenderTexture;
        public Light previewLight;

        [Header("Настройки")]
        public int previewLayer = 30; // Индекс слоя для ItemPreview
        public float cameraDistance = 2f;
        public float rotationSpeed = 20f;

        private GameObject currentPreviewObject;
        private bool isRotating = true;

        void Update()
        {
            if (currentPreviewObject != null && isRotating)
            {
                currentPreviewObject.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
            }
        }

        public void FillCard(ComputerSystem.DatabaseItem item)
        {
            if (item == null) return;

            if (itemNameText != null)
                itemNameText.text = item.name;

            if (itemDateText != null)
                itemDateText.text = item.date;

            SetupPreviewCamera();
            Create3DPreview(item.original3DObject);
        }

        void Create3DPreview(GameObject originalObject)
        {
            if (originalObject == null) return;

            if (currentPreviewObject != null)
            {
                Destroy(currentPreviewObject);
            }

            currentPreviewObject = Instantiate(originalObject, itemPreviewSlot);
            currentPreviewObject.SetActive(true);
            SetLayerRecursively(currentPreviewObject, previewLayer);
            RemoveUnnecessaryComponents(currentPreviewObject);
            currentPreviewObject.transform.localPosition = Vector3.zero;
            AdjustObjectScale(currentPreviewObject);
            SetupCameraForObject(currentPreviewObject);
        }

        void SetupPreviewCamera()
        {
            if (previewCamera == null)
            {
                Debug.LogWarning("[BlackMarketItemCard] PreviewCamera не назначена на карточке!");
                return;
            }

            if (previewRenderTexture != null)
            {
                previewRenderTexture.Release();
                Destroy(previewRenderTexture);
            }

            previewRenderTexture = new RenderTexture(256, 256, 16);
            previewRenderTexture.name = $"BM_ItemPreview_{gameObject.name}_{GetInstanceID()}";
            previewRenderTexture.Create();

            previewCamera.targetTexture = previewRenderTexture;
            previewCamera.cullingMask = 1 << previewLayer;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0f);
            previewCamera.orthographic = false;
            previewCamera.fieldOfView = 30f;

            if (itemPreviewImage != null)
                itemPreviewImage.texture = previewRenderTexture;
            else
                Debug.LogWarning("[BlackMarketItemCard] ItemPreviewImage не назначен!");
        }

        void SetupCameraForObject(GameObject obj)
        {
            if (previewCamera == null || obj == null) return;

            Bounds bounds = GetObjectBounds(obj);
            Vector3 cameraPosition = bounds.center + Vector3.back * cameraDistance;
            cameraPosition.y = bounds.center.y + bounds.size.y * 0.3f;
            previewCamera.transform.position = cameraPosition;
            previewCamera.transform.LookAt(bounds.center);
        }

        Bounds GetObjectBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(obj.transform.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }
            return bounds;
        }

        void AdjustObjectScale(GameObject obj)
        {
            Bounds bounds = GetObjectBounds(obj);
            float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (maxSize > 0f)
            {
                float targetSize = 1f;
                float scaleFactor = targetSize / maxSize;
                obj.transform.localScale *= scaleFactor;
            }
        }

        void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        void RemoveUnnecessaryComponents(GameObject obj)
        {
            foreach (Collider c in obj.GetComponentsInChildren<Collider>())
            {
                Destroy(c);
            }
            foreach (Rigidbody rb in obj.GetComponentsInChildren<Rigidbody>())
            {
                Destroy(rb);
            }
        }

        void OnDestroy()
        {
            if (previewRenderTexture != null)
            {
                previewRenderTexture.Release();
                Destroy(previewRenderTexture);
            }

            if (currentPreviewObject != null)
            {
                Destroy(currentPreviewObject);
            }
        }
    }
}

