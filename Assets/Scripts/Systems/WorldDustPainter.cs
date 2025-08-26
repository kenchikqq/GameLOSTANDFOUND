using UnityEngine;

namespace LostAndFound.Systems
{
    /// <summary>
    /// Реальная чистка пыли на 3D-предмете. Пишет в runtime-маску (_DustMask) по UV при ЛКМ.
    /// Шейдер должен иметь свойства: _DustMask (Texture2D), _PlacementMask (Texture2D, опц.), _DustStrength (float).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldDustPainter : MonoBehaviour
    {
        [Header("Target")]
        public Renderer targetRenderer;
        [Tooltip("Коллайдер с UV. Нужен MeshCollider (IsTrigger можно)")]
        public MeshCollider meshCollider;

        [Header("Masks & Material")]        
        [Tooltip("Маска размещения пыли по UV (белое = где может быть пыль)")]
        public Texture2D placementMask;
        [Tooltip("Тайлинг маски размещения (_MaskTiling) для удобной подстройки из инспектора")] public Vector2 maskTiling = new Vector2(1f, 1f);
        [Tooltip("Смещение маски размещения (_MaskOffset) для позиционирования области")] public Vector2 maskOffset = Vector2.zero;
        [Tooltip("Размер runtime-маски пыли (_DustMask)")]
        public int maskSize = 1024;
        [Tooltip("Пыль с самого начала покрывает всю область размещения")]
        public bool startFullyDusty = true;

        [Header("Brush")]
        public float brushSizePixels = 96f;
        [Range(0.05f, 1f)] public float hardness = 0.8f;
        [Range(0.05f, 1f)] public float strength = 0.4f;
        public LayerMask raycastMask = ~0;
        public bool onlyWhenInspecting = true;
        [Tooltip("Какая кнопка мыши рисует: 0 = ЛКМ, 1 = ПКМ, 2 = СКМ")] public int paintMouseButton = 1;
        [Tooltip("Бросать луч из центра экрана (true) или по позиции курсора (false)")]
        public bool useScreenCenterRay = true;
        [Tooltip("Использовать вторые UV (Lightmap UVs). Включай, если на UV0 острова пересекаются (как у дефолтного куба)")]
        public bool useUV2 = true;

        [Header("Debug")]
        public bool debugHotkeys = false; // F6 очистить, F7 запылить, F8 мазок по центру UV

        private Texture2D _dustMask;
        private Color32[] _pixels;
        private MaterialPropertyBlock _mpb;
        [Header("Raycast Camera Override")]
        [Tooltip("Камера, из которой бросаем лучи. Если не задано — возьмём Camera.main, иначе первую найденную камеру.")]
        public Camera raycastCamera;
        private Camera _cam;
        public float raycastMaxDistance = 100f;

        void Reset()
        {
            targetRenderer = GetComponent<Renderer>();
            meshCollider = GetComponent<MeshCollider>();
        }

        void Awake()
        {
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            if (meshCollider == null) meshCollider = GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                Debug.LogWarning("[WorldDustPainter] Нужен MeshCollider для получения textureCoord.", this);
            }
            _cam = raycastCamera != null ? raycastCamera : Camera.main;
            _mpb = new MaterialPropertyBlock();
            InitMasks();
        }

        void InitMasks()
        {
            _dustMask = new Texture2D(maskSize, maskSize, TextureFormat.RGBA32, false, true);
            _pixels = new Color32[maskSize * maskSize];
            // Важно: держим R синхронным с A, т.к. некоторые шейдеры читают маску как max(R, A)
            byte v = startFullyDusty ? (byte)255 : (byte)0;
            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = new Color32(v, v, v, v);
            _dustMask.SetPixels32(_pixels);
            _dustMask.Apply(false, false);
            PushToMaterial();
        }

        void PushToMaterial()
        {
            if (targetRenderer == null) return;
            targetRenderer.GetPropertyBlock(_mpb);
            _mpb.SetTexture("_DustMask", _dustMask);
            if (placementMask != null) _mpb.SetTexture("_PlacementMask", placementMask);
            _mpb.SetVector("_MaskTiling", maskTiling);
            _mpb.SetVector("_MaskOffset", maskOffset);
            _mpb.SetFloat("_DustStrength", 1f);
            targetRenderer.SetPropertyBlock(_mpb);

            // Fallback: продублируем значения напрямую в экземпляр материала,
            // чтобы исключить возможные проблемы с PropertyBlock на некоторых рендерах/батчерах
            try
            {
                var mat = targetRenderer.material; // создаст инстанс материала для этого рендера
                if (mat != null)
                {
                    if (mat.HasProperty("_DustMask")) mat.SetTexture("_DustMask", _dustMask);
                    if (mat.HasProperty("_PlacementMask") && placementMask != null) mat.SetTexture("_PlacementMask", placementMask);
                    if (mat.HasProperty("_MaskTiling")) mat.SetVector("_MaskTiling", maskTiling);
                    if (mat.HasProperty("_MaskOffset")) mat.SetVector("_MaskOffset", maskOffset);
                    if (mat.HasProperty("_DustStrength")) mat.SetFloat("_DustStrength", 1f);
                }
            }
            catch { /* игнор */ }
        }

        private Camera ResolveRaycastCamera()
        {
            if (raycastCamera != null) return raycastCamera;
            if (Camera.main != null) return Camera.main;
            Camera[] cams = Camera.allCameras;
            Camera best = null;
            for (int i = 0; i < cams.Length; i++)
            {
                var c = cams[i];
                if (!c.isActiveAndEnabled) continue;
                if (c.targetDisplay != 0) continue; // работаем только с основным дисплеем
                if (best == null || c.depth > best.depth) best = c;
            }
            return best;
        }

        void Update()
        {
            if (onlyWhenInspecting)
            {
                // Разрешаем чистку только в режиме осмотра предмета
                if (PlayerInventory.Instance == null || !PlayerInventory.Instance.IsInspecting()) return;
            }

            _cam = ResolveRaycastCamera();

            if (Input.GetMouseButtonDown(paintMouseButton)) Debug.Log($"[WorldDustPainter] MouseButton {paintMouseButton} down — начинаю красить", this);
            if (Input.GetMouseButton(paintMouseButton)) TryPaintUnderCursor();

            if (debugHotkeys)
            {
                if (Input.GetKeyDown(KeyCode.F6)) { FillMask(0); Debug.Log("[WorldDustPainter] F6: очистил маску", this); }
                if (Input.GetKeyDown(KeyCode.F7)) { FillMask(255); Debug.Log("[WorldDustPainter] F7: запылил маску", this); }
                if (Input.GetKeyDown(KeyCode.F8)) { PaintAtUV(new Vector2(0.5f, 0.5f)); Debug.Log("[WorldDustPainter] F8: мазок по центру", this); }
            }
        }

        void TryPaintUnderCursor()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null || meshCollider == null) return;

            if (_cam == null) return;
            Ray ray;
            if (useScreenCenterRay)
            {
                var center = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
                ray = _cam.ScreenPointToRay(center);
            }
            else
            {
                ray = _cam.ScreenPointToRay(Input.mousePosition);
            }
            // 1) Сначала пробуем напрямую пересечь наш MeshCollider — игнорируя остальные коллайдеры
            // Сначала — точный Raycast по MeshCollider из выбранной камеры
            if (meshCollider.Raycast(ray, out var meshHitDirect, raycastMaxDistance))
            {
                PaintAtUV(GetHitUV(meshHitDirect));
                return;
            }

            // Попробуем все активные камеры на Display 1 — вдруг override указан не та камера
            var cams = Camera.allCameras;
            for (int i = 0; i < cams.Length; i++)
            {
                var c = cams[i];
                if (c == null || !c.isActiveAndEnabled) continue;
                if (c.targetDisplay != 0) continue;
                if (c == _cam) continue;
                var r = c.ScreenPointToRay(Input.mousePosition);
                if (meshCollider.Raycast(r, out var mh, raycastMaxDistance))
                {
                    PaintAtUV(GetHitUV(mh));
                    return;
                }
            }

            // 2) Фоллбек: Physics.Raycast только по слою цели (исключаем пол и прочее)
            int targetLayerMask = meshCollider != null ? (1 << meshCollider.gameObject.layer) : raycastMask;
            if (Physics.Raycast(ray, out var hit, raycastMaxDistance, targetLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (debugHotkeys)
                {
                    var n = hit.collider != null ? hit.collider.name : "<none>";
                    Debug.Log($"[WorldDustPainter] Raycast hit (not our mesh): {n}", this);
                }
                // Если всё-таки попали в нужный MeshCollider через общий Raycast — используем его UV
                if (hit.collider == meshCollider)
                {
                    PaintAtUV(GetHitUV(hit));
                    return;
                }
            }

            // 3) Последняя попытка: RaycastAll по всем слоям и поиск нашего MeshCollider среди попаданий
            var allHits = Physics.RaycastAll(ray, raycastMaxDistance, ~0, QueryTriggerInteraction.Ignore);
            if (allHits != null && allHits.Length > 0)
            {
                System.Array.Sort(allHits, (a, b) => a.distance.CompareTo(b.distance));
                for (int i = 0; i < allHits.Length; i++)
                {
                    var h = allHits[i];
                    if (h.collider == null) continue;
                    if (h.collider == meshCollider || h.collider.GetComponentInParent<WorldDustPainter>() == this)
                    {
                        PaintAtUV(GetHitUV(h));
                        return;
                    }
                }
            }

            if (debugHotkeys)
            {
                Debug.Log("[WorldDustPainter] Raycast miss", this);
            }
        }

        void PaintAtUV(Vector2 uv)
        {
            if (_dustMask == null || _pixels == null) return;
            int cx = Mathf.RoundToInt(uv.x * (maskSize - 1));
            int cy = Mathf.RoundToInt(uv.y * (maskSize - 1));
            int rs = Mathf.CeilToInt(brushSizePixels);
            bool changed = false;
            for (int y = -rs; y <= rs; y++)
            {
                int py = cy + y; if (py < 0 || py >= maskSize) continue;
                for (int x = -rs; x <= rs; x++)
                {
                    int px = cx + x; if (px < 0 || px >= maskSize) continue;
                    float dist = Mathf.Sqrt(x * x + y * y);
                    if (dist > brushSizePixels) continue;
                    float t = dist / brushSizePixels; // 0 в центре, 1 на краю
                    // Перо с плавным градиентом: используем сглаженную кривую
                    t = Mathf.SmoothStep(1f, 0f, t);
                    t = Mathf.Pow(t, Mathf.Lerp(1f, 3f, 1f - hardness));
                    int idx = py * maskSize + px;
                    var c = _pixels[idx];
                    byte old = c.a;
                    byte sub = (byte)Mathf.RoundToInt(255f * (t * strength));
                    byte na = (byte)Mathf.Max(0, old - sub);
                    if (na < old)
                    {
                        c.a = na;
                        c.r = na; // синхронизируем R с альфой для совместимости со шейдерами
                        _pixels[idx] = c; changed = true;
                    }
                }
            }
            if (changed)
            {
                _dustMask.SetPixels32(_pixels);
                _dustMask.Apply(false, false);
                PushToMaterial();
            }
        }

        void FillMask(byte alpha)
        {
            if (_pixels == null) return;
            for (int i = 0; i < _pixels.Length; i++) { var c = _pixels[i]; c.a = alpha; c.r = alpha; _pixels[i] = c; }
            _dustMask.SetPixels32(_pixels); _dustMask.Apply(false, false); PushToMaterial();
        }

        // Возвращает корректные UV: используем UV2, если включено (для куба/объектов с наложенными UV0), иначе UV0
        private Vector2 GetHitUV(RaycastHit hit)
        {
            if (useUV2)
            {
#if UNITY_2021_2_OR_NEWER
                return hit.textureCoord2;
#else
                return hit.textureCoord; // запасной путь для старых версий
#endif
            }
            return hit.textureCoord;
        }
    }
}

