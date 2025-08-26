using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LostAndFound.Systems
{
    /// <summary>
    /// СКРИПТ КОЛЕСА РУЛЕТКИ - только вращение и шарик
    /// Вешается на GameObject колеса рулетки
    /// </summary>
    public class RouletteWheel : MonoBehaviour
    {
        [Header("Настройки колеса")]
        public Transform wheelTransform; // Transform колеса для вращения
        public float minSpinTime = 3f; // Минимальное время вращения
        public float maxSpinTime = 6f; // Максимальное время вращения
        public AnimationCurve spinCurve = AnimationCurve.EaseInOut(0, 1, 1, 0); // Кривая замедления

        [Header("🎯 СТРЕЛОЧКА")]
        public RectTransform arrowImage; // Image стрелочки (назначь в Inspector)
        public float arrowSpeed = 720f; // Скорость вращения стрелочки (град/сек)
        public float arrowRadius = 5.0f; // Радиус движения стрелочки вокруг колеса
        public AnimationCurve arrowCurve = AnimationCurve.EaseInOut(0, 1, 1, 0.2f); // Кривая стрелочки
        public float arrowScale = 1f; // Размер стрелочки




        [Header("Числа на колесе (по порядку)")]
        public int[] wheelNumbers = { 0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10, 5, 24, 16, 33, 1, 20, 14, 31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26 };

        // Приватные переменные
        private bool isSpinning = false;
        private float currentWheelRotation = 0f;
        
        // 🎯 ПЛАВНАЯ АНИМАЦИЯ
        private float targetWheelRotation = 0f;
        private float animationProgress = 0f;
        private bool isAnimating = false;
        private int targetBallNumber = 0;

        void Start()
        {
            // 🔍 ПРОВЕРЯЕМ КОЛЕСО
            if (wheelTransform == null)
            {
                Debug.LogError("[RouletteWheel] ❌ Колесо не назначено! Назначь Transform колеса в Inspector!");
            }
            else
            {
                Debug.Log($"[RouletteWheel] ✅ Колесо назначено: {wheelTransform.name}");
            }

            // 🔍 ПРОВЕРЯЕМ СТРЕЛОЧКУ
            if (arrowImage == null)
            {
                Debug.LogError("[RouletteWheel] ❌ Стрелочка не назначена! Назначь Image стрелочки в Inspector!");
            }
            else
            {
                Debug.Log($"[RouletteWheel] ✅ Стрелочка назначена: {arrowImage.name}");
                // Устанавливаем начальную позицию стрелочки ВОКРУГ колеса
                SetupArrowInitialPosition();
            }

            Debug.Log("[RouletteWheel] 🎰 Рулетка инициализирована");
        }
        
        void Update()
        {
            // 🎯 ПЛАВНАЯ АНИМАЦИЯ КОЛЕСА
            if (isAnimating)
            {
                animationProgress += Time.deltaTime * arrowSpeed / 360f; // Плавное увеличение прогресса
                
                if (animationProgress >= 1f)
                {
                    // Анимация завершена
                    isAnimating = false;
                    isSpinning = false;
                    animationProgress = 1f;
                    
                    // Финальная позиция колеса
                    currentWheelRotation = targetWheelRotation;
                    
                    if (wheelTransform != null)
                    {
                        wheelTransform.rotation = Quaternion.Euler(0, 0, currentWheelRotation);
                    }
                    
                    Debug.Log($"[RouletteWheel] 🏆 Плавная анимация завершена! Результат: {targetBallNumber}");
                    
                    // ⏱️ ПАУЗА 5 СЕКУНД
                    StartCoroutine(ShowResultAfterDelay(targetBallNumber));
                }
                else
                {
                    // Плавная интерполяция колеса
                    float wheelCurveValue = spinCurve.Evaluate(animationProgress);
                    currentWheelRotation = Mathf.Lerp(0f, targetWheelRotation, wheelCurveValue);
                    
                    if (wheelTransform != null)
                    {
                        wheelTransform.rotation = Quaternion.Euler(0, 0, currentWheelRotation);
                    }
                }
            }
        }

        /// <summary>
        /// Устанавливает начальную позицию стрелочки
        /// </summary>
        void SetupArrowInitialPosition()
        {
            if (arrowImage == null) return;
            
            Debug.Log("[RouletteWheel] 🎯 Стрелочка готова к использованию");
        }



        // Удален метод FindExistingBall - теперь используем только назначение в Inspector

        // Удален метод SetupBallEffects - эффекты настраиваются в самом шарике

        /// <summary>
        /// Запускает плавное двойное вращение (колесо + стрелочка)
        /// </summary>
        public void SpinWheel(System.Action<int> onResult)
        {
            if (isSpinning || isAnimating)
            {
                Debug.LogWarning("[RouletteWheel] Колесо уже крутится!");
                return;
            }

            // 🎲 ГЕНЕРИРУЕМ СЛУЧАЙНОЕ ЧИСЛО (0-36)
            int resultNumber = Random.Range(0, 37);
            targetBallNumber = resultNumber;
            Debug.Log($"[RouletteWheel] 🎰 Начинаю плавное двойное вращение! Выпадет число: {resultNumber}");
            
            // Запускаем плавную анимацию
            StartPlaftAnimation(resultNumber, onResult);
        }

        /// <summary>
        /// Запускает плавную анимацию колеса
        /// </summary>
        void StartPlaftAnimation(int resultNumber, System.Action<int> onResult)
        {
            isSpinning = true;
            isAnimating = true;
            animationProgress = 0f;
            
            // Вычисляем финальный угол колеса
            float targetAngle = CalculateAngleForNumber(resultNumber);
            targetWheelRotation = targetAngle + (Random.Range(3, 6) * 360f);
            
            // Сохраняем callback
            this.onResultCallback = onResult;
            
            Debug.Log($"[RouletteWheel] 🎰 Плавная анимация колеса запущена! Цель: {resultNumber}");
        }
        
        /// <summary>
        /// Показывает результат после задержки
        /// </summary>
        IEnumerator ShowResultAfterDelay(int resultNumber)
        {
            Debug.Log("[RouletteWheel] ⏱️ Пауза 5 секунд для понимания результата...");
            yield return new WaitForSeconds(5f);
            
            // Вызываем callback с результатом
            onResultCallback?.Invoke(resultNumber);
        }
        
        // Переменная для хранения callback
        private System.Action<int> onResultCallback;
        


        // СТАРЫЕ МЕТОДЫ АНИМАЦИИ ШАРИКА УДАЛЕНЫ
        // Теперь анимация обрабатывается отдельным скриптом RouletteBall

        // СТАРЫЕ МЕТОДЫ PlaceBallAtNumber И GTA5BallLandingAnimation УДАЛЕНЫ
        // Теперь это обрабатывается в RouletteBall.cs



        /// <summary>
        /// Вычисляет угол для определенного числа на колесе
        /// </summary>
        float CalculateAngleForNumber(int number)
        {
            // Находим индекс числа в массиве
            int index = System.Array.IndexOf(wheelNumbers, number);
            if (index == -1)
            {
                Debug.LogError($"[RouletteWheel] Число {number} не найдено в wheelNumbers!");
                return 0f;
            }

            // Вычисляем угол (360° / 37 чисел)
            float anglePerSegment = 360f / wheelNumbers.Length;
            float angle = index * anglePerSegment;
            
            Debug.Log($"[RouletteWheel] 🎯 Число {number} находится на позиции {index}, угол: {angle}");
            return angle;
        }

        /// <summary>
        /// Проверяет, крутится ли колесо
        /// </summary>
        public bool IsSpinning()
        {
            return isSpinning;
        }

        /// <summary>
        /// Получает текущий угол колеса
        /// </summary>
        public float GetCurrentRotation()
        {
            return currentWheelRotation;
        }

        /// <summary>
        /// Сбрасывает стрелочку в начальную позицию
        /// </summary>
        public void ResetArrow()
        {
            if (arrowImage != null)
            {
                Debug.Log("[RouletteWheel] 🔄 Стрелочка сброшена");
            }
        }



        /// <summary>
        /// Рисует круг траектории стрелочки в редакторе
        /// </summary>
        void OnDrawGizmos()
        {
            // Рисуем круг траектории стрелочки
            Gizmos.color = Color.yellow;
            DrawWireCircle(transform.position, arrowRadius);
        }
        
        /// <summary>
        /// Рисует проволочный круг с помощью Gizmos
        /// </summary>
        void DrawWireCircle(Vector3 center, float radius)
        {
            int segments = 32;
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
    }
}