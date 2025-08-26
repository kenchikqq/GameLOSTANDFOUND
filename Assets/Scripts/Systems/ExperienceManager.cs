using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace LostAndFound.Systems
{
    /// <summary>
    /// Centralized EXP/Level system with smooth UI animation.
    /// Level caps are configurable. Default: level 1→2 requires 100 EXP, then +50 per level, max level 10.
    /// Other systems should call the public Award* methods or AddExperience directly.
    /// </summary>
    public sealed class ExperienceManager : MonoBehaviour
    {
        public static ExperienceManager Instance { get; private set; }

        [Header("Level Settings")]
        [Tooltip("Maximum player level. At max level EXP stops increasing and the bar stays full.")]
        [Min(1)] public int maxLevel = 10;

        [Tooltip("Base EXP required to reach level 2.")]
        [Min(1)] public int baseExpToLevel2 = 100;

        [Tooltip("Additional EXP required per further level (linear growth). Example: 50 → L2=100, L3=150, L4=200...")]
        [Min(0)] public int expIncrementPerLevel = 50;

        [Header("EXP Rewards (tunable)")]
        [Tooltip("Accept an item from a white NPC.")]
        [Min(0)] public int rewardAcceptFromWhiteNpc = 5;

        [Tooltip("Return correct item to a black NPC.")]
        [Min(0)] public int rewardReturnToBlackNpc = 10;

        [Tooltip("Deliver a contraband item to police.")]
        [Min(0)] public int rewardGiveContrabandToPolice = 15;

        [Header("UI")]
        public GameObject expPanel;
        [Tooltip("Image with FillMethod Radial/Horizontal. Fill Amount will be animated 0..1.")]
        public Image expFill;
        public TextMeshProUGUI expText;   // e.g. 35/100
        public TextMeshProUGUI levelText; // e.g. 3
        [Tooltip("Seconds for a full bar animation (0..1).")]
        [Min(0.01f)] public float fillAnimationDuration = 0.5f;

        [Header("Events")]
        public UnityEvent<int> OnLevelUp;       // newLevel
        public UnityEvent<int, int> OnExpChanged; // currentExp, expToNext

        // Runtime state
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int currentExp = 0;

        private Coroutine animateRoutine;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            UpdateUIImmediate();
        }

        void OnValidate()
        {
            if (maxLevel < 1) maxLevel = 1;
            if (fillAnimationDuration < 0.01f) fillAnimationDuration = 0.01f;
            if (!Application.isPlaying)
            {
                UpdateUIImmediate();
            }
        }

        // ===== API =====

        public int GetLevel() => currentLevel;
        public int GetCurrentExp() => currentExp;
        public int GetExpToNextLevel() => IsMaxLevel() ? 0 : GetRequiredExpForLevel(currentLevel + 1);
        public float GetNormalizedProgress() => IsMaxLevel() ? 1f : Mathf.Clamp01((float)currentExp / GetExpToNextLevel());
        public bool IsMaxLevel() => currentLevel >= maxLevel;

        public void ShowPanel(bool show)
        {
            if (expPanel != null) expPanel.SetActive(show);
        }

        public void ResetProgress(int level = 1, int exp = 0)
        {
            currentLevel = Mathf.Clamp(level, 1, maxLevel);
            currentExp = Mathf.Max(0, exp);
            ClampExpWithinLevel();
            UpdateUIImmediate();
        }

        public void AddExperience(int amount, string reason = null)
        {
            if (amount <= 0 || IsMaxLevel())
            {
                // Still update UI to keep texts consistent
                UpdateUIImmediate();
                return;
            }

            StartCoroutine(AnimateGain(amount));
        }

        // Convenience wrappers for other systems
        public void AwardAcceptFromWhiteNpc() => AddExperience(rewardAcceptFromWhiteNpc, "WhiteNPCAccept");
        public void AwardReturnToBlackNpc() => AddExperience(rewardReturnToBlackNpc, "BlackNPCReturn");
        public void AwardGiveContrabandToPolice() => AddExperience(rewardGiveContrabandToPolice, "PoliceContraband");

        // ===== Internals =====

        private IEnumerator AnimateGain(int totalAmount)
        {
            int remaining = totalAmount;

            while (remaining > 0 && !IsMaxLevel())
            {
                int toNext = GetExpToNextLevel();
                int space = toNext - currentExp;
                int step = Mathf.Min(space, remaining);

                // Animate filling current level portion
                yield return AnimateFill(currentExp, currentExp + step, toNext);
                currentExp += step;
                remaining -= step;
                OnExpChanged?.Invoke(currentExp, toNext);

                // Level up if reached
                if (currentExp >= toNext && !IsMaxLevel())
                {
                    currentLevel = Mathf.Min(currentLevel + 1, maxLevel);
                    currentExp = 0;
                    OnLevelUp?.Invoke(currentLevel);
                    UpdateUIImmediate(); // reset bar to 0 for the new level
                }
            }

            // If at max level, fill to 1 visually
            if (IsMaxLevel())
            {
                UpdateUIImmediate();
            }
        }

        private IEnumerator AnimateFill(int fromExp, int toExp, int expToNext)
        {
            if (expFill == null && expText == null && levelText == null)
                yield break;

            float from = expToNext <= 0 ? 1f : Mathf.Clamp01((float)fromExp / expToNext);
            float to = expToNext <= 0 ? 1f : Mathf.Clamp01((float)toExp / expToNext);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, fillAnimationDuration);
                float v = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                if (expFill != null) expFill.fillAmount = v;
                if (expText != null) expText.text = $"{Mathf.RoundToInt(v * expToNext)}/{expToNext}";
                if (levelText != null) levelText.text = currentLevel.ToString();
                yield return null;
            }

            // Finalize exact values
            if (expFill != null) expFill.fillAmount = to;
            if (expText != null) expText.text = $"{toExp}/{expToNext}";
            if (levelText != null) levelText.text = currentLevel.ToString();
        }

        private void UpdateUIImmediate()
        {
            if (expFill != null)
                expFill.fillAmount = GetNormalizedProgress();

            if (expText != null)
            {
                if (IsMaxLevel()) expText.text = "MAX";
                else expText.text = $"{currentExp}/{GetExpToNextLevel()}";
            }

            if (levelText != null)
                levelText.text = currentLevel.ToString();
        }

        private void ClampExpWithinLevel()
        {
            if (IsMaxLevel())
            {
                currentExp = 0; // not used at max, bar forced to 1
                return;
            }
            int cap = GetExpToNextLevel();
            if (cap > 0) currentExp = Mathf.Clamp(currentExp, 0, cap - 1);
        }

        private int GetRequiredExpForLevel(int targetLevel)
        {
            if (targetLevel <= 1) return 0;
            int stepIndex = targetLevel - 2; // L2 is first step
            return baseExpToLevel2 + Mathf.Max(0, stepIndex) * expIncrementPerLevel;
        }
    }
}

