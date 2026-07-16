using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DialogueController : MonoBehaviour
{
    public static DialogueController Instance { get; private set; }

    public GameObject dialoguePanel;
    public Text dialogueText;
    public float typeSpeed = 0.04f;

    private Coroutine routine;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        FontManager.EnsureExists();
        EnsureUI();
    }

    void EnsureUI()
    {
        if (dialoguePanel != null) return;
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        GameObject panel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        panel.transform.SetAsLastSibling();

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.08f);
        rt.anchorMax = new Vector2(0.5f, 0.08f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(900, 110);

        Image img = panel.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0.7f);
        img.raycastTarget = false;

        GameObject textGO = new GameObject("DialogueText", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(panel.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(20, 15);
        textRT.offsetMax = new Vector2(-20, -15);

        Text t = textGO.GetComponent<Text>();
        t.font = GetUIFont();
        t.fontSize = 28;
        t.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        t.alignment = TextAnchor.MiddleCenter;

        dialoguePanel = panel;
        dialogueText = t;
        dialoguePanel.SetActive(false);
    }

    Font GetUIFont()
    {
        var fm = FontManager.Instance;
        if (fm != null && fm.GetUIFont() != null) return fm.GetUIFont();
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    public void Show(string text, float autoHideDelay = 2.5f)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (dialoguePanel == null || dialogueText == null) EnsureUI();
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(TypeRoutine(text, autoHideDelay));
    }

    public void Hide()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    IEnumerator TypeRoutine(string text, float autoHideDelay)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        dialogueText.text = "";
        int len = 0;
        float timer = 0f;
        float typewriterTimer = 0f;
        float typewriterCooldown = 0.08f;
        while (len < text.Length)
        {
            timer += Time.unscaledDeltaTime;
            typewriterTimer += Time.unscaledDeltaTime;
            if (timer >= typeSpeed)
            {
                timer = 0f;
                len++;
                dialogueText.text = text.Substring(0, len);
                if (typewriterTimer >= typewriterCooldown)
                {
                    typewriterTimer = 0f;
                    AudioManager.Instance?.PlayTypewriter();
                }
            }
            yield return null;
        }
        yield return new WaitForSecondsRealtime(autoHideDelay);
        Hide();
    }
}
