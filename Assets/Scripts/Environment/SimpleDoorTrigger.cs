using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using LostAndFound.NPCs;

namespace LostAndFound.Environment
{
    /// <summary>
    /// Простая дверь с триггером: когда NPC заходит в триггер — дверь открывается,
    /// через autoCloseDelay секунд после выхода всех — закрывается.
    /// Скрипт вешается на объект-триггер (IsTrigger = true).
    /// Поворот вокруг локального pivot двери.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimpleDoorTrigger : MonoBehaviour
    {
        [Header("Door")]
        [Tooltip("Трансформ створки. Пивот должен быть на петлях.")] public Transform doorTransform;
        [Tooltip("Плотный коллайдер двери, чтобы не мешал при открытии")] public Collider doorCollider;
        [Tooltip("NavMeshObstacle, чтобы агенты не проходили сквозь закрытую дверь")] public NavMeshObstacle navObstacle;
        [Tooltip("Угол открытия по оси Y (локально)")] public float openAngle = 90f;
        [Tooltip("Время открытия, сек")] public float openDuration = 0.3f;
        [Tooltip("Время закрытия, сек")] public float closeDuration = 0.3f;
        [Tooltip("Автозакрытие через, сек, после выхода последнего")] public float autoCloseDelay = 2f;
        [Tooltip("Открывать также от игрока (тег Player)")] public bool reactToPlayer = false;

        private Quaternion _closedRot;
        private Quaternion _openRot;
        private int _insideCount;
        private bool _isOpen;
        private Coroutine _motion;

        void Awake()
        {
            if (doorTransform == null) doorTransform = transform;
            _closedRot = doorTransform.localRotation;
            _openRot = _closedRot * Quaternion.Euler(0f, openAngle, 0f);

            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            // Гарантируем Rigidbody на триггере для событий OnTrigger*
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            if (doorCollider != null) doorCollider.enabled = true;
            if (navObstacle == null) navObstacle = GetComponent<NavMeshObstacle>();
            if (navObstacle != null) { navObstacle.enabled = true; navObstacle.carving = true; }
        }

        void OnTriggerEnter(Collider other)
        {
            if (!IsNpc(other)) return;
            _insideCount++;
            Open();
        }

        void OnTriggerExit(Collider other)
        {
            if (!IsNpc(other)) return;
            _insideCount = Mathf.Max(0, _insideCount - 1);
            if (_insideCount == 0)
            {
                StopCoroutineSafe(ref _motion);
                StartCoroutine(CloseAfterDelay());
            }
        }

        IEnumerator CloseAfterDelay()
        {
            yield return new WaitForSeconds(autoCloseDelay);
            if (_insideCount == 0)
            {
                Close();
            }
        }

        bool IsNpc(Collider other)
        {
            // Явные наши NPC
            if (other.GetComponentInParent<WhiteNPC>() != null) return true;
            if (other.GetComponentInParent<BlackNPC>() != null) return true;
            if (other.GetComponentInParent<PoliceOfficer>() != null) return true;

            // Любой агент навмеш (на случай будущих NPC)
            if (other.GetComponentInParent<NavMeshAgent>() != null) return true;

            // Игрок по желанию
            if (reactToPlayer && other.CompareTag("Player")) return true;

            return false;
        }

        public void Open()
        {
            if (_isOpen) return;
            StopCoroutineSafe(ref _motion);
            if (doorCollider) doorCollider.enabled = false;
            if (navObstacle) { navObstacle.carving = false; navObstacle.enabled = false; }
            _motion = StartCoroutine(AnimateRotation(_openRot, openDuration, true));
        }

        public void Close()
        {
            if (!_isOpen) return;
            StopCoroutineSafe(ref _motion);
            if (navObstacle) { navObstacle.enabled = true; navObstacle.carving = true; }
            _motion = StartCoroutine(AnimateRotation(_closedRot, closeDuration, false));
        }

        IEnumerator AnimateRotation(Quaternion target, float duration, bool setOpen)
        {
            Quaternion start = doorTransform.localRotation;
            float t = 0f;
            duration = Mathf.Max(0.01f, duration);
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                doorTransform.localRotation = Quaternion.Slerp(start, target, t);
                yield return null;
            }
            _isOpen = setOpen;
            if (!_isOpen && doorCollider) doorCollider.enabled = true;
            _motion = null;
        }

        void StopCoroutineSafe(ref Coroutine routine)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }
    }
}


