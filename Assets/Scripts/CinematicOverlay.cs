using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CinematicOverlay : MonoBehaviour
{
    public static CinematicOverlay Instance { get; private set; }

    public Canvas canvas;
    public float combatLetterboxRatio = 0.12f;
    public float endLetterboxRatio = 0.18f;

    private Image topBar;
    private Image bottomBar;
    private Image vignetteImage;
    private Color vignetteColor = Color.black;
    private float vignetteAlpha = 0f;
    private float baseVignetteAlpha = 0f;

    private Coroutine letterboxRoutine;
    private Coroutine vignetteRoutine;

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
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        EnsureLetterbox();
        EnsureVignette();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static CinematicOverlay EnsureExists()
    {
        if (Instance != null) return Instance;
        var existing = FindObjectOfType<CinematicOverlay>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }
        GameObject go = new GameObject("CinematicOverlay");
        return go.AddComponent<CinematicOverlay>();
    }

    void EnsureLetterbox()
    {
        if (topBar != null && bottomBar != null) return;
        if (canvas == null) return;

        topBar = CreateBar("LetterboxTop", Vector2.one, Vector2.zero, TextAnchor.UpperCenter);
        bottomBar = CreateBar("LetterboxBottom", Vector2.zero, Vector2.one, TextAnchor.LowerCenter);
    }

    Image CreateBar(string name, Vector2 anchorMin, Vector2 anchorMax, TextAnchor alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, anchorMin.y);
        rt.anchorMax = new Vector2(1f, anchorMax.y);
        rt.pivot = new Vector2(0.5f, anchorMin.y);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 0f);

        Image img = go.GetComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        return img;
    }

    void EnsureVignette()
    {
        if (vignetteImage != null) return;
        if (canvas == null) return;

        GameObject go = new GameObject("VignetteOverlay", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        Image img = go.GetComponent<Image>();
        img.sprite = CreateVignetteSprite();
        img.type = Image.Type.Simple;
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;
        img.preserveAspect = false;
        vignetteImage = img;
    }

    Sprite CreateVignetteSprite()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(center, new Vector2(x, y)) / maxDist;
                float alpha = Mathf.Clamp01((dist - 0.3f) / 0.7f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    public void SetLetterbox(float ratio, float duration = 0.5f)
    {
        EnsureLetterbox();
        if (topBar == null || bottomBar == null) return;
        if (letterboxRoutine != null) StopCoroutine(letterboxRoutine);
        letterboxRoutine = StartCoroutine(AnimateLetterbox(ratio, duration));
    }

    IEnumerator AnimateLetterbox(float targetRatio, float duration)
    {
        float start = topBar.rectTransform.sizeDelta.y / Screen.height;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration > 0f ? elapsed / duration : 1f;
            t = Mathf.SmoothStep(0f, 1f, t);
            float current = Mathf.Lerp(start, targetRatio, t);
            float height = current * Screen.height;
            topBar.rectTransform.sizeDelta = new Vector2(0f, height);
            bottomBar.rectTransform.sizeDelta = new Vector2(0f, height);
            yield return null;
        }
        float finalHeight = targetRatio * Screen.height;
        topBar.rectTransform.sizeDelta = new Vector2(0f, finalHeight);
        bottomBar.rectTransform.sizeDelta = new Vector2(0f, finalHeight);
    }

    public void SetVignette(Color tint, float alpha, float duration = 0.5f)
    {
        EnsureVignette();
        if (vignetteImage == null) return;
        vignetteColor = tint;
        vignetteAlpha = alpha;
        baseVignetteAlpha = alpha;
        if (vignetteRoutine != null) StopCoroutine(vignetteRoutine);
        vignetteRoutine = StartCoroutine(AnimateVignette(tint, alpha, duration));
    }

    public void SetVignetteAlpha(float alpha, float duration = 0.5f)
    {
        SetVignette(vignetteColor, alpha, duration);
    }

    public void SetVignetteExposure(float normalizedExposure, float duration = 0.3f)
    {
        EnsureVignette();
        if (vignetteImage == null) return;
        float targetAlpha = Mathf.Clamp01(baseVignetteAlpha + normalizedExposure * 0.25f);
        if (vignetteRoutine != null) StopCoroutine(vignetteRoutine);
        vignetteRoutine = StartCoroutine(AnimateVignette(vignetteColor, targetAlpha, duration));
    }

    IEnumerator AnimateVignette(Color targetTint, float targetAlpha, float duration)
    {
        Color start = vignetteImage.color;
        Color target = new Color(targetTint.r, targetTint.g, targetTint.b, targetAlpha);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration > 0f ? elapsed / duration : 1f;
            t = Mathf.SmoothStep(0f, 1f, t);
            vignetteImage.color = Color.Lerp(start, target, t);
            yield return null;
        }
        vignetteImage.color = target;
    }
}
