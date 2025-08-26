using UnityEngine;

namespace LostAndFound.Systems
{
    /// <summary>
    /// УФ-фонарик для режима осмотра. Создаёт/управляет Spot Light фиолетового цвета,
    /// даёт публичный метод вычисления интенсивности луча в произвольной мировой точке.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UVFlashlight : MonoBehaviour
    {
        public static UVFlashlight Instance { get; private set; }

        [Header("Включение")]
        [Tooltip("Клавиша включения/выключения")] public KeyCode toggleKey = KeyCode.U;
        [Tooltip("Работает только в режиме осмотра предмета")] public bool onlyWhenInspecting = true;
        [Tooltip("Включать фонарик при входе в режим осмотра")] public bool autoEnableOnInspect = true;

        [Header("Параметры луча")]
        [Tooltip("Источник света. Если не назначен — будет создан автоматически как дочерний объект.")]
        public Light spotLight;
        [Range(5f, 120f)] public float spotAngle = 40f;
        [Range(0.1f, 1f)] public float innerAngleSoftness = 0.3f; // мягкая сердцевина луча
        public float range = 5f;
        public Color lightColor = new Color(0.65f, 0.25f, 0.85f); // фиолетовый
        public float lightIntensity = 4f;

        [Header("Привязка")]
        [Tooltip("Если задано — фонарик будет следовать за этим трансформом (например, камерой осмотра)")]
        public Transform followTarget;
        public Vector3 localOffset = new Vector3(0f, 0f, 0.2f);

        private bool _isEnabled;

        public bool IsEnabled => _isEnabled;
        public Light SpotLight => spotLight;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EnsureLight();
            ApplyParamsToLight();
            SetEnabled(false, true);
        }

        void EnsureLight()
        {
            if (spotLight == null)
            {
                var go = new GameObject("UV Spot Light");
                go.transform.SetParent(transform, false);
                spotLight = go.AddComponent<Light>();
                spotLight.type = LightType.Spot;
            }
        }

        void ApplyParamsToLight()
        {
            if (spotLight == null) return;
            spotLight.spotAngle = spotAngle;
            spotLight.range = range;
            spotLight.color = lightColor;
            spotLight.intensity = lightIntensity;
            spotLight.enabled = _isEnabled;
        }

        void Update()
        {
            bool allow = !onlyWhenInspecting || (PlayerInventory.Instance != null && PlayerInventory.Instance.IsInspecting());

            if (!allow)
            {
                if (_isEnabled) SetEnabled(false);
            }
            else
            {
                if (Input.GetKeyDown(toggleKey)) SetEnabled(!_isEnabled);
            }

            if (followTarget != null)
            {
                transform.position = followTarget.position;
                transform.rotation = followTarget.rotation;
                spotLight.transform.localPosition = localOffset;
            }

            // Параметры могли поменяться в инспекторе
            ApplyParamsToLight();
        }

        public void SetEnabled(bool enabled, bool instant = false)
        {
            _isEnabled = enabled;
            if (spotLight != null) spotLight.enabled = enabled;
        }

        /// <summary>
        /// Возвращает нормированную интенсивность луча (0..1) в указанной мировой точке.
        /// Учитывает угол и расстояние, с мягкой сердцевиной.
        /// </summary>
        public float EvaluateIntensityAtPoint(Vector3 worldPoint)
        {
            if (!_isEnabled || spotLight == null) return 0f;

            Vector3 toPoint = worldPoint - spotLight.transform.position;
            float distance = toPoint.magnitude;
            if (distance > range || distance <= 0.0001f) return 0f;

            Vector3 dir = toPoint / distance;
            float cosAngle = Vector3.Dot(spotLight.transform.forward, dir);
            float cutoffCos = Mathf.Cos(Mathf.Deg2Rad * (spotLight.spotAngle * 0.5f));
            if (cosAngle <= cutoffCos) return 0f;

            // Угловой спад: мягче в центре
            float angleFactor = Mathf.InverseLerp(cutoffCos, 1f, cosAngle);
            float core = Mathf.Clamp01(angleFactor / Mathf.Max(0.0001f, innerAngleSoftness));
            float angular = Mathf.Lerp(angleFactor * angleFactor, 1f, core);

            // Дистанционный спад
            float distance01 = Mathf.Clamp01(distance / range);
            float distanceFactor = 1f - distance01;

            return Mathf.Clamp01(angular * distanceFactor);
        }
    }
}


