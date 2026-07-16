using UnityEngine;
using UnityEngine.UI;

public class WordHitEffect : MonoBehaviour
{
    public static WordHitEffect Instance { get; private set; }

    public RectTransform canvasTransform;
    public Sprite[] hitSuccessFrames;
    public Sprite[] hitMissFrames;
    public float effectSize = 120f;
    public float fps = 12f;

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
        if (canvasTransform == null)
            canvasTransform = FindObjectOfType<Canvas>()?.GetComponent<RectTransform>();

        if (hitSuccessFrames == null || hitSuccessFrames.Length == 0)
        {
            hitSuccessFrames = Resources.LoadAll<Sprite>("VFX/HitSuccess");
            if (hitSuccessFrames != null) System.Array.Sort(hitSuccessFrames, (a, b) => a.name.CompareTo(b.name));
        }
        if (hitMissFrames == null || hitMissFrames.Length == 0)
        {
            hitMissFrames = Resources.LoadAll<Sprite>("VFX/HitMiss");
            if (hitMissFrames != null) System.Array.Sort(hitMissFrames, (a, b) => a.name.CompareTo(b.name));
        }
    }

    public void Play(Vector2 anchoredPosition, LanguageType lang, bool hit)
    {
        CameraShake.Instance?.Shake(hit ? 0.05f : 0.12f, hit ? 0.08f : 0.2f);

        Sprite[] frames = hit ? hitSuccessFrames : hitMissFrames;
        if (frames == null || frames.Length == 0) return;

        GameObject go = new GameObject("HitSpriteAnim", typeof(RectTransform), typeof(Image));
        if (canvasTransform != null)
        {
            go.transform.SetParent(canvasTransform, false);
            go.transform.SetAsLastSibling();
        }

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = new Vector2(effectSize, effectSize);

        Image img = go.GetComponent<Image>();
        img.color = GetTint(lang, hit);
        img.raycastTarget = false;

        SpriteImageAnimator anim = go.AddComponent<SpriteImageAnimator>();
        anim.image = img;
        anim.frames = frames;
        anim.fps = fps;
        anim.loop = false;
        anim.Restart();

        Destroy(go, frames.Length / Mathf.Max(fps, 0.001f));
    }

    Color GetTint(LanguageType lang, bool hit)
    {
        if (!hit) return new Color(1f, 0.35f, 0.35f, 1f);
        if (lang == LanguageType.Sacred) return new Color(1f, 0.92f, 0.6f, 1f);
        if (lang == LanguageType.Demonic) return new Color(0.85f, 0.5f, 1f, 1f);
        if (lang == LanguageType.Interference) return new Color(0.55f, 0.55f, 0.55f, 1f);
        return Color.white;
    }
}
