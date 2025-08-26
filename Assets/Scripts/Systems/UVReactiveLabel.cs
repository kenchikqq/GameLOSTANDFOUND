using UnityEngine;
using UnityEngine.UI;

namespace LostAndFound.Systems
{
	/// <summary>
	/// Простая «УФ-надпись» без шейдеров: рисует спрайт на world-space Canvas поверх предмета
	/// и плавно управляет альфой в зависимости от интенсивности УФ-фонарика.
	/// Подходит для любых мешей, не трогает материалы предмета.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class UVReactiveLabel : MonoBehaviour
	{
		[Header("Target")]
		[Tooltip("Рендерер предмета (нужен только чтобы брать ближайшую к фонарю точку)")] public Renderer targetRenderer;
		[Tooltip("Якорь для наклейки. Если пусто — берётся трансформ объекта со скриптом")] public Transform anchor;

		[Header("Label Sprite")]
		[Tooltip("PNG-спрайт с альфой. Белое на прозрачном — это сама надпись")] public Sprite labelSprite;
		[Tooltip("Цвет/оттенок надписи")] public Color labelTint = new Color(1f, 0.95f, 0.7f, 1f);
		[Tooltip("Размер наклейки в метрах (ширина, высота)")] public Vector2 worldSize = new Vector2(0.2f, 0.1f);
		[Tooltip("Смещение вперёд по направлению на камеру (м) — чтобы не проваливалась в меш")] public float cameraZOffset = 0.003f;

		[Header("Appearance")] 
		[Tooltip("Скорость плавного появления/исчезновения")] public float fadeSpeed = 6f;
		[Tooltip("Интенсивность УФ для полного проявления (0..1)")] public float intensityForFull = 0.75f;
		[Tooltip("Минимум, ниже которого полностью невидимо")] public float minVisibleIntensity = 0.1f;
		[Tooltip("Брать точку на рендерере, ближайшую к источнику света")] public bool sampleClosestPoint = true;

		[Header("Billboard")]
		[Tooltip("Поворачивать наклейку лицом к активной камере")] public bool faceCamera = true;
		[Tooltip("Автопоиск камеры, если не задана")] public Camera overrideCamera;

		[Header("Debug")]
		[Tooltip("Показывать всегда (для настройки), игнорируя УФ-луч")] public bool debugForceVisible = false;
		[Range(0f,1f)] public float debugAlpha = 1f;

		// Runtime
		private Canvas _canvas;
		private RectTransform _rect;
		private Image _image;
		private float _currentAlpha;

		void Reset()
		{
			anchor = transform;
			targetRenderer = GetComponent<Renderer>();
		}

		void Awake()
		{
			EnsureCanvas();
			ApplySpriteAndTint();
		}

		void OnValidate()
		{
			if (anchor == null) anchor = transform;
			EnsureCanvas();
			ApplySpriteAndTint();
			ApplyWorldSize();
		}

		void Update()
		{
			if (_canvas == null || _image == null) { EnsureCanvas(); ApplySpriteAndTint(); }

			// Позиция и поворот
			var cam = overrideCamera != null ? overrideCamera : Camera.main;
			Vector3 basePos = (anchor != null ? anchor.position : transform.position);
			if (cam != null)
			{
				// Небольшой вынос в сторону камеры, чтобы исключить z-fighting
				basePos += cam.transform.forward * cameraZOffset;
				if (faceCamera) _canvas.transform.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
			}
			_canvas.transform.position = basePos;

			// Интенсивность УФ
			float targetAlpha = 0f;
			if (debugForceVisible)
			{
				targetAlpha = debugAlpha;
			}
			else
			{
				Vector3 evalPoint = basePos;
				if (UVFlashlight.Instance != null && sampleClosestPoint && targetRenderer != null && UVFlashlight.Instance.SpotLight != null)
				{
					// точка на рендерере, ближайшая к источнику света
					evalPoint = targetRenderer.bounds.ClosestPoint(UVFlashlight.Instance.SpotLight.transform.position);
				}
				float intensity = (UVFlashlight.Instance != null) ? UVFlashlight.Instance.EvaluateIntensityAtPoint(evalPoint) : 0f;
				if (intensity >= minVisibleIntensity)
				{
					targetAlpha = Mathf.Clamp01(intensity / Mathf.Max(0.0001f, intensityForFull));
				}
			}

			// Плавная альфа
			_currentAlpha = Mathf.MoveTowards(_currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
			var c = _image.color; c.a = _currentAlpha; _image.color = c;
		}

		void EnsureCanvas()
		{
			if (_canvas != null && _rect != null && _image != null) return;
			// Создаём дочерний world-space Canvas
			var go = new GameObject("UVLabelCanvas");
			go.layer = gameObject.layer;
			go.transform.SetParent(anchor != null ? anchor : transform, worldPositionStays: false);
			_canvas = go.AddComponent<Canvas>();
			_canvas.renderMode = RenderMode.WorldSpace;
			_canvas.sortingOrder = 5000;
			_rect = go.GetComponent<RectTransform>();
			_rect.pivot = new Vector2(0.5f, 0.5f);
			_rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
			// 1x1 и масштабом зададим реальные метры
			_rect.sizeDelta = Vector2.one;
			ApplyWorldSize();
			_image = go.AddComponent<Image>();
			_image.raycastTarget = false;
			_image.preserveAspect = true;
			// Начально невидима
			var c = labelTint; c.a = 0f; _image.color = c;
		}

		void ApplyWorldSize()
		{
			if (_rect == null) return;
			_rect.localScale = new Vector3(Mathf.Max(1e-4f, worldSize.x), Mathf.Max(1e-4f, worldSize.y), 1f);
		}

		void ApplySpriteAndTint()
		{
			if (_image == null) return;
			_image.sprite = labelSprite;
			var c = labelTint; c.a = _currentAlpha; _image.color = c;
		}
	}
}


