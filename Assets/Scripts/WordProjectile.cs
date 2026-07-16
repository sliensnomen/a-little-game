using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum WordState { Fly, Hold, Miss, Hit }
public enum WordPattern { A, B, C }
public enum WordType { Normal, Interference, Dual }

public class WordProjectile : MonoBehaviour
{
    public RectTransform rectTransform;
    public Text label;
    public Image background;

    public LanguageType language;
    public WordPattern pattern;
    public WordType wordType;
    public float speed;
    public float defenseLineX = 640f;
    public float defenseLineHitRange = 50f;
    public float holdDuration = 0.7f;
    public float sineAmplitude = 10f;
    public float sineFrequency = 2f;

    public WordState State { get; set; } = WordState.Fly;
    public float holdElapsed { get; set; }

    private float baseY;
    private float oscillationTime;
    private Text promptText;

    public void Init(LanguageType lang, WordPattern pat, WordType type, string word, float sp, Vector2 startPos)
    {
        language = lang;
        pattern = pat;
        wordType = type;
        speed = sp;
        label.text = word;
        SetVisual(type, lang);
        State = WordState.Fly;
        holdElapsed = 0f;
        baseY = startPos.y;
        oscillationTime = 0f;
        rectTransform.anchoredPosition = startPos;
        gameObject.SetActive(true);
        ClearPrompt();
    }

    void SetVisual(WordType type, LanguageType lang)
    {
        if (label != null)
        {
            Font f = FontManager.Instance?.GetFont(lang);
            if (f != null) label.font = f;
        }

        if (type == WordType.Interference)
        {
            label.color = new Color(0.5f, 0.5f, 0.5f);
            background.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
            return;
        }

        if (lang == LanguageType.Sacred)
        {
            label.color = new Color(0.831f, 0.686f, 0.216f);
            background.color = new Color(0.831f, 0.686f, 0.216f, 0.2f);
        }
        else if (lang == LanguageType.Demonic)
        {
            label.color = new Color(0.58f, 0f, 0.827f);
            background.color = new Color(0.58f, 0f, 0.827f, 0.2f);
        }
        else
        {
            label.color = Color.white;
            background.color = new Color(0.831f, 0.686f, 0.216f, 0.5f);
        }
    }

    void EnsurePrompt()
    {
        if (promptText != null) return;
        GameObject go = new GameObject("Prompt", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 8f);
        rt.sizeDelta = new Vector2(80f, 30f);
        Text txt = go.GetComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 20;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontStyle = FontStyle.Bold;
        promptText = txt;
        go.SetActive(false);
    }

    public void UpdatePrompt()
    {
        EnsurePrompt();
        if (promptText == null) return;

        if (wordType == WordType.Dual)
        {
            promptText.text = "Q+E";
            promptText.color = new Color(0.706f, 0.343f, 0.522f);
            promptText.gameObject.SetActive(true);
        }
        else if (wordType == WordType.Interference)
        {
            promptText.text = "E";
            promptText.color = new Color(0.5f, 0.5f, 0.5f);
            promptText.gameObject.SetActive(true);
        }
        else if (language == LanguageType.Sacred)
        {
            promptText.text = "Q";
            promptText.color = new Color(0.831f, 0.686f, 0.216f);
            promptText.gameObject.SetActive(true);
        }
        else if (language == LanguageType.Demonic)
        {
            promptText.text = "E";
            promptText.color = new Color(0.58f, 0f, 0.827f);
            promptText.gameObject.SetActive(true);
        }
        else
        {
            promptText.gameObject.SetActive(false);
        }
    }

    public void ClearPrompt()
    {
        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (State == WordState.Fly)
        {
            Vector2 pos = rectTransform.anchoredPosition;
            pos.x -= speed * Time.deltaTime;
            oscillationTime += Time.deltaTime;
            pos.y = baseY + Mathf.Sin(oscillationTime * 2f * Mathf.PI * sineFrequency) * sineAmplitude;
            rectTransform.anchoredPosition = pos;
            if (pos.x <= defenseLineX + defenseLineHitRange)
            {
                pos.x = defenseLineX;
                rectTransform.anchoredPosition = pos;
                State = WordState.Hold;
                holdElapsed = 0f;
                WordSpawner.Instance?.RegisterHold(this);
                UpdatePrompt();
                StartCoroutine(HoldRoutine());
            }
        }
        else if (State == WordState.Hold)
        {
            holdElapsed += Time.deltaTime;
        }
    }

    IEnumerator HoldRoutine()
    {
        yield return new WaitForSeconds(holdDuration);
        if (State == WordState.Hold)
            ForceMiss();
    }

    public void ForceMiss()
    {
        if (State != WordState.Hold) return;
        StopAllCoroutines();
        State = WordState.Miss;
        WordSpawner.Instance?.HandleMiss(this);
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }
}
