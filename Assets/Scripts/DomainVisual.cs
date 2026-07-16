using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DomainVisual : MonoBehaviour
{
    public static DomainVisual Instance { get; private set; }

    public Canvas canvas;
    public Image shockwaveRing;
    public Image flashOverlay;
    public Sprite[] domainFrames;

    public float ringDuration = 1.5f;
    public float ringMaxSize = 2000f;

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
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (domainFrames == null || domainFrames.Length == 0)
        {
            domainFrames = Resources.LoadAll<Sprite>("VFX/Domain");
            if (domainFrames != null) System.Array.Sort(domainFrames, (a, b) => a.name.CompareTo(b.name));
        }
        EnsureRing();
        EnsureFlash();
    }

    void EnsureRing()
    {
        if (shockwaveRing != null) return;
        GameObject go = new GameObject("DomainShockwave", typeof(RectTransform), typeof(Image));
        if (canvas != null) go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        Image img = go.GetComponent<Image>();
        if (domainFrames != null && domainFrames.Length > 0)
        {
            img.sprite = domainFrames[0];
            img.color = new Color(1f, 1f, 0.85f, 0.8f);
        }
        else
        {
            Sprite ring = TextureGenerator.CreateRingSprite(256, 0.7f, new Color(0.95f, 0.9f, 0.6f, 0.8f));
            img.sprite = ring;
            img.color = Color.clear;
        }
        img.type = Image.Type.Simple;
        img.raycastTarget = false;
        shockwaveRing = img;
        go.SetActive(false);
    }

    void EnsureFlash()
    {
        if (flashOverlay != null) return;
        GameObject go = new GameObject("DomainFlash", typeof(RectTransform), typeof(Image));
        if (canvas != null) go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.GetComponent<Image>();
        img.color = Color.clear;
        img.raycastTarget = false;
        flashOverlay = img;
    }

    public void Play()
    {
        CameraShake.Instance?.Shake(0.18f, 0.3f);
        StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        if (flashOverlay != null)
        {
            flashOverlay.gameObject.SetActive(true);
            flashOverlay.color = new Color(1, 1, 1, 0.8f);
            float t = 0;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                flashOverlay.color = Color.Lerp(new Color(1, 1, 1, 0.8f), Color.clear, t / 0.5f);
                yield return null;
            }
            flashOverlay.color = Color.clear;
            flashOverlay.gameObject.SetActive(false);
        }

        if (shockwaveRing != null)
        {
            shockwaveRing.gameObject.SetActive(true);
            RectTransform rt = shockwaveRing.rectTransform;
            rt.sizeDelta = Vector2.zero;
            float t = 0;
            while (t < ringDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = t / ringDuration;
                float size = Mathf.Lerp(0, ringMaxSize, p);
                rt.sizeDelta = new Vector2(size, size);
                Color c = shockwaveRing.color;
                c.a = Mathf.Lerp(0.8f, 0f, p);
                shockwaveRing.color = c;
                yield return null;
            }
            shockwaveRing.gameObject.SetActive(false);
            shockwaveRing.color = new Color(1f, 1f, 0.85f, 0.8f);
        }
    }
}
