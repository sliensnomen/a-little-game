using UnityEngine;
using UnityEngine.UI;

public class DefenseLine : MonoBehaviour
{
    public Image core;
    public Image glow;

    public Color lineColor = new Color(1f, 0.95f, 0.7f, 0.9f);
    public Color glowColor = new Color(1f, 0.9f, 0.5f, 0.18f);

    public float lineWidth = 3f;
    public float glowWidth = 100f;
    public float pulseSpeed = 2f;
    public float pulseMin = 0.1f;
    public float pulseMax = 0.28f;

    private RectTransform rectTransform;

    void Start()
    {
        EnsureVisuals();
        if (WordSpawner.Instance != null)
            SetPosition(WordSpawner.Instance.defenseLineX);
    }

    void EnsureVisuals()
    {
        if (core != null) return;

        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) rectTransform = gameObject.AddComponent<RectTransform>();
        core = GetComponent<Image>();
        if (core == null) core = gameObject.AddComponent<Image>();

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null && transform.parent != canvas.transform)
            transform.SetParent(canvas.transform, false);

        rectTransform.anchorMin = new Vector2(0, 0.5f);
        rectTransform.anchorMax = new Vector2(0, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(lineWidth, 1200f);

        core.color = lineColor;
        core.raycastTarget = false;

        int siblingIndex = 0;
        GameObject player = GameObject.Find("WitchKingPlaceholder");
        GameObject enemy = GameObject.Find("MirrorPlaceholder");
        GameObject wordContainer = GameObject.Find("WordContainer");
        GameObject bg = GameObject.Find("Background");

        if (player != null || enemy != null)
        {
            int playerIndex = player != null ? player.transform.GetSiblingIndex() : -1;
            int enemyIndex = enemy != null ? enemy.transform.GetSiblingIndex() : -1;
            siblingIndex = Mathf.Max(playerIndex, enemyIndex) + 1;
        }
        else if (wordContainer != null)
        {
            siblingIndex = wordContainer.transform.GetSiblingIndex();
        }
        else if (bg != null)
        {
            siblingIndex = bg.transform.GetSiblingIndex() + 1;
        }
        transform.SetSiblingIndex(siblingIndex);

        GameObject glowGO = transform.Find("Glow")?.gameObject;
        if (glowGO == null)
        {
            glowGO = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glowGO.transform.SetParent(transform, false);
        }
        RectTransform glowRT = glowGO.GetComponent<RectTransform>();
        glowRT.anchorMin = new Vector2(0.5f, 0.5f);
        glowRT.anchorMax = new Vector2(0.5f, 0.5f);
        glowRT.pivot = new Vector2(0.5f, 0.5f);
        glowRT.anchoredPosition = Vector2.zero;
        glowRT.sizeDelta = new Vector2(glowWidth, 1200f);

        glow = glowGO.GetComponent<Image>();
        glow.color = glowColor;
        glow.raycastTarget = false;
    }

    public void SetPosition(float x)
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null) rectTransform.anchoredPosition = new Vector2(x, 0);
    }

    void Update()
    {
        if (glow != null)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float a = Mathf.Lerp(pulseMin, pulseMax, t);
            Color c = glowColor;
            c.a = a;
            glow.color = c;
        }
    }
}
