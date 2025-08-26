using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LostAndFound.Systems
{
    /// <summary>
    /// Runtime-пылесниматель: накладывает слой пыли поверх предмета и позволяет стирать его кистью в режиме осмотра.
    /// Работает поверх любого материала, добавляя второй материал-оверлей с шейдером Dust/Overlay.
    /// </summary>
    [DisallowMultipleComponent]
    public class DustCleaner : MonoBehaviour
    {
        [Header("Enable / Control")]
        public bool cleaningEnabled = true;
        [Tooltip("Требовать режим осмотра для чистки (PlayerInventory.IsInspecting())")] public bool requireInspectMode = true;
        public Camera raycastCamera; // если не задана — возьмём Camera.main
        [Tooltip("Максимальная дистанция луча для чистки")] public float paintMaxDistance = 10f;
        [Tooltip("Какие слои участвуют в луче чистки")] public LayerMask raycastMask = ~0;
        [Header("Debug")]
        public bool debugHotkeys = true; // F6 очистить, F7 запылить, F8 мазок в центре

        [Header("Overlay (Shader Dust/Overlay)")]
        public Texture2D dustTexture; // карта пыли (атлас/узор)
        [Tooltip("(Опционально) Готовый материал Shader Graph для HDRP/URP. Должен иметь свойства _DustTex, _DustColor, _DustStrength, _DustMask")]
        public Material overlayMaterialTemplate;
        public Color dustColor = new Color(1,1,1,1);
        [Range(0f, 2f)] public float dustStrength = 1f;

        [Header("Mask (RenderTexture)")]
        [Tooltip("Размер текстуры маски. 1024 — хороший компромисс")] public int maskSize = 1024;
        [Tooltip("Запускать с полностью пыльной поверхностью")]
        public bool startFullyDusty = true;
        [Tooltip("Необязательная стартовая маска (0 — чисто, 1 — пыль)")]
        public Texture2D initialMask;

        [Header("Brush")]
        [Range(1f, 256f)] public float brushSize = 64f; // в пикселях маски
        [Range(0.01f, 1f)] public float brushHardness = 0.8f; // мягкость края
        [Range(0.01f, 1f)] public float brushStrength = 0.3f; // насколько вычесть из маски за мазок

        [Header("UI (optional)")]
        public Slider progressBar;
        public TextMeshProUGUI progressText; // 0/100
        [Range(0.05f, 1f)] public float progressUpdateInterval = 0.25f;

        // Runtime
        private RenderTexture _maskRT;
        private Material _overlayMat;          // Dust/Overlay материал
        private Material _brushMat;            // Hidden/Dust/Brush материал
        private MeshRenderer _renderer;
        private float _progressTimer;

        private static readonly int ID_DustTex = Shader.PropertyToID("_DustTex");
        private static readonly int ID_DustColor = Shader.PropertyToID("_DustColor");
        private static readonly int ID_DustStrength = Shader.PropertyToID("_DustStrength");
        private static readonly int ID_DustMask = Shader.PropertyToID("_DustMask");

        private static readonly int ID_BrushPos = Shader.PropertyToID("_BrushPos");
        private static readonly int ID_BrushSize = Shader.PropertyToID("_BrushSize");
        private static readonly int ID_BrushHardness = Shader.PropertyToID("_BrushHardness");
        private static readonly int ID_BrushStrength = Shader.PropertyToID("_BrushStrength");

        private bool _validatedOnce;

        void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            if (_renderer == null)
            {
                Debug.LogError("[DustCleaner] MeshRenderer не найден на объекте");
                enabled = false;
                return;
            }

            EnsureOverlayMaterial();
            EnsureMask();
            ValidateConfiguration();
        }

        void Start()
        {
            UpdateOverlayProps();
            UpdateUI(true);
        }

        void OnDestroy()
        {
            if (_maskRT != null)
            {
                _maskRT.Release();
                _maskRT = null;
            }
        }

        void EnsureOverlayMaterial()
        {
            // Добавляем второй материал с шейдером пыли
            if (overlayMaterialTemplate != null)
            {
                _overlayMat = new Material(overlayMaterialTemplate) { name = "DustOverlay (runtime)" };
            }
            else
            {
                Shader overlayShader = Shader.Find("Dust/Overlay");
                if (overlayShader == null)
                {
                    Debug.LogError("[DustCleaner] Не найден материал Shader Graph и шейдер Dust/Overlay. Укажи overlayMaterialTemplate (Shader Graph) для твоего рендера или добавь шейдер Dust/Overlay.", this);
                    return;
                }
                _overlayMat = new Material(overlayShader) { name = "DustOverlay (runtime)" };
            }

            var mats = _renderer.sharedMaterials;
            bool alreadyAdded = Array.Exists(mats, m => m != null && _overlayMat != null && m.shader == _overlayMat.shader);
            if (!alreadyAdded)
            {
                Array.Resize(ref mats, mats.Length + 1);
                mats[mats.Length - 1] = _overlayMat;
                _renderer.sharedMaterials = mats;
            }
            else
            {
                // Найдём уже добавленный материал
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && _overlayMat != null && mats[i].shader == _overlayMat.shader)
                        _overlayMat = mats[i];
            }
        }

        void ValidateConfiguration()
        {
            if (_validatedOnce) return;
            _validatedOnce = true;

            // Камера
            if (raycastCamera == null && Camera.main == null)
            {
                Debug.LogError("[DustCleaner] Raycast Camera не задана и нет MainCamera в сцене — стирание не сработает.", this);
            }

            // Коллайдеры
            MeshCollider mc = GetComponent<MeshCollider>();
            if (mc == null)
            {
                Debug.LogError("[DustCleaner] Не найден MeshCollider. Добавь MeshCollider (IsTrigger=On) на этот объект — без него Raycast не получит UV.", this);
            }
            else if (mc.sharedMesh == null)
            {
                Debug.LogError("[DustCleaner] У MeshCollider не назначен Mesh. Укажи тот же Mesh, что в MeshFilter.", this);
            }

            // Рендерер
            if (_renderer.sharedMaterials == null || _renderer.sharedMaterials.Length == 0)
            {
                Debug.LogError("[DustCleaner] У MeshRenderer нет материалов. Нужен базовый материал, поверх которого рисуется пыль.", this);
            }

            // Текстура пыли
            if (dustTexture == null)
            {
                Debug.LogError("[DustCleaner] Не назначена Dust Texture. Перетащи карту пыли в поле 'Dust Texture'.", this);
            }
        }

        void EnsureMask()
        {
            if (_maskRT == null)
            {
                // Используем ARGB32 для максимальной совместимости SRP/HDRP
                _maskRT = new RenderTexture(maskSize, maskSize, 0, RenderTextureFormat.ARGB32)
                {
                    name = $"DustMask_{gameObject.name}",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                _maskRT.Create();

                // Инициализация
                var prev = RenderTexture.active;
                RenderTexture.active = _maskRT;
                GL.Clear(false, true, startFullyDusty ? Color.white : Color.black);
                if (initialMask != null)
                {
                    // Копируем стартовую маску
                    Graphics.Blit(initialMask, _maskRT);
                }
                RenderTexture.active = prev;
            }

            // Материал кисти
            if (_brushMat == null)
            {
                Shader brush = Shader.Find("Hidden/Dust/Brush");
                _brushMat = new Material(brush);
            }
        }

        void Update()
        {
            if (!_validatedOnce) ValidateConfiguration();
            if (!cleaningEnabled) return;
            if (requireInspectMode && PlayerInventory.Instance != null && !PlayerInventory.Instance.IsInspecting()) return;

            if (Input.GetMouseButton(0))
            {
                TryPaintUnderCursor();
            }

            if (debugHotkeys)
            {
                if (Input.GetKeyDown(KeyCode.F6)) { FillMask(Color.black); Debug.Log("[DustCleaner] Debug: mask -> BLACK (чисто)"); }
                if (Input.GetKeyDown(KeyCode.F7)) { FillMask(Color.white); Debug.Log("[DustCleaner] Debug: mask -> WHITE (вся пыль)"); }
                if (Input.GetKeyDown(KeyCode.F8)) { PaintAtUV(new Vector2(0.5f, 0.5f)); Debug.Log("[DustCleaner] Debug: мазок по центру UV"); }
            }

            _progressTimer += Time.deltaTime;
            if (_progressTimer >= progressUpdateInterval)
            {
                _progressTimer = 0f;
                UpdateUI(false);
            }
        }

        void TryPaintUnderCursor()
        {
            Camera cam = raycastCamera != null ? raycastCamera : Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            var hits = Physics.RaycastAll(ray, paintMaxDistance, raycastMask, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0) return;
            Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (h.collider == null) continue;
                var targetCleaner = h.collider.GetComponentInParent<DustCleaner>();
                if (targetCleaner != this) continue;
                var meshCol = h.collider.GetComponent<MeshCollider>();
                if (meshCol == null)
                {
                    // продолжаем искать MeshCollider этого же объекта, если поверх стоит BoxCollider
                    continue;
                }
                Vector2 uv = h.textureCoord; // 0..1
                PaintAtUV(uv);
                break;
            }
        }

        void PaintAtUV(Vector2 uv)
        {
            if (_maskRT == null || _brushMat == null) return;

            _brushMat.SetVector(ID_BrushPos, new Vector4(uv.x, uv.y, 0, 0));
            _brushMat.SetFloat(ID_BrushSize, brushSize / maskSize); // нормализованный радиус
            _brushMat.SetFloat(ID_BrushHardness, brushHardness);
            _brushMat.SetFloat(ID_BrushStrength, brushStrength);

            RenderTexture tmp = RenderTexture.GetTemporary(_maskRT.descriptor);
            Graphics.Blit(_maskRT, tmp);               // текущая маска → tmp
            Graphics.Blit(tmp, _maskRT, _brushMat);    // рисуем круг и пишем обратно
            RenderTexture.ReleaseTemporary(tmp);

            // На некоторых платформах полезно повторно проставить RT в материал
            UpdateOverlayProps();
        }

        void FillMask(Color c)
        {
            if (_maskRT == null) return;
            var prev = RenderTexture.active; RenderTexture.active = _maskRT;
            GL.Clear(false, true, c);
            RenderTexture.active = prev;
            UpdateOverlayProps();
        }

        void UpdateOverlayProps()
        {
            if (_overlayMat == null) return;
            _overlayMat.SetTexture(ID_DustTex, dustTexture);
            _overlayMat.SetColor(ID_DustColor, dustColor);
            _overlayMat.SetFloat(ID_DustStrength, dustStrength);
            _overlayMat.SetTexture(ID_DustMask, _maskRT);
        }

        void UpdateUI(bool force)
        {
            if (progressBar == null && progressText == null) return;

            // Оценим процент чистоты грубо: читаем в маленький Texture2D
            int down = 128;
            RenderTexture tmp = RenderTexture.GetTemporary(down, down, 0, RenderTextureFormat.R8);
            Graphics.Blit(_maskRT, tmp);
            Texture2D read = new Texture2D(down, down, TextureFormat.R8, false, true);
            var prev = RenderTexture.active; RenderTexture.active = tmp;
            read.ReadPixels(new Rect(0, 0, down, down), 0, 0); read.Apply(false);
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(tmp);

            // Маска: 1 — пыль. Чистота = 1 - среднее
            var data = read.GetRawTextureData<byte>();
            long sum = 0; int len = data.Length;
            for (int i = 0; i < len; i++) sum += data[i];
            Destroy(read);
            float avg = (sum / (255f * len)); // 0..1 пыль
            float cleanliness = Mathf.Clamp01(1f - avg);

            if (progressBar != null) progressBar.value = cleanliness;
            if (progressText != null) progressText.text = Mathf.RoundToInt(cleanliness * 100f) + "/100";
        }
    }
}

