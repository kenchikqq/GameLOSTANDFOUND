using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Визуальная лупа: по клавише показывает круглое окно, которое следует за курсором
/// и отображает увеличенную область под курсором. Работает без дополнительных шейдеров.
/// Достаточно повесить скрипт на пустой объект в сцене.
/// </summary>
public class VisualMagnifier : MonoBehaviour
{
    [Header("Toggle")]
    public KeyCode toggleKey = KeyCode.M;
    public bool startEnabled = false;
    public bool hideHardwareCursor = true;

    [Header("Raycast")]
    public LayerMask raycastMask = ~0;
    public float raycastMaxDistance = 50f;

    [Header("Zoom Camera")] 
    [Tooltip("Меньше FOV — больше увеличение")] 
    public float zoomFieldOfView = 18f;
    public float nearClip = 0.02f;
    public float farClip = 1000f;
    public Vector2Int renderTextureSize = new Vector2Int(512, 512);

    [Header("UI")] 
    public float diameterPixels = 220f;
    public bool clampInsideScreen = true;
    public bool showBorder = true;
    public float borderThicknessPixels = 4f;
    public Color borderColor = new Color(0f, 0f, 0f, 0.9f);

    private Camera mainCamera;
    private Camera magnifierCamera;
    private RenderTexture magnifierRT;

    private Canvas canvas;
    private RectTransform magnifierRoot;
    private Image maskImage;
    private RawImage magnifierView;
    private Image borderImage;

    private Texture2D generatedCircleMask;
    private Texture2D generatedRing;
    private Sprite circleMaskSprite;

    private bool isActive;

    void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("VisualMagnifier: В сцене не найдена MainCamera. Скрипт будет ожидать появления камеры.");
        }
    }

    void Start()
    {
        EnsureSetup();
        SetActive(startEnabled);
    }

    void OnDestroy()
    {
        if (magnifierRT != null)
        {
            magnifierRT.Release();
            Destroy(magnifierRT);
        }
        if (generatedCircleMask != null) Destroy(generatedCircleMask);
        if (generatedRing != null) Destroy(generatedRing);
        if (circleMaskSprite != null) Destroy(circleMaskSprite);
        if (magnifierCamera != null) Destroy(magnifierCamera.gameObject);
        if (canvas != null) Destroy(canvas.gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            SetActive(!isActive);
        }

        if (!isActive)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
            SyncCameraFromMain();
        }

        FollowMouse();
        AimMagnifierCamera();
    }

    private void EnsureSetup()
    {
        if (magnifierRT == null)
        {
            magnifierRT = new RenderTexture(renderTextureSize.x, renderTextureSize.y, 16, RenderTextureFormat.ARGB32)
            {
                name = "MagnifierRT",
                useMipMap = false,
                autoGenerateMips = false
            };
        }

        if (magnifierCamera == null)
        {
            var camGo = new GameObject("MagnifierCamera");
            camGo.transform.SetParent(transform, false);
            magnifierCamera = camGo.AddComponent<Camera>();
            magnifierCamera.enabled = true;
            magnifierCamera.targetTexture = magnifierRT;
            magnifierCamera.clearFlags = CameraClearFlags.Skybox;
            magnifierCamera.allowHDR = true;
            magnifierCamera.allowMSAA = true;
            SyncCameraFromMain();
        }

        if (canvas == null)
        {
            var canvasGo = new GameObject("MagnifierCanvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue; // поверх всего
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            magnifierRoot = new GameObject("MagnifierRoot").AddComponent<RectTransform>();
            magnifierRoot.SetParent(canvas.transform, false);
            magnifierRoot.sizeDelta = new Vector2(diameterPixels, diameterPixels);

            // Круглая маска
            maskImage = new GameObject("Mask").AddComponent<Image>();
            maskImage.transform.SetParent(magnifierRoot, false);
            maskImage.rectTransform.anchorMin = Vector2.zero;
            maskImage.rectTransform.anchorMax = Vector2.one;
            maskImage.rectTransform.offsetMin = Vector2.zero;
            maskImage.rectTransform.offsetMax = Vector2.zero;
            maskImage.raycastTarget = false;
            circleMaskSprite = GenerateCircleSprite(256);
            maskImage.sprite = circleMaskSprite;
            maskImage.type = Image.Type.Simple;
            maskImage.color = new Color(1, 1, 1, 1);
            maskImage.gameObject.AddComponent<Mask>();

            // Изображение увеличенной области
            magnifierView = new GameObject("View").AddComponent<RawImage>();
            magnifierView.transform.SetParent(maskImage.transform, false);
            magnifierView.rectTransform.anchorMin = Vector2.zero;
            magnifierView.rectTransform.anchorMax = Vector2.one;
            magnifierView.rectTransform.offsetMin = Vector2.zero;
            magnifierView.rectTransform.offsetMax = Vector2.zero;
            magnifierView.texture = magnifierRT;
            magnifierView.raycastTarget = false;

            // Бордер по желанию
            borderImage = new GameObject("Border").AddComponent<Image>();
            borderImage.transform.SetParent(magnifierRoot, false);
            borderImage.rectTransform.anchorMin = Vector2.zero;
            borderImage.rectTransform.anchorMax = Vector2.one;
            borderImage.rectTransform.offsetMin = Vector2.zero;
            borderImage.rectTransform.offsetMax = Vector2.zero;
            borderImage.raycastTarget = false;
            borderImage.sprite = GenerateRingSprite(256, Mathf.Clamp(borderThicknessPixels, 1f, diameterPixels * 0.5f));
            borderImage.type = Image.Type.Simple;
            borderImage.color = borderColor;
            borderImage.gameObject.SetActive(showBorder);
        }
    }

    private void SyncCameraFromMain()
    {
        if (magnifierCamera == null) return;
        if (mainCamera != null)
        {
            magnifierCamera.cullingMask = mainCamera.cullingMask;
            magnifierCamera.backgroundColor = mainCamera.backgroundColor;
            magnifierCamera.nearClipPlane = nearClip;
            magnifierCamera.farClipPlane = farClip;
            magnifierCamera.fieldOfView = zoomFieldOfView;
            magnifierCamera.orthographic = false;
        }
        else
        {
            magnifierCamera.nearClipPlane = nearClip;
            magnifierCamera.farClipPlane = farClip;
            magnifierCamera.fieldOfView = zoomFieldOfView;
        }
    }

    private void SetActive(bool active)
    {
        isActive = active;
        EnsureSetup();
        if (magnifierRoot != null) magnifierRoot.gameObject.SetActive(active);
        if (magnifierCamera != null) magnifierCamera.enabled = active;
        if (hideHardwareCursor) Cursor.visible = !active;
    }

    private void FollowMouse()
    {
        Vector2 mouse = Input.mousePosition;
        if (clampInsideScreen)
        {
            float half = diameterPixels * 0.5f;
            mouse.x = Mathf.Clamp(mouse.x, half, Screen.width - half);
            mouse.y = Mathf.Clamp(mouse.y, half, Screen.height - half);
        }
        magnifierRoot.position = mouse;
        magnifierRoot.sizeDelta = new Vector2(diameterPixels, diameterPixels);
    }

    private void AimMagnifierCamera()
    {
        if (mainCamera == null || magnifierCamera == null) return;

        magnifierCamera.transform.position = mainCamera.transform.position;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, raycastMaxDistance, raycastMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 target = hit.point;
            Vector3 up = mainCamera.transform.up;
            magnifierCamera.transform.rotation = Quaternion.LookRotation(target - magnifierCamera.transform.position, up);
        }
        else
        {
            magnifierCamera.transform.rotation = mainCamera.transform.rotation;
        }
    }

    private Sprite GenerateCircleSprite(int size)
    {
        generatedCircleMask = new Texture2D(size, size, TextureFormat.ARGB32, false);
        generatedCircleMask.name = "MagnifierMaskTex";
        generatedCircleMask.wrapMode = TextureWrapMode.Clamp;
        generatedCircleMask.filterMode = FilterMode.Bilinear;

        float r = (size - 1) * 0.5f;
        float cx = r;
        float cy = r;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = dist <= r ? 1f : 0f;
                generatedCircleMask.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        generatedCircleMask.Apply();
        return Sprite.Create(generatedCircleMask, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite GenerateRingSprite(int size, float thickness)
    {
        generatedRing = new Texture2D(size, size, TextureFormat.ARGB32, false);
        generatedRing.name = "MagnifierRingTex";
        generatedRing.wrapMode = TextureWrapMode.Clamp;
        generatedRing.filterMode = FilterMode.Bilinear;

        float rOuter = (size - 1) * 0.5f;
        float rInner = Mathf.Max(0f, rOuter - thickness);
        float cx = rOuter;
        float cy = rOuter;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = (dist <= rOuter && dist >= rInner) ? 1f : 0f;
                generatedRing.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        generatedRing.Apply();
        return Sprite.Create(generatedRing, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}


