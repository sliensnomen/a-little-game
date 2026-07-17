using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public GameObject startPanel;
    public GameObject hudPanel;
    public GameObject winPanel;
    public GameObject losePanel;

    public InputField nameInput;
    public Button startButton;

    public Text exposureText;
    public Text domainText;
    public Text comboText;
    public Text holyLightText;
    public Text loseNameText;
    public Text winNameText;

    public DialogueController dialogueController;

    public Button simulateHitButton;
    public Button simulateMissButton;
    public Button simulateWinButton;
    public Button simulateLoseButton;

    private Text[] trueNameSlots;
    private GameObject trueNameSlotsContainer;
    private Text phaseIndicatorText;
    private Text revealHintText;
    private GameObject guessPanel;
    private InputField guessInput;
    private Button guessConfirmButton;
    private int guessCharacterLimit = 6;
    private GameState lastState = GameState.Intro;

    private Text nameWarningText;
    private Color originalButtonColor;
    private Outline startButtonOutline;

    private Image[] exposureLayerDots;
    private Sprite dotSprite;

    private float lastExposure = 0f;
    private int lastRevealedMask = 0;

    private Button winRetryButton;
    private Button loseRetryButton;

    public bool IsGuessPanelOpen => guessPanel != null && guessPanel.activeSelf;

    Font GetUIFont()
    {
        var fm = FontManager.Instance;
        if (fm != null && fm.GetUIFont() != null) return fm.GetUIFont();
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        FontManager.EnsureExists();
        CinematicOverlay.EnsureExists();
        CinematicCamera.EnsureExists();
        AudioManager.EnsureExists();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged.AddListener(OnStateChanged);
            OnStateChanged(GameManager.Instance.State);
        }

        var trueNameSystem = TrueNameSystem.Instance ?? FindObjectOfType<TrueNameSystem>();
        if (trueNameSystem != null)
        {
            trueNameSystem.OnLetterRevealed.AddListener(UpdateTrueNameSlots);
            trueNameSystem.OnTemporaryReveal.AddListener(() => UpdateTrueNameSlots(-1));
            trueNameSystem.OnTemporaryHide.AddListener(() => UpdateTrueNameSlots(-1));
            trueNameSystem.OnExposureChanged.AddListener(UpdateExposure);
            trueNameSystem.OnExposureChanged.AddListener(OnExposureDeltaChanged);
            trueNameSystem.OnDomainChargeChanged.AddListener(UpdateDomain);
            trueNameSystem.OnExposureLayerChanged.AddListener(UpdateExposureLayer);
            trueNameSystem.OnLose.AddListener(() => GameManager.Instance?.TriggerLose());
        }

        HolyLightSystem.EnsureExists();
        var holyLightSystem = HolyLightSystem.Instance ?? FindObjectOfType<HolyLightSystem>();
        if (holyLightSystem != null)
            holyLightSystem.OnHolyLightChanged.AddListener(UpdateHolyLight);

        var inputManager = InputManager.Instance ?? FindObjectOfType<InputManager>();
        if (inputManager != null)
            inputManager.OnGuessPressed.AddListener(ShowGuessPanel);

        var phaseManager = PhaseManager.Instance ?? FindObjectOfType<PhaseManager>();
        if (phaseManager != null)
            phaseManager.OnPhaseChanged.AddListener(OnPhaseChanged);

        if (dialogueController == null) dialogueController = FindObjectOfType<DialogueController>();
        EnsureWinNameText();
        EnsureComboText();
        EnsureHolyLightText();
        EnsureDomainText();
        EnsureExposureLayerDots();
        EnsureGuessPanel();

        UpdateHolyLight(HolyLightSystem.Instance?.HolyLight ?? 0f);
        UpdateExposure(TrueNameSystem.Instance?.PlayerExposure ?? 0f);
        UpdateDomain(TrueNameSystem.Instance?.DomainCharge ?? 0f);
        UpdateExposureLayer(TrueNameSystem.Instance?.ExposureLayer ?? 0);

        startButton.onClick.AddListener(OnStartClicked);

        if (nameInput != null)
        {
            nameInput.contentType = InputField.ContentType.Alphanumeric;
            nameInput.characterLimit = 12;
            nameInput.onValueChanged.AddListener(UpdateStartButtonState);
        }

        if (startButton != null && startButton.image != null)
            originalButtonColor = startButton.image.color;

        EnsureWarningText();
        UpdateStartButtonState(nameInput?.text ?? "");

        if (simulateHitButton != null) simulateHitButton.onClick.AddListener(SimulateHit);
        if (simulateMissButton != null) simulateMissButton.onClick.AddListener(SimulateMiss);
        if (simulateWinButton != null) simulateWinButton.onClick.AddListener(() => GameManager.Instance?.TriggerWin());
        if (simulateLoseButton != null) simulateLoseButton.onClick.AddListener(() => GameManager.Instance?.TriggerLose());
    }

    void OnStartClicked()
    {
        GameManager.Instance?.SetPlayerName(nameInput?.text);
        GameManager.Instance?.StartCombat();
    }

    bool IsValidNameChar(char c)
    {
        return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
    }

    void UpdateStartButtonState(string text)
    {
        if (startButton == null) return;

        bool valid = !string.IsNullOrWhiteSpace(text);
        if (valid)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (!IsValidNameChar(text[i]))
                {
                    valid = false;
                    break;
                }
            }
        }

        if (valid)
        {
            startButton.interactable = true;
            if (startButton.image != null)
            {
                Color c = originalButtonColor;
                c.a = 1f;
                startButton.image.color = c;
            }
            AddGoldenGlow();
            HideNameWarning();
        }
        else
        {
            startButton.interactable = false;
            if (startButton.image != null)
            {
                Color c = originalButtonColor;
                c.a = 0.3f;
                startButton.image.color = c;
            }
            RemoveGoldenGlow();
            ShowNameWarning();
        }
    }

    void AddGoldenGlow()
    {
        if (startButton == null) return;
        Text btnText = startButton.GetComponentInChildren<Text>();
        if (btnText == null) return;
        if (startButtonOutline == null)
            startButtonOutline = btnText.GetComponent<Outline>() ?? btnText.gameObject.AddComponent<Outline>();
        startButtonOutline.enabled = true;
        startButtonOutline.effectColor = new Color(0.831f, 0.686f, 0.216f);
        startButtonOutline.effectDistance = new Vector2(2f, -2f);
    }

    void RemoveGoldenGlow()
    {
        if (startButtonOutline != null)
            startButtonOutline.enabled = false;
    }

    void EnsureWarningText()
    {
        if (nameWarningText != null) return;
        if (startPanel == null) return;
        Transform t = startPanel.transform.Find("NameWarningText");
        if (t != null)
        {
            nameWarningText = t.GetComponent<Text>();
            return;
        }
        GameObject go = new GameObject("NameWarningText", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(startPanel.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -100f);
        rt.sizeDelta = new Vector2(600f, 40f);
        Text txt = go.GetComponent<Text>();
        txt.font = GetUIFont();
        txt.fontSize = 22;
        txt.color = new Color(0.8f, 0.2f, 0.2f);
        txt.alignment = TextAnchor.MiddleCenter;
        nameWarningText = txt;
        go.SetActive(false);
    }

    void ShowNameWarning()
    {
        if (nameWarningText == null) EnsureWarningText();
        if (nameWarningText == null) return;
        nameWarningText.text = "无名者无法进入王座之间。";
        nameWarningText.gameObject.SetActive(true);
    }

    void HideNameWarning()
    {
        if (nameWarningText == null) return;
        nameWarningText.gameObject.SetActive(false);
    }

    void SimulateHit()
    {
        TrueNameSystem.Instance?.RevealLetter();
    }

    void SimulateMiss()
    {
        TrueNameSystem.Instance?.ExposePlayer(GameManager.Instance?.config?.exposurePerMiss ?? 4f);
    }

    void OnStateChanged(GameState state)
    {
        HideAll();
        DialogueController.Instance?.Hide();
        switch (state)
        {
            case GameState.Intro:
                CinematicOverlay.Instance?.SetLetterbox(0f);
                startPanel.SetActive(true);
                AudioManager.Instance?.PlayBGM();
                break;
            case GameState.Combat:
                CinematicOverlay.Instance?.SetLetterbox(CinematicOverlay.Instance != null ? CinematicOverlay.Instance.combatLetterboxRatio : 0.12f);
                hudPanel.SetActive(true);
                AudioManager.Instance?.PlayBGM();
                EnsureTrueNameSlots();
                UpdateTrueNameSlots(-1);
                EnsureExposureLayerDots();
                UpdateHolyLight(HolyLightSystem.Instance?.HolyLight ?? 0f);
                lastExposure = TrueNameSystem.Instance?.PlayerExposure ?? 0f;
                UpdateExposure(lastExposure);
                EnsureDomainText();
                UpdateDomain(TrueNameSystem.Instance?.DomainCharge ?? 0f);
                UpdateExposureLayer(TrueNameSystem.Instance?.ExposureLayer ?? 0);
                EnsurePhaseIndicator();
                UpdatePhaseIndicator(PhaseManager.Instance?.CurrentPhaseIndex ?? 0);
                if (lastState == GameState.Intro)
                {
                    UpdateCombo(0);
                    DialogueController.Instance?.Show("真名剥露开始。", 2.5f);
                }
                break;
            case GameState.Domain:
                CinematicOverlay.Instance?.SetLetterbox(CinematicOverlay.Instance != null ? CinematicOverlay.Instance.combatLetterboxRatio : 0.12f);
                hudPanel.SetActive(true);
                AudioManager.Instance?.PlayBGM();
                break;
            case GameState.Win:
                CinematicOverlay.Instance?.SetLetterbox(CinematicOverlay.Instance != null ? CinematicOverlay.Instance.endLetterboxRatio : 0.18f);
                AudioManager.Instance?.StopBGM();
                StartCoroutine(WinSequence());
                break;
            case GameState.Lose:
                CinematicOverlay.Instance?.SetLetterbox(CinematicOverlay.Instance != null ? CinematicOverlay.Instance.endLetterboxRatio : 0.18f);
                AudioManager.Instance?.StopBGM();
                StartCoroutine(LoseSequence());
                break;
        }
        lastState = state;
    }

    void EnsureWinNameText()
    {
        if (winNameText != null) return;
        if (winPanel == null) return;
        Transform t = winPanel.transform.Find("WinNameText");
        if (t != null)
        {
            winNameText = t.GetComponent<Text>();
            return;
        }
        GameObject go = new GameObject("WinNameText", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(winPanel.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.4f);
        rt.anchorMax = new Vector2(0.5f, 0.4f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(600, 60);
        Text txt = go.GetComponent<Text>();
        txt.font = GetUIFont();
        txt.fontSize = 32;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        winNameText = txt;
    }

    void TriggerLoseEffects()
    {
        CameraShake.Instance?.Shake(0.4f, 0.6f);
    }

    void EnsureComboText()
    {
        if (comboText != null) return;
        if (hudPanel == null) return;
        Transform t = hudPanel.transform.Find("ComboText");
        if (t != null)
        {
            comboText = t.GetComponent<Text>();
            return;
        }
        GameObject go = new GameObject("ComboText", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(hudPanel.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-40f, -40f);
        rt.sizeDelta = new Vector2(200f, 60f);
        Text txt = go.GetComponent<Text>();
        txt.font = GetUIFont();
        txt.fontSize = 32;
        txt.color = Color.white;
        txt.alignment = TextAnchor.UpperRight;
        comboText = txt;
    }

    public void UpdateCombo(int combo)
    {
        if (comboText == null) return;
        comboText.text = "连击: " + combo;
        comboText.color = combo >= 3 ? new Color(1f, 0.84f, 0f) : Color.white;
    }

    void HideAll()
    {
        startPanel.SetActive(false);
        hudPanel.SetActive(false);
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        if (guessPanel != null) guessPanel.SetActive(false);
    }

    void EnsureTrueNameSlots()
    {
        if (trueNameSlots != null) return;
        if (hudPanel == null) return;

        int len = TrueNameSystem.Instance != null && !string.IsNullOrEmpty(TrueNameSystem.Instance.EnemyTrueName)
            ? TrueNameSystem.Instance.EnemyTrueName.Length
            : (FindObjectOfType<TrueNameSystem>() != null && !string.IsNullOrEmpty(FindObjectOfType<TrueNameSystem>().EnemyTrueName)
                ? FindObjectOfType<TrueNameSystem>().EnemyTrueName.Length
                : 6);

        GameObject container = new GameObject("TrueNameSlots", typeof(RectTransform));
        trueNameSlotsContainer = container;
        container.transform.SetParent(hudPanel.transform, false);
        container.transform.SetAsLastSibling();

        RectTransform crt = container.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 1f);
        crt.anchorMax = new Vector2(0.5f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.anchoredPosition = new Vector2(0f, -120f);
        crt.sizeDelta = new Vector2(len * 100f + 40f, 140f);

        GameObject labelGO = new GameObject("TrueNameLabel", typeof(RectTransform), typeof(Text));
        labelGO.transform.SetParent(container.transform, false);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0.5f, 1f);
        labelRT.anchorMax = new Vector2(0.5f, 1f);
        labelRT.pivot = new Vector2(0.5f, 1f);
        labelRT.anchoredPosition = new Vector2(0f, 10f);
        labelRT.sizeDelta = new Vector2(300f, 40f);
        Text label = labelGO.GetComponent<Text>();
        label.text = "敌方真名";
        label.font = GetUIFont();
        label.fontSize = 22;
        label.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        label.alignment = TextAnchor.MiddleCenter;
        Outline labelOutline = labelGO.AddComponent<Outline>();
        labelOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        labelOutline.effectDistance = new Vector2(2f, -2f);

        trueNameSlots = new Text[len];
        for (int i = 0; i < len; i++)
        {
            GameObject slot = new GameObject("Slot" + i, typeof(RectTransform), typeof(Text));
            slot.transform.SetParent(container.transform, false);
            RectTransform rt = slot.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2((i - (len - 1) / 2f) * 100f, -20f);
            rt.sizeDelta = new Vector2(90f, 90f);
            Text t = slot.GetComponent<Text>();
            t.font = GetUIFont();
            t.fontSize = 56;
            t.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            t.alignment = TextAnchor.MiddleCenter;
            t.text = "_";
            Outline outline = slot.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);
            trueNameSlots[i] = t;
        }
    }

    void UpdateTrueNameSlots(int index)
    {
        var tns = TrueNameSystem.Instance ?? FindObjectOfType<TrueNameSystem>();
        if (trueNameSlots == null || tns == null || string.IsNullOrEmpty(tns.EnemyTrueName)) return;
        string name = tns.EnemyTrueName;
        int newMask = tns.RevealedMask;
        for (int i = 0; i < trueNameSlots.Length; i++)
        {
            if (i >= name.Length) continue;
            bool revealed = tns.IsLetterRevealed(i);
            if (revealed && trueNameSlots[i].text == "_")
            {
                trueNameSlots[i].text = name[i].ToString();
                trueNameSlots[i].color = new Color(0.831f, 0.686f, 0.216f);
                StartCoroutine(RevealSlotAnimation(trueNameSlots[i], name[i].ToString()));
            }
            else if (!revealed)
            {
                trueNameSlots[i].text = "_";
                trueNameSlots[i].color = new Color(0.7f, 0.7f, 0.7f);
            }
        }
        lastRevealedMask = newMask;
        UpdateRevealHint(tns.IsAllRevealed);
        if (tns.IsAllRevealed)
            StartCoroutine(HighlightAllSlots());
    }

    IEnumerator RevealSlotAnimation(Text slot, string letter)
    {
        if (slot == null) yield break;
        RectTransform rt = slot.GetComponent<RectTransform>();
        Vector3 baseScale = rt != null ? rt.localScale : Vector3.one;
        Color baseColor = new Color(0.831f, 0.686f, 0.216f);
        slot.text = letter;

        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.unscaledDeltaTime;
            float p = t / 0.25f;
            if (rt != null) rt.localScale = baseScale * (1f + Mathf.Sin(p * Mathf.PI) * 0.4f);
            Color c = baseColor;
            c.a = Mathf.Lerp(0.5f, 1f, p);
            slot.color = c;
            yield return null;
        }

        if (rt != null) rt.localScale = baseScale;
        slot.color = baseColor;
    }

    IEnumerator HighlightAllSlots()
    {
        if (trueNameSlots == null) yield break;
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.unscaledDeltaTime;
            float p = t / 0.6f;
            Color c = Color.Lerp(new Color(0.831f, 0.686f, 0.216f), Color.white, Mathf.Sin(p * Mathf.PI));
            for (int i = 0; i < trueNameSlots.Length; i++)
            {
                if (trueNameSlots[i] != null)
                    trueNameSlots[i].color = c;
            }
            yield return null;
        }

        for (int i = 0; i < trueNameSlots.Length; i++)
        {
            if (trueNameSlots[i] != null)
                trueNameSlots[i].color = new Color(0.831f, 0.686f, 0.216f);
        }
    }

    Vector2 GetSlotWorldPosition(Text slot)
    {
        if (slot == null || slot.rectTransform == null) return Vector2.zero;
        Vector3 worldPos = slot.rectTransform.position;
        return new Vector2(worldPos.x, worldPos.y);
    }

    void EnsureRevealHint()
    {
        if (revealHintText != null) return;
        if (trueNameSlotsContainer == null) return;
        Transform t = trueNameSlotsContainer.transform.Find("RevealHint");
        if (t != null)
        {
            revealHintText = t.GetComponent<Text>();
            return;
        }
        GameObject go = new GameObject("RevealHint", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(trueNameSlotsContainer.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, 50f);
        rt.sizeDelta = new Vector2(400f, 30f);
        Text txt = go.GetComponent<Text>();
        txt.font = GetUIFont();
        txt.fontSize = 20;
        txt.color = new Color(0.831f, 0.686f, 0.216f);
        txt.alignment = TextAnchor.MiddleCenter;
        revealHintText = txt;
        go.SetActive(false);
    }

    void UpdateRevealHint(bool show)
    {
        EnsureRevealHint();
        if (revealHintText == null) return;
        revealHintText.gameObject.SetActive(show);
        if (show)
            revealHintText.text = "按 Enter 确认真名";
    }

    public void RebuildTrueNameSlots()
    {
        if (trueNameSlotsContainer != null)
        {
            Destroy(trueNameSlotsContainer);
            trueNameSlotsContainer = null;
        }
        trueNameSlots = null;
        lastRevealedMask = 0;
        EnsureTrueNameSlots();
        UpdateTrueNameSlots(-1);
    }

    void OnPhaseChanged(int phase)
    {
        RebuildTrueNameSlots();
        if (guessPanel != null)
        {
            Destroy(guessPanel);
            guessPanel = null;
            guessInput = null;
            guessConfirmButton = null;
        }
        EnsureGuessPanel();
        UpdatePhaseIndicator(phase);
    }

    void EnsurePhaseIndicator()
    {
        if (phaseIndicatorText != null) return;
        if (hudPanel == null) return;
        Transform t = hudPanel.transform.Find("PhaseIndicatorText");
        if (t != null)
        {
            phaseIndicatorText = t.GetComponent<Text>();
            return;
        }
        GameObject go = new GameObject("PhaseIndicatorText", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(hudPanel.transform, false);
        go.transform.SetAsLastSibling();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(40f, -40f);
        rt.sizeDelta = new Vector2(200f, 40f);
        Text txt = go.GetComponent<Text>();
        txt.font = GetUIFont();
        txt.fontSize = 24;
        txt.color = new Color(0.831f, 0.686f, 0.216f);
        txt.alignment = TextAnchor.UpperLeft;
        phaseIndicatorText = txt;
    }

    public void UpdatePhaseIndicator(int phase)
    {
        EnsurePhaseIndicator();
        if (phaseIndicatorText != null)
            phaseIndicatorText.text = "阶段 " + (phase + 1);
    }

    public void ShowPhaseTitle(int phase)
    {
        StartCoroutine(PhaseTitleRoutine(phase));
    }

    IEnumerator PhaseTitleRoutine(int phase)
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) yield break;

        GameObject go = new GameObject("PhaseTitle", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(800f, 120f);
        rt.localScale = Vector3.one * 1.5f;

        Text txt = go.GetComponent<Text>();
        txt.font = GetUIFont();
        txt.fontSize = 64;
        txt.color = new Color(0.831f, 0.686f, 0.216f, 0f);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = "第 " + (phase + 1) + " 层真名";
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(3f, -3f);

        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            float p = t / 0.4f;
            Color c = txt.color;
            c.a = Mathf.Lerp(0f, 1f, p);
            txt.color = c;
            rt.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, p);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(1.2f);

        t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            float p = t / 0.4f;
            Color c = txt.color;
            c.a = Mathf.Lerp(1f, 0f, p);
            txt.color = c;
            yield return null;
        }

        Destroy(go);
    }

    void EnsureHolyLightText()
    {
        if (holyLightText != null) return;
        if (hudPanel == null) return;
        Transform t = hudPanel.transform.Find("HolyLightText");
        if (t != null)
        {
            holyLightText = t.GetComponent<Text>();
            return;
        }
        GameObject go = new GameObject("HolyLightText", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(hudPanel.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(40f, 40f);
        rt.sizeDelta = new Vector2(250f, 40f);
        Text txt = go.GetComponent<Text>();
        txt.font = GetUIFont();
        txt.fontSize = 24;
        txt.color = new Color(0.831f, 0.686f, 0.216f);
        txt.alignment = TextAnchor.MiddleLeft;
        holyLightText = txt;
    }

    void UpdateHolyLight(float value)
    {
        if (holyLightText != null) holyLightText.text = "圣光: " + value.ToString("0") + "%";
    }

    void EnsureExposureLayerDots()
    {
        if (exposureLayerDots != null && exposureLayerDots.Length > 0) return;

        var silhouette = SilhouetteDirector.Instance?.enemySilhouette;
        if (silhouette == null) return;

        Transform parent = silhouette.transform;
        GameObject container = new GameObject("ExposureLayerDots", typeof(RectTransform));
        container.transform.SetParent(parent, false);

        RectTransform crt = container.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = new Vector2(0f, 420f);
        crt.sizeDelta = new Vector2(120f, 40f);

        if (dotSprite == null) dotSprite = CreateCircleSprite(10);

        exposureLayerDots = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject dot = new GameObject("Dot" + i, typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(container.transform, false);
            RectTransform rt = dot.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2((i - 1) * 30f, 0f);
            rt.sizeDelta = new Vector2(20f, 20f);

            Image img = dot.GetComponent<Image>();
            img.sprite = dotSprite;
            img.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            exposureLayerDots[i] = img;
        }
    }

    Sprite CreateCircleSprite(int radius)
    {
        int size = radius * 2;
        Texture2D tex = new Texture2D(size, size);
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dx = x - radius + 0.5f;
                float dy = y - radius + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    public void UpdateExposureLayer(int value)
    {
        if (exposureLayerDots == null) EnsureExposureLayerDots();
        if (exposureLayerDots == null) return;

        for (int i = 0; i < exposureLayerDots.Length; i++)
        {
            if (exposureLayerDots[i] == null) continue;
            exposureLayerDots[i].color = i < value
                ? new Color(0.58f, 0f, 0.827f, 1f)
                : new Color(0.1f, 0.1f, 0.1f, 1f);
        }
    }

    void EnsureGuessPanel()
    {
        if (guessPanel != null)
        {
            if (guessPanel.transform.Find("InputBox") != null)
            {
                if (guessInput != null) guessInput.interactable = true;
                return;
            }
            Destroy(guessPanel);
            guessPanel = null;
            guessInput = null;
            guessConfirmButton = null;
        }
        if (hudPanel == null) return;

        GameObject panel = new GameObject("GuessPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(hudPanel.transform, false);
        panel.transform.SetAsLastSibling();
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(500f, 240f);
        Image img = panel.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.85f);
        img.raycastTarget = true;

        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleGO.transform.SetParent(panel.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 0.9f);
        titleRT.anchorMax = new Vector2(0.5f, 0.9f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = Vector2.zero;
        titleRT.sizeDelta = new Vector2(400f, 40f);
        Text title = titleGO.GetComponent<Text>();
        title.text = "输入敌方真名";
        title.font = GetUIFont();
        title.fontSize = 24;
        title.color = Color.white;
        title.alignment = TextAnchor.MiddleCenter;

        GameObject inputBox = new GameObject("InputBox", typeof(RectTransform), typeof(Image), typeof(InputField));
        inputBox.transform.SetParent(panel.transform, false);
        RectTransform inputBoxRT = inputBox.GetComponent<RectTransform>();
        inputBoxRT.anchorMin = new Vector2(0.5f, 0.55f);
        inputBoxRT.anchorMax = new Vector2(0.5f, 0.55f);
        inputBoxRT.pivot = new Vector2(0.5f, 0.5f);
        inputBoxRT.anchoredPosition = Vector2.zero;
        inputBoxRT.sizeDelta = new Vector2(400f, 60f);
        Image inputBoxImg = inputBox.GetComponent<Image>();
        inputBoxImg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        inputBoxImg.raycastTarget = true;

        InputField field = inputBox.GetComponent<InputField>();
        field.targetGraphic = inputBoxImg;

        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(inputBox.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(10f, 0f);
        textRT.offsetMax = new Vector2(-10f, 0f);
        Text fieldText = textGO.GetComponent<Text>();
        fieldText.font = GetUIFont();
        fieldText.fontSize = 32;
        fieldText.color = Color.white;
        fieldText.alignment = TextAnchor.MiddleCenter;
        field.textComponent = fieldText;
        field.contentType = InputField.ContentType.Alphanumeric;

        GameObject placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
        placeholderGO.transform.SetParent(inputBox.transform, false);
        RectTransform placeholderRT = placeholderGO.GetComponent<RectTransform>();
        placeholderRT.anchorMin = Vector2.zero;
        placeholderRT.anchorMax = Vector2.one;
        placeholderRT.offsetMin = new Vector2(10f, 0f);
        placeholderRT.offsetMax = new Vector2(-10f, 0f);
        Text placeholderText = placeholderGO.GetComponent<Text>();
        placeholderText.text = "输入真名";
        placeholderText.font = GetUIFont();
        placeholderText.fontSize = 32;
        placeholderText.color = new Color(1f, 1f, 1f, 0.4f);
        placeholderText.alignment = TextAnchor.MiddleCenter;
        field.placeholder = placeholderText;
        textGO.transform.SetAsLastSibling();

        guessCharacterLimit = TrueNameSystem.Instance != null && !string.IsNullOrEmpty(TrueNameSystem.Instance.EnemyTrueName)
            ? TrueNameSystem.Instance.EnemyTrueName.Length
            : 6;
        field.interactable = true;
        guessInput = field;

        GameObject btn = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btn.transform.SetParent(panel.transform, false);
        RectTransform btnRT = btn.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0.3f);
        btnRT.anchorMax = new Vector2(0.5f, 0.3f);
        btnRT.pivot = new Vector2(0.5f, 0.5f);
        btnRT.anchoredPosition = Vector2.zero;
        btnRT.sizeDelta = new Vector2(180f, 50f);
        Image btnImage = btn.GetComponent<Image>();
        btnImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        btnImage.raycastTarget = true;

        GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        btnTextGO.transform.SetParent(btn.transform, false);
        RectTransform btnTextRT = btnTextGO.GetComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.offsetMin = Vector2.zero;
        btnTextRT.offsetMax = Vector2.zero;
        Text btnText = btnTextGO.GetComponent<Text>();
        btnText.text = "确认";
        btnText.font = GetUIFont();
        btnText.fontSize = 24;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;

        Button button = btn.GetComponent<Button>();
        button.targetGraphic = btnImage;
        button.onClick.AddListener(OnGuessConfirm);
        guessConfirmButton = button;

        guessPanel = panel;
        guessPanel.SetActive(false);
    }

    void ShowGuessPanel()
    {
        GameState state = GameManager.Instance != null ? GameManager.Instance.State : GameState.Intro;
        if (state != GameState.Combat) return;
        if (PhaseManager.Instance != null && PhaseManager.Instance.IsTransitioning) return;

        var tns = TrueNameSystem.Instance ?? FindObjectOfType<TrueNameSystem>();
        bool autoAdvance = (GameManager.Instance?.config?.autoAdvanceOnAllRevealed ?? true) && tns != null && tns.IsAllRevealed;
        if (autoAdvance)
        {
            if (guessInput != null) guessInput.text = tns.EnemyTrueName;
            OnGuessConfirm();
            return;
        }

        EnsureGuessPanel();
        if (guessPanel == null) return;
        if (guessPanel.activeSelf)
        {
            OnGuessConfirm();
            return;
        }
        guessPanel.SetActive(true);
        if (guessInput != null)
        {
            guessInput.text = "";
            guessInput.ActivateInputField();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(guessInput.gameObject);
            else
                Debug.LogWarning("[UIManager] EventSystem is missing. Guess panel input may not work.");
        }
        Time.timeScale = GameManager.Instance?.config?.guessTimeScale ?? 0.2f;
    }

    void OnGuessConfirm()
    {
        var tns = TrueNameSystem.Instance ?? FindObjectOfType<TrueNameSystem>();
        if (tns != null && guessInput != null)
        {
            bool correct = tns.GuessTrueName(guessInput.text);
            if (!correct)
                DialogueController.Instance?.Show("不是这个真名。暴露度 +25%。", 2.5f);
        }
        if (guessInput != null) guessInput.text = "";
        if (guessPanel != null) guessPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void UpdateExposure(float value)
    {
        if (exposureText != null) exposureText.text = "暴露度: " + value.ToString("0") + "%";
    }

    void OnExposureDeltaChanged(float value)
    {
        float delta = value - lastExposure;
        if (Mathf.Abs(delta) < 0.01f) return;
        string text = (delta > 0 ? "+" : "") + delta.ToString("0") + "%";
        Color color = delta > 0 ? new Color(1f, 0.25f, 0.25f) : new Color(0.25f, 1f, 0.4f);
        Vector2 pos = Vector2.zero;
        var playerSilhouette = SilhouetteDirector.Instance?.playerSilhouette;
        if (playerSilhouette != null)
        {
            var rt = playerSilhouette.GetComponent<RectTransform>();
            if (rt != null) pos = rt.anchoredPosition + new Vector2(0f, 80f);
        }
        ShowFloatingText(text, pos, color);
        CinematicOverlay.Instance?.SetVignetteExposure(value / 100f, 0.3f);
        lastExposure = value;
    }

    public void ShowFloatingTextAtWord(string text, RectTransform wordRect, Color color, float yOffset = 60f)
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null || wordRect == null) return;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        Vector2 refRes = scaler != null ? scaler.referenceResolution : new Vector2(1920, 1080);
        Vector2 centerOffset = new Vector2(refRes.x * 0.5f, refRes.y * 0.5f);
        Vector2 pos = wordRect.anchoredPosition - centerOffset + new Vector2(0, yOffset);
        ShowFloatingText(text, pos, color);
    }

    public void ShowFloatingText(string text, Vector2 anchoredPosition, Color color)
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        GameObject go = new GameObject("FloatingText", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = new Vector2(200f, 50f);
        Text txt = go.GetComponent<Text>();
        txt.font = GetUIFont();
        txt.fontSize = 28;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = text;
        StartCoroutine(AnimateFloatingText(go, rt));
    }

    IEnumerator AnimateFloatingText(GameObject go, RectTransform rt)
    {
        float duration = 1f;
        float elapsed = 0f;
        Vector2 startPos = rt.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0f, 60f);
        Text txt = go.GetComponent<Text>();
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            if (txt != null)
            {
                Color c = txt.color;
                c.a = 1f - t;
                txt.color = c;
            }
            yield return null;
        }
        Destroy(go);
    }

    void EnsureDomainText()
    {
        if (domainText != null) return;
        if (hudPanel == null) return;
        Transform t = hudPanel.transform.Find("DomainText");
        if (t != null)
        {
            domainText = t.GetComponent<Text>();
            return;
        }
        GameObject go = new GameObject("DomainText", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(hudPanel.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-40f, 40f);
        rt.sizeDelta = new Vector2(250f, 40f);
        Text txt = go.GetComponent<Text>();
        txt.font = GetUIFont();
        txt.fontSize = 24;
        txt.color = new Color(0.58f, 0f, 0.827f, 1f);
        txt.alignment = TextAnchor.MiddleRight;
        domainText = txt;
    }

    public void UpdateDomain(float value)
    {
        if (domainText != null) domainText.text = "领域: " + value.ToString("0") + "%";
    }

    void EnsureWinRetryButton()
    {
        if (winRetryButton != null) return;
        if (winPanel == null) return;
        Transform t = winPanel.transform.Find("RetryButton");
        if (t != null)
        {
            winRetryButton = t.GetComponent<Button>();
            return;
        }
        winRetryButton = CreateRetryButton(winPanel.transform, "再试一次");
    }

    void EnsureLoseRetryButton()
    {
        if (loseRetryButton != null) return;
        if (losePanel == null) return;
        Transform t = losePanel.transform.Find("RetryButton");
        if (t != null)
        {
            loseRetryButton = t.GetComponent<Button>();
            return;
        }
        loseRetryButton = CreateRetryButton(losePanel.transform, "再试一次");
    }

    Button CreateRetryButton(Transform parent, string buttonText)
    {
        GameObject btn = new GameObject("RetryButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btn.transform.SetParent(parent, false);
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.15f);
        rt.anchorMax = new Vector2(0.5f, 0.15f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(200f, 60f);
        Image img = btn.GetComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        img.raycastTarget = true;
        Button button = btn.GetComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(() => GameManager.Instance?.RestartGame());

        GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtGO.transform.SetParent(btn.transform, false);
        RectTransform txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;
        Text txt = txtGO.GetComponent<Text>();
        txt.text = buttonText;
        txt.font = GetUIFont();
        txt.fontSize = 24;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;

        return button;
    }

    void EnsureWinMonologueText()
    {
        if (winPanel == null) return;
        if (winPanel.transform.Find("WinMonologue") != null) return;
        GameObject go = new GameObject("WinMonologue", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(winPanel.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.35f);
        rt.anchorMax = new Vector2(0.5f, 0.35f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(800f, 120f);
        Text txt = go.GetComponent<Text>();
        txt.text = "你的灵魂属于深渊，我知道。但在你漫长的虚无里……有一个角落是属于我的。作为那个剥开你的人。";
        txt.font = GetUIFont();
        txt.fontSize = 24;
        txt.color = new Color(0.831f, 0.686f, 0.216f);
        txt.alignment = TextAnchor.MiddleCenter;
    }

    void EnsureLoseMirrorText()
    {
        if (losePanel == null) return;
        if (losePanel.transform.Find("LoseMirrorText") != null) return;
        GameObject go = new GameObject("LoseMirrorText", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(losePanel.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.35f);
        rt.anchorMax = new Vector2(0.5f, 0.35f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(800f, 120f);
        Text txt = go.GetComponent<Text>();
        string name = GameManager.Instance?.playerTrueName ?? "无名者";
        txt.text = name + "……\n原来这就是你的真名。太轻了。太轻了，不值得记住。";
        txt.font = GetUIFont();
        txt.fontSize = 24;
        txt.color = new Color(0.58f, 0f, 0.827f);
        txt.alignment = TextAnchor.MiddleCenter;
    }

    IEnumerator WinSequence()
    {
        Time.timeScale = 1f;
        string name = GameManager.Instance?.playerTrueName ?? "无名者";
        AudioManager.Instance?.PlayDomain();
        if (winNameText != null)
            winNameText.text = "你的真名：" + name;
        ParticleManager.Instance?.PlayWin();
        EnsureWinRetryButton();
        EnsureWinMonologueText();
        DialogueController.Instance?.Show("你的灵魂属于深渊，我知道。但在你漫长的虚无里……有一个角落是属于我的。作为那个剥开你的人。", 2.5f);
        winPanel.SetActive(true);
        yield break;
    }

    IEnumerator LoseSequence()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        string name = GameManager.Instance?.playerTrueName ?? "无名者";
        AudioManager.Instance?.PlayShatter();

        TriggerLoseEffects();
        ParticleManager.Instance?.PlayLose();

        Time.timeScale = 0f;
        WordSpawner.Instance?.ClearAllWords();
        DialogueController.Instance?.Hide();

        Image crackOverlay = null;
        if (canvas != null)
            crackOverlay = CreateCrackOverlay(canvas);

        yield return new WaitForSecondsRealtime(0.3f);

        if (crackOverlay != null)
            yield return FadeImageAlpha(crackOverlay, 0f, 1f, 0.5f);

        RectTransform nameContainer = null;
        if (canvas != null)
        {
            nameContainer = CreateTrueNameContainer(canvas);
            StartCoroutine(TypeTrueName(nameContainer, name));
        }

        float nameTypeDuration = name.Length * 0.15f;
        yield return new WaitForSecondsRealtime(nameTypeDuration);

        DialogueController.Instance?.Show(name + "……\n原来这就是你的真名。太轻了。太轻了，不值得记住。", 2.5f);

        float elapsedSoFar = 0.8f + nameTypeDuration;
        float remainingToDim = 3.0f - elapsedSoFar;
        if (remainingToDim > 0f)
            yield return new WaitForSecondsRealtime(remainingToDim);

        if (SilhouetteDirector.Instance?.playerSilhouette != null)
        {
            SilhouetteDirector.Instance.playerSilhouette.SetExposure(1f);
            SilhouetteDirector.Instance.playerSilhouette.TriggerHurt(2f);
        }

        yield return new WaitForSecondsRealtime(1.0f);

        if (crackOverlay != null)
            yield return FadeImageAlpha(crackOverlay, 1f, 0f, 0.5f);

        if (crackOverlay != null)
            Destroy(crackOverlay.gameObject);
        if (nameContainer != null)
            Destroy(nameContainer.gameObject);

        Time.timeScale = 1f;
        if (loseNameText != null)
            loseNameText.text = "被剥露的真名：" + name;
        EnsureLoseRetryButton();
        EnsureLoseMirrorText();
        losePanel.SetActive(true);
    }

    Image CreateCrackOverlay(Canvas canvas)
    {
        GameObject go = new GameObject("LoseCrackOverlay", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = go.GetComponent<Image>();
        img.sprite = TextureGenerator.CreateCrackSprite(1024, Color.white);
        img.color = new Color(1f, 1f, 1f, 0f);
        img.raycastTarget = false;
        return img;
    }

    RectTransform CreateTrueNameContainer(Canvas canvas)
    {
        GameObject go = new GameObject("LoseTrueNameText", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(800f, 120f);
        Text txt = go.GetComponent<Text>();
        txt.font = GetUIFont();
        txt.fontSize = 52;
        txt.color = new Color(0.85f, 0.9f, 0.95f, 1f);
        txt.alignment = TextAnchor.MiddleCenter;
        return rt;
    }

    IEnumerator TypeTrueName(RectTransform container, string name)
    {
        Text txt = container.GetComponent<Text>();
        if (txt == null) yield break;
        txt.text = "";
        for (int i = 0; i < name.Length; i++)
        {
            txt.text += name[i];
            yield return new WaitForSecondsRealtime(0.15f);
        }
    }

    IEnumerator FadeImageAlpha(Image img, float from, float to, float duration)
    {
        if (img == null) yield break;
        Color c = img.color;
        c.a = from;
        img.color = c;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            c.a = Mathf.Lerp(from, to, t);
            img.color = c;
            yield return null;
        }
        c.a = to;
        img.color = c;
    }
}
