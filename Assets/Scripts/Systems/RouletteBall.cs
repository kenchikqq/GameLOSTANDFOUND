using UnityEngine;
using System.Collections;

namespace LostAndFound.Systems
{
    /// <summary>
    /// 🎰 Скрипт для шарика рулетки (GTA 5 стиль)
    /// Автономный компонент с красивой анимацией падения
    /// </summary>
    public class RouletteBall : MonoBehaviour
    {
        [Header("🎰 НАСТРОЙКИ ШАРИКА")]
        public float ballRadius = 1.2f; // Радиус движения вокруг колеса
        public float ballSpeed = 8f; // Скорость движения
        public float ballHeight = 0.5f; // Высота над колесом
        public float ballSize = 0.15f; // Размер шарика
        
        [Header("🎨 АНИМАЦИЯ")]
        public float spinDuration = 4f; // Время кручения (секунды)
        public float fallDuration = 2f; // Время падения (секунды)
        public AnimationCurve spinCurve = AnimationCurve.EaseInOut(0, 1, 1, 0.2f); // Кривая замедления
        public AnimationCurve fallCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Кривая падения
        
        [Header("🎵 ЭФФЕКТЫ")]
        public AudioSource rollSound; // Звук качения (опционально)
        public ParticleSystem sparks; // Искры при падении (опционально)
        
        [Header("📹 КАМЕРА ДЛЯ ВИДИМОСТИ")]
        public Camera ballCamera; // Камера для рендера шарика (назначь в Inspector)
        public RenderTexture ballRenderTexture; // RenderTexture для отображения
        public int renderTextureSize = 256; // Размер текстуры
        
        [Header("🎲 ЧИСЛА НА КОЛЕСЕ")]
        public int[] wheelNumbers = { 0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10, 5, 24, 16, 33, 1, 20, 14, 31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26 };
        
        // Приватные переменные
        private Transform rouletteCenter; // Центр колеса рулетки
        private bool isSpinning = false;
        private bool isFalling = false;
        private float currentAngle = 0f;
        private float currentSpinRotation = 0f;
        private Renderer ballRenderer;
        private Vector3 startPosition;
        
        // Callback для результата
        private System.Action<int> onResultCallback;
        
        void Start()
        {
            // Получаем Renderer
            ballRenderer = GetComponent<Renderer>();
            if (ballRenderer == null)
            {
                Debug.LogError("[RouletteBall] ❌ Нет Renderer компонента!");
                return;
            }
            
            // 📹 НАСТРАИВАЕМ КАМЕРУ ДЛЯ ВИДИМОСТИ
            SetupBallCamera();
            
            // 🎯 НАХОДИМ ЦЕНТР РУЛЕТКИ
            FindRouletteCenter();
            
            // 🎨 НАСТРАИВАЕМ МАТЕРИАЛ
            SetupBallMaterial();
            
            // 📐 УСТАНАВЛИВАЕМ РАЗМЕР
            transform.localScale = Vector3.one * ballSize;
            
            // 📍 НАЧАЛЬНАЯ ПОЗИЦИЯ
            SetStartPosition();
            
            // 🔴 ПРИНУДИТЕЛЬНАЯ ВИДИМОСТЬ
            ForceVisibility();
            
            Debug.Log("[RouletteBall] 🎰 Шарик с камерой инициализирован!");
        }
        
        /// <summary>
        /// Ищет центр рулетки (родительский объект или объект с RouletteWheel)
        /// </summary>
        void FindRouletteCenter()
        {
            // Сначала проверяем родительский объект
            if (transform.parent != null)
            {
                rouletteCenter = transform.parent;
                Debug.Log($"[RouletteBall] 🎯 Центр рулетки: {rouletteCenter.name} (родитель)");
                return;
            }
            
            // Ищем объект с RouletteWheel компонентом
            RouletteWheel rouletteWheel = FindObjectOfType<RouletteWheel>();
            if (rouletteWheel != null)
            {
                rouletteCenter = rouletteWheel.transform;
                Debug.Log($"[RouletteBall] 🎯 Центр рулетки: {rouletteCenter.name} (найден RouletteWheel)");
                return;
            }
            
            // Fallback - используем свою позицию как центр
            rouletteCenter = transform;
            Debug.LogWarning("[RouletteBall] ⚠️ Центр рулетки не найден, используем собственную позицию");
        }
        
        /// <summary>
        /// Настраивает камеру для лучшей видимости шарика
        /// </summary>
        void SetupBallCamera()
        {
            if (ballCamera == null)
            {
                // Ищем камеру среди дочерних объектов
                ballCamera = GetComponentInChildren<Camera>();
                if (ballCamera == null)
                {
                    Debug.LogWarning("[RouletteBall] ⚠️ Камера не найдена, но можно работать без неё");
                    return;
                }
            }
            
            // 🎥 СОЗДАЕМ RENDER TEXTURE
            if (ballRenderTexture == null)
            {
                ballRenderTexture = new RenderTexture(renderTextureSize, renderTextureSize, 24);
                ballRenderTexture.name = "BallRenderTexture";
            }
            
            // Настраиваем камеру
            ballCamera.targetTexture = ballRenderTexture;
            ballCamera.backgroundColor = Color.clear; // Прозрачный фон
            ballCamera.clearFlags = CameraClearFlags.SolidColor;
            ballCamera.cullingMask = -1; // Показывать все слои
            
            // Позиционируем камеру чтобы смотрела на шарик
            ballCamera.transform.localPosition = new Vector3(0, 0, -1f);
            ballCamera.transform.LookAt(transform);
            
            Debug.Log("[RouletteBall] 📹 Камера настроена с RenderTexture");
        }
        
        /// <summary>
        /// Принудительно делает шарик видимым
        /// </summary>
        void ForceVisibility()
        {
            // 🔴 МАКСИМАЛЬНАЯ ВИДИМОСТЬ
            gameObject.layer = 0; // Default слой
            gameObject.SetActive(true);
            
            if (ballRenderer != null)
            {
                ballRenderer.enabled = true;
                ballRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                ballRenderer.receiveShadows = true;
            }
            
            // 🎯 ЯРКИЙ ЦВЕТ ДЛЯ ОТЛАДКИ
            if (ballRenderer != null && ballRenderer.material != null)
            {
                ballRenderer.material.color = Color.yellow; // Ярко-желтый для видимости
            }
            
            Debug.Log($"[RouletteBall] 🔴 Принудительная видимость: позиция={transform.position}, активен={gameObject.activeInHierarchy}");
        }

        /// <summary>
        /// Настраивает красивый материал для шарика
        /// </summary>
        void SetupBallMaterial()
        {
            Material ballMaterial = new Material(Shader.Find("Standard"));
            if (ballMaterial.shader == null)
            {
                ballMaterial = new Material(Shader.Find("Legacy Shaders/Diffuse"));
            }
            
            // 🎨 ЯРКО-ЖЕЛТЫЙ ШАРИК ДЛЯ ВИДИМОСТИ
            ballMaterial.color = Color.yellow;
            
            if (ballMaterial.HasProperty("_Metallic"))
                ballMaterial.SetFloat("_Metallic", 0.8f);
            if (ballMaterial.HasProperty("_Smoothness"))
                ballMaterial.SetFloat("_Smoothness", 0.95f);
            
            ballRenderer.material = ballMaterial;
            
            Debug.Log("[RouletteBall] 🎨 Ярко-желтый материал настроен");
        }
        
        /// <summary>
        /// Устанавливает начальную позицию шарика
        /// </summary>
        void SetStartPosition()
        {
            if (rouletteCenter == null) return;
            
            startPosition = rouletteCenter.position + new Vector3(ballRadius, 0, ballHeight);
            transform.position = startPosition;
            
            Debug.Log($"[RouletteBall] 📍 Начальная позиция: {startPosition}");
        }
        
        /// <summary>
        /// Запускает анимацию кручения и падения шарика
        /// </summary>
        public void StartSpin(System.Action<int> onResult = null)
        {
            if (isSpinning || isFalling) return;
            
            onResultCallback = onResult;
            
            // Генерируем случайный результат
            int resultNumber = wheelNumbers[Random.Range(0, wheelNumbers.Length)];
            
            Debug.Log($"[RouletteBall] 🎰 Начинаю спин! Результат: {resultNumber}");
            
            StartCoroutine(SpinAnimation(resultNumber));
        }
        
        /// <summary>
        /// Корутина анимации кручения
        /// </summary>
        IEnumerator SpinAnimation(int resultNumber)
        {
            isSpinning = true;
            float elapsed = 0f;
            
            // 🔊 ЗВУК КРУЧЕНИЯ
            if (rollSound != null)
            {
                rollSound.Play();
            }
            
            // 🌀 КРУЧЕНИЕ ВОКРУГ КОЛЕСА
            while (elapsed < spinDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / spinDuration;
                float curveProgress = spinCurve.Evaluate(progress);
                
                // Скорость движения по кругу
                float currentSpeed = ballSpeed * (1 - curveProgress);
                currentAngle += currentSpeed * Time.deltaTime * 100f;
                
                // Позиция по кругу
                Vector3 circlePosition = rouletteCenter.position + new Vector3(
                    Mathf.Cos(currentAngle * Mathf.Deg2Rad) * ballRadius,
                    Mathf.Sin(currentAngle * Mathf.Deg2Rad) * ballRadius,
                    0
                );
                circlePosition.z = ballHeight;
                
                transform.position = circlePosition;
                
                // 🌪️ ВРАЩЕНИЕ ШАРИКА
                currentSpinRotation += currentSpeed * Time.deltaTime * 360f;
                transform.rotation = Quaternion.Euler(
                    currentSpinRotation * 0.7f, 
                    currentSpinRotation, 
                    currentSpinRotation * 1.3f
                );
                
                yield return null;
            }
            
            isSpinning = false;
            
            // 🎯 ПЕРЕХОДИМ К ПАДЕНИЮ
            StartCoroutine(FallAnimation(resultNumber));
        }
        
        /// <summary>
        /// Корутина анимации падения на число
        /// </summary>
        IEnumerator FallAnimation(int resultNumber)
        {
            isFalling = true;
            
            Vector3 startPos = transform.position;
            Vector3 endPos = CalculateNumberPosition(resultNumber);
            
            float elapsed = 0f;
            
            Debug.Log($"[RouletteBall] 🎯 Падаю на число {resultNumber}");
            
            // ✨ ЭФФЕКТЫ ПРИ ПАДЕНИИ
            if (sparks != null)
            {
                sparks.Play();
            }
            
            // 🏀 АНИМАЦИЯ ПАДЕНИЯ
            while (elapsed < fallDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / fallDuration;
                float curveProgress = fallCurve.Evaluate(progress);
                
                // Плавное движение к целевой позиции
                Vector3 currentPos = Vector3.Lerp(startPos, endPos, curveProgress);
                
                // Добавляем подпрыгивание в конце
                if (progress > 0.7f)
                {
                    float bouncePhase = (progress - 0.7f) / 0.3f;
                    float bounceHeight = Mathf.Sin(bouncePhase * Mathf.PI * 2f) * 0.1f * (1f - bouncePhase);
                    currentPos.z += bounceHeight;
                }
                
                transform.position = currentPos;
                
                // Замедляющееся вращение
                float spinSpeed = (1f - progress) * 180f * Time.deltaTime;
                currentSpinRotation += spinSpeed;
                transform.rotation = Quaternion.Euler(
                    currentSpinRotation * 0.3f, 
                    currentSpinRotation * 0.7f, 
                    currentSpinRotation
                );
                
                yield return null;
            }
            
            // Финальная позиция
            transform.position = endPos;
            
            // 🔕 ОСТАНАВЛИВАЕМ ЗВУКИ
            if (rollSound != null)
            {
                rollSound.Stop();
            }
            
            if (sparks != null)
            {
                sparks.Stop();
            }
            
            isFalling = false;
            
            Debug.Log($"[RouletteBall] 🏆 Шарик упал на число: {resultNumber}");
            
            // Вызываем callback с результатом
            onResultCallback?.Invoke(resultNumber);
        }
        
        /// <summary>
        /// Вычисляет позицию для определенного числа на колесе
        /// </summary>
        Vector3 CalculateNumberPosition(int number)
        {
            // Находим индекс числа в массиве
            int index = System.Array.IndexOf(wheelNumbers, number);
            if (index == -1) index = 0; // Fallback
            
            // Вычисляем угол (360 градусов разделены на количество чисел)
            float angle = (360f / wheelNumbers.Length) * index;
            
            // Позиция ближе к центру (в "лунке" числа)
            Vector3 numberPosition = rouletteCenter.position + new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * ballRadius * 0.7f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * ballRadius * 0.7f,
                0
            );
            numberPosition.z = ballHeight * 0.3f; // Ниже, как будто в лунке
            
            return numberPosition;
        }
        
        /// <summary>
        /// Сбрасывает шарик в начальную позицию
        /// </summary>
        public void ResetPosition()
        {
            if (isSpinning || isFalling) return;
            
            transform.position = startPosition;
            transform.rotation = Quaternion.identity;
            currentAngle = 0f;
            currentSpinRotation = 0f;
            
            Debug.Log("[RouletteBall] 🔄 Позиция сброшена");
        }
        
        /// <summary>
        /// Проверяет, крутится ли шарик
        /// </summary>
        public bool IsAnimating()
        {
            return isSpinning || isFalling;
        }
    }
}