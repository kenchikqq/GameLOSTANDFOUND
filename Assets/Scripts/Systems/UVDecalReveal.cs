using UnityEngine;
using UnityEngine.Rendering.Universal; // DecalProjector

namespace LostAndFound.Systems
{
	/// <summary>
	/// Управляет прозрачностью URP Decal Projector в зависимости от УФ-фонарика.
	/// Ожидает, что в материале есть float-параметр "_RevealAlpha" (0..1).
	/// Если его нет — пытается править альфу "_BaseColor"/"_Color".
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class UVDecalReveal : MonoBehaviour
	{
		[Header("Decal")]
		[Tooltip("Decal Projector на этом объекте")] public DecalProjector projector;
		[Tooltip("Инстанцировать материал, чтобы не влиять на шаблон")] public bool instantiateMaterial = true;

		[Header("Sampling")] 
		[Tooltip("Рендерер предмета, на который проецируем (для ближней точки)")] public Renderer targetRenderer;
		[Tooltip("Если нет рендера — использовать эту точку для оценки")] public Transform samplePoint;
		[Tooltip("Брать ближайшую к источнику УФ точку на рендерере")] public bool sampleClosestPoint = true;

		[Header("Reveal")] 
		[Tooltip("Скорость плавного появления/исчезновения")] public float fadeSpeed = 6f;
		[Tooltip("Интенсивность УФ для полной видимости")] public float intensityForFull = 0.75f;
		[Tooltip("Порог видимости (ниже — невидимо)")] public float minVisibleIntensity = 0.1f;

		[Header("Debug")] 
		public bool debugForceVisible = false;
		[Range(0f,1f)] public float debugAlpha = 1f;

		private static readonly int ID_RevealAlpha = Shader.PropertyToID("_RevealAlpha");
		private static readonly int ID_BaseColor = Shader.PropertyToID("_BaseColor");
		private static readonly int ID_Color = Shader.PropertyToID("_Color");
		// UV light params for per-pixel reveal in shader
		private static readonly int ID_UVLightPos = Shader.PropertyToID("_UVLightPos");
		private static readonly int ID_UVLightDir = Shader.PropertyToID("_UVLightDir");
		private static readonly int ID_UVLightRange = Shader.PropertyToID("_UVLightRange");
		private static readonly int ID_UVSpotCosCutoff = Shader.PropertyToID("_UVSpotCosCutoff");
		private static readonly int ID_UVInnerSoftness = Shader.PropertyToID("_UVInnerSoftness");
		private static readonly int ID_UVEnabled = Shader.PropertyToID("_UVEnabled");

		// Decal projector basis to build projector UVs in graph (world → local)
		private static readonly int ID_DecalPos = Shader.PropertyToID("_DecalPos");
		private static readonly int ID_DecalRight = Shader.PropertyToID("_DecalRight");
		private static readonly int ID_DecalUp = Shader.PropertyToID("_DecalUp");
		private static readonly int ID_DecalForward = Shader.PropertyToID("_DecalForward");
		private static readonly int ID_DecalExtents = Shader.PropertyToID("_DecalExtents"); // (halfWidth, halfHeight, halfDepth, 0)

		private Material _mat;
		private float _currentAlpha;

		void Reset()
		{
			projector = GetComponent<DecalProjector>();
			targetRenderer = GetComponentInParent<Renderer>();
			samplePoint = transform;
		}

		void Awake()
		{
			if (projector == null) projector = GetComponent<DecalProjector>();
			if (projector != null && projector.material != null)
			{
				_mat = instantiateMaterial ? new Material(projector.material) : projector.material;
				if (instantiateMaterial) projector.material = _mat;
			}
		}

		void Update()
		{
			if (_mat == null || projector == null) return;

			float targetAlpha = 0f;
			if (debugForceVisible) targetAlpha = debugAlpha;
			else
			{
				Vector3 p = samplePoint != null ? samplePoint.position : transform.position;
				if (UVFlashlight.Instance != null && UVFlashlight.Instance.SpotLight != null && sampleClosestPoint && targetRenderer != null)
				{
					p = targetRenderer.bounds.ClosestPoint(UVFlashlight.Instance.SpotLight.transform.position);
				}
				float intensity = (UVFlashlight.Instance != null) ? UVFlashlight.Instance.EvaluateIntensityAtPoint(p) : 0f;
				if (intensity >= minVisibleIntensity)
					targetAlpha = Mathf.Clamp01(intensity / Mathf.Max(0.0001f, intensityForFull));
			}

			_currentAlpha = Mathf.MoveTowards(_currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
			ApplyAlpha(_currentAlpha);
			ApplyUVLightParams();
			ApplyDecalBasis();
		}

		void ApplyAlpha(float a)
		{
			if (_mat == null) return;
			if (_mat.HasProperty(ID_RevealAlpha))
			{
				_mat.SetFloat(ID_RevealAlpha, a);
				return;
			}
			// Фоллбек: правим альфу цвета
			if (_mat.HasProperty(ID_BaseColor))
			{
				var c = _mat.GetColor(ID_BaseColor); c.a = a; _mat.SetColor(ID_BaseColor, c);
				return;
			}
			if (_mat.HasProperty(ID_Color))
			{
				var c = _mat.GetColor(ID_Color); c.a = a; _mat.SetColor(ID_Color, c);
			}
		}

		void ApplyDecalBasis()
		{
			if (_mat == null || projector == null) return;
			Transform t = projector.transform;
			if (_mat.HasProperty(ID_DecalPos)) _mat.SetVector(ID_DecalPos, t.position);
			if (_mat.HasProperty(ID_DecalRight)) _mat.SetVector(ID_DecalRight, t.right);
			if (_mat.HasProperty(ID_DecalUp)) _mat.SetVector(ID_DecalUp, t.up);
			if (_mat.HasProperty(ID_DecalForward)) _mat.SetVector(ID_DecalForward, t.forward);
			// Extents from projector size (width, height, depth)
			Vector3 size = Vector3.one;
			#if UNITY_2021_2_OR_NEWER
			try { size = projector.size; } catch { size = new Vector3(1f, 1f, 0.05f); }
			#else
			size = new Vector3(1f,1f,0.05f);
			#endif
			Vector3 half = size * 0.5f;
			if (_mat.HasProperty(ID_DecalExtents)) _mat.SetVector(ID_DecalExtents, new Vector4(half.x, half.y, half.z, 0f));
		}

		void ApplyUVLightParams()
		{
			if (_mat == null) return;
			var uv = UVFlashlight.Instance;
			bool enabled = uv != null && uv.SpotLight != null && uv.IsEnabled;
			if (_mat.HasProperty(ID_UVEnabled)) _mat.SetFloat(ID_UVEnabled, enabled ? 1f : 0f);
			if (!enabled) return;
			var lt = uv.SpotLight.transform;
			if (_mat.HasProperty(ID_UVLightPos)) _mat.SetVector(ID_UVLightPos, lt.position);
			if (_mat.HasProperty(ID_UVLightDir)) _mat.SetVector(ID_UVLightDir, lt.forward);
			if (_mat.HasProperty(ID_UVLightRange)) _mat.SetFloat(ID_UVLightRange, uv.range);
			if (_mat.HasProperty(ID_UVInnerSoftness)) _mat.SetFloat(ID_UVInnerSoftness, Mathf.Clamp01(uv.innerAngleSoftness));
			float cutoff = Mathf.Cos(Mathf.Deg2Rad * (uv.spotAngle * 0.5f));
			if (_mat.HasProperty(ID_UVSpotCosCutoff)) _mat.SetFloat(ID_UVSpotCosCutoff, cutoff);
		}
	}
}


