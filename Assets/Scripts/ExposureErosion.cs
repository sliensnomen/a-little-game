using UnityEngine;
using UnityEngine.UI;

public class ExposureErosion : MonoBehaviour
{
    public static ExposureErosion Instance { get; private set; }

    public Image vignette;
    public float maxAlpha = 0.6f;
    public float pulseSpeed = 2f;

    private float targetAlpha;
    private float currentExposure;

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
        EnsureVignette();
        if (TrueNameSystem.Instance != null)
        {
            TrueNameSystem.Instance.OnExposureChanged.AddListener(UpdateExposure);
            UpdateExposure(TrueNameSystem.Instance.PlayerExposure);
        }
    }

    void EnsureVignette()
    {
        if (vignette != null) return;
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        GameObject go = new GameObject("ExposureVignette", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.GetComponent<Image>();
        Sprite radial = TextureGenerator.CreateRadialGradientSprite(256, new Color(1, 0, 0, 0), new Color(1, 0, 0, maxAlpha));
        img.sprite = radial;
        img.type = Image.Type.Simple;
        img.color = Color.clear;
        img.raycastTarget = false;
        vignette = img;
    }

    void UpdateExposure(float exposure)
    {
        currentExposure = exposure;
        float max = 100f;
        if (TrueNameSystem.Instance != null && TrueNameSystem.Instance.gameManager != null)
            max = TrueNameSystem.Instance.gameManager.config.playerExposureMax;
        targetAlpha = Mathf.Clamp01(exposure / max) * maxAlpha;
    }

    void Update()
    {
        if (vignette == null) return;
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.05f * targetAlpha;
        float alpha = Mathf.Lerp(vignette.color.a, targetAlpha + pulse, Time.deltaTime * 5f);
        Color c = new Color(0.8f, 0, 0, 1f);
        c.a = Mathf.Clamp01(alpha);
        vignette.color = c;
    }
}
