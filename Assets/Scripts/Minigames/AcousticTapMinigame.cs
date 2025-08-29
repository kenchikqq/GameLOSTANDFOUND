using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Minigames
{
    /// <summary>
    /// Полностью самодостаточная мини-игра «Простукивание» 6x3 (18 кнопок).
    /// - При первом запуске сама создаёт Canvas, Panel и 18 кнопок.
    /// - Одна кнопка — «contraband» (зелёная при нажатии), остальные — красные.
    /// - «Горячо/холодно»: чем ближе к целевой, тем выше тон клика.
    /// - Время игры ставится на паузу, курсор показывается.
    /// Для запуска просто активируй gameObject с этим компонентом (через MinigameRandomOnItem).
    /// </summary>
    public class AcousticTapMinigame : MonoBehaviour
    {
        [Header("UI Style")]
        [SerializeField] private Color correctPressed = new Color(0.2f, 0.9f, 0.2f, 1f);
        [SerializeField] private Color wrongPressed = new Color(0.95f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField] private Color panelBgColor = new Color(0.08f, 0.1f, 0.14f, 0.92f);
        [SerializeField] private string titleText = "Простукивание";
        [SerializeField] private string hintText = "Чем выше тон — тем ближе к контрабанде";
        [SerializeField] private bool showCloseButton = true;
        [SerializeField] private string closeButtonText = "Закрыть";

        [Header("Audio")]
        [SerializeField] private AudioClip tapClip;
        [SerializeField] [Range(0f, 1f)] private float baseVolume = 0.8f;
        [SerializeField] private float coldPitch = 0.85f;
        [SerializeField] private float hotPitch = 1.4f;
        [SerializeField] private float minNotePitch = 0.9f;  // индивидуальный тон кнопки
        [SerializeField] private float maxNotePitch = 1.2f;
        [SerializeField] private bool volumeGetsHotter = true; // громкость тоже растёт при приближении
        [SerializeField] private bool generateFallbackClick = true; // создать внутренний клип, если не задан

        private const int Columns = 6;
        private const int Rows = 3;
        private const int Total = Columns * Rows;
        [Header("Grid")]
        [SerializeField] private Vector2 gridCellSize = new Vector2(240, 240);
        [SerializeField] private Vector2 gridSpacing = new Vector2(16, 16);
        [SerializeField] private float cardPadding = 24f; // внутренняя рамка
        [SerializeField] private float headerSpace = 60f;
        [SerializeField] private float footerSpace = 40f;

        private RectTransform panel;
        private RectTransform gridContainer;
        private GridLayoutGroup grid;
        private readonly List<Button> buttons = new List<Button>(Total);
        private readonly List<float> basePitches = new List<float>(Total);
        private int targetIndex;
        private AudioSource audioSource;
        private GameObject uiRoot;
        private float prevTimeScale;
        private CursorLockMode prevLock;
        private bool prevCursor;

        private void Awake()
        {
            EnsureEventSystem();
            EnsureUI();
            EnsureAudio();
            if (uiRoot != null) uiRoot.SetActive(false);
        }

        private void OnMinigameStart()
        {
            SetupRound();
            if (uiRoot != null) uiRoot.SetActive(true);
            EnterModal();
        }

        private void OnMinigameStop()
        {
            if (uiRoot != null) uiRoot.SetActive(false);
            ExitModal();
        }

        private void EnsureUI()
        {
            Canvas canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("AcousticCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGO.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasGO.transform.SetParent(transform, false);
            }

            uiRoot = new GameObject("AcousticUI");
            uiRoot.transform.SetParent(canvas.transform, false);
            // Backdrop
            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(uiRoot.transform, false);
            var bdRt = backdrop.GetComponent<RectTransform>();
            bdRt.anchorMin = Vector2.zero;
            bdRt.anchorMax = Vector2.one;
            bdRt.offsetMin = Vector2.zero;
            bdRt.offsetMax = Vector2.zero;
            var bdImg = backdrop.GetComponent<Image>();
            bdImg.color = backdropColor;
            GameObject panelGO = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(uiRoot.transform, false);
            panel = panelGO.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(1f, 1f);
            panel.offsetMin = new Vector2(32f, 80f);
            panel.offsetMax = new Vector2(-32f, -80f);
            panelGO.GetComponent<Image>().color = panelBgColor;

            // Рамка вокруг сетки: Card
            var cardGO = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(panelGO.transform, false);
            var cardRT = cardGO.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            // размеры карточки вычислим ниже после настройки сетки
            cardRT.anchoredPosition = Vector2.zero;
            var cardImg = cardGO.GetComponent<Image>();
            cardImg.color = new Color(0.15f, 0.18f, 0.26f, 0.98f);

            // Тонкая внутренняя подложка
            var innerGO = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            innerGO.transform.SetParent(cardGO.transform, false);
            var innerRT = innerGO.GetComponent<RectTransform>();
            innerRT.anchorMin = new Vector2(0f, 0f);
            innerRT.anchorMax = new Vector2(1f, 1f);
            innerRT.offsetMin = new Vector2(cardPadding, cardPadding);
            innerRT.offsetMax = new Vector2(-cardPadding, -cardPadding);
            innerGO.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 1f);

            // Контейнер для сетки кнопок — НЕ меняем сами кнопки
            var gridWrap = new GameObject("GridContainer", typeof(RectTransform));
            gridWrap.transform.SetParent(innerGO.transform, false);
            gridContainer = gridWrap.GetComponent<RectTransform>();
            gridContainer.anchorMin = new Vector2(0.5f, 0.5f);
            gridContainer.anchorMax = new Vector2(0.5f, 0.5f);
            // вычислим размеры на основе размеров ячеек и отступов
            float gridW = Columns * gridCellSize.x + (Columns - 1) * gridSpacing.x;
            float gridH = Rows * gridCellSize.y + (Rows - 1) * gridSpacing.y;
            gridContainer.sizeDelta = new Vector2(gridW, gridH);
            gridContainer.anchoredPosition = Vector2.zero;

            grid = gridWrap.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            grid.spacing = gridSpacing;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.cellSize = gridCellSize;

            // теперь можем задать итоговые размеры карточки с учётом заголовка и подсказки
            cardRT.sizeDelta = new Vector2(gridW + 2 * cardPadding, gridH + 2 * cardPadding + headerSpace + footerSpace);

            CreateButtons();
            // Title (над карточкой)
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGO.transform.SetParent(cardGO.transform, false);
            var tRt = titleGO.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 1f);
            tRt.anchorMax = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, (cardRT.sizeDelta.y * 0.5f) - 30f);
            tRt.sizeDelta = new Vector2(1200f, 60f);
            var tText = titleGO.GetComponent<Text>();
            tText.text = titleText;
            tText.color = Color.white;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.fontSize = 42;
            tText.fontStyle = FontStyle.Bold;

            // Hint
            var hintGO = new GameObject("Hint", typeof(RectTransform), typeof(Text));
            hintGO.transform.SetParent(cardGO.transform, false);
            var hRt = hintGO.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.5f, 0f);
            hRt.anchorMax = new Vector2(0.5f, 0f);
            hRt.anchoredPosition = new Vector2(0f, (-cardRT.sizeDelta.y * 0.5f) + 28f);
            hRt.sizeDelta = new Vector2(1200f, 40f);
            var hText = hintGO.GetComponent<Text>();
            hText.text = hintText;
            hText.color = new Color(1f, 1f, 1f, 0.85f);
            hText.alignment = TextAnchor.MiddleCenter;
            hText.fontSize = 24;

            // Close Button
            if (showCloseButton)
            {
                var closeGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
                closeGO.transform.SetParent(cardGO.transform, false);
                var cRt = closeGO.GetComponent<RectTransform>();
                cRt.anchorMin = new Vector2(1f, 1f);
                cRt.anchorMax = new Vector2(1f, 1f);
                cRt.anchoredPosition = new Vector2(-16f, -16f);
                cRt.sizeDelta = new Vector2(160f, 48f);
                closeGO.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.35f, 0.95f);
                var closeBtn = closeGO.GetComponent<Button>();
                closeBtn.onClick.AddListener(() => OnMinigameStop());

                var cTextGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
                cTextGO.transform.SetParent(closeGO.transform, false);
                var cTR = cTextGO.GetComponent<RectTransform>();
                cTR.anchorMin = Vector2.zero;
                cTR.anchorMax = Vector2.one;
                cTR.offsetMin = Vector2.zero;
                cTR.offsetMax = Vector2.zero;
                var cText = cTextGO.GetComponent<Text>();
                cText.text = closeButtonText;
                cText.color = Color.white;
                cText.alignment = TextAnchor.MiddleCenter;
                cText.fontSize = 22;
            }
            if (uiRoot != null) uiRoot.SetActive(false);
        }

        private void CreateButtons()
        {
            buttons.Clear();
            for (int i = 0; i < Total; i++)
            {
                GameObject b = new GameObject($"Btn_{i+1}", typeof(RectTransform), typeof(Image), typeof(Button));
                // Родитель — контейнер с GridLayoutGroup, чтобы кнопки автоматически разложились в сетку
                b.transform.SetParent(gridContainer, false);
                var img = b.GetComponent<Image>();
                img.color = new Color(0.8f, 0.75f, 0.95f, 1f);
                var btn = b.GetComponent<Button>();
                int captured = i;
                btn.onClick.AddListener(() => OnPressed(captured));
                buttons.Add(btn);
            }
        }

        private void EnsureAudio()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
            audioSource.spatialBlend = 0f; // 2D звук

            if (tapClip == null && generateFallbackClick)
            {
                tapClip = CreateDefaultClickClip();
            }
        }

        private void SetupRound()
        {
            targetIndex = Random.Range(0, Total);

            for (int i = 0; i < buttons.Count; i++)
            {
                var cb = buttons[i].colors;
                cb.pressedColor = i == targetIndex ? correctPressed : wrongPressed;
                buttons[i].colors = cb;
            }

            // распределяем базовые тона по кнопкам (градиент слева направо)
            basePitches.Clear();
            for (int i = 0; i < buttons.Count; i++)
            {
                float t = buttons.Count <= 1 ? 0f : (float)i / (buttons.Count - 1);
                basePitches.Add(Mathf.Lerp(minNotePitch, maxNotePitch, t));
            }
        }

        private static AudioClip CreateDefaultClickClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.06f; // 60 мс короткий щелчок
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float frequency = 900f;

            var clip = AudioClip.Create("tap_fallback", samples, 1, sampleRate, false);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Exp(-t * 35f);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.7f;
            }
            clip.SetData(data, 0);
            return clip;
        }

        private void OnPressed(int index)
        {
            // звук «горячо/холодно»
            int dist = Manhattan(index, targetIndex);
            int max = (Columns - 1) + (Rows - 1);
            float proximity = 1f - (dist / (float)max);
            float proximityPitch = Mathf.Lerp(coldPitch, hotPitch, proximity);
            float buttonPitch = basePitches.Count > index ? basePitches[index] : 1f;
            float pitch = buttonPitch * proximityPitch;
            float volume = volumeGetsHotter ? Mathf.Lerp(0.6f, 1f, proximity) * baseVolume : baseVolume;
            if (tapClip != null)
            {
                audioSource.pitch = pitch;
                audioSource.PlayOneShot(tapClip, volume);
            }

            // если угадали – закрываем игру
            if (index == targetIndex)
            {
                OnMinigameStop();
            }
        }

        private static int Manhattan(int a, int b)
        {
            int ax = a % Columns; int ay = a / Columns;
            int bx = b % Columns; int by = b / Columns;
            return Mathf.Abs(ax - bx) + Mathf.Abs(ay - by);
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                if (uiRoot != null && !uiRoot.activeSelf)
                {
                    OnMinigameStart();
                }
            }
        }

        private void EnterModal()
        {
            prevTimeScale = Time.timeScale;
            prevLock = Cursor.lockState;
            prevCursor = Cursor.visible;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ExitModal()
        {
            Time.timeScale = prevTimeScale;
            Cursor.lockState = prevLock;
            Cursor.visible = prevCursor;
        }
    }
}


