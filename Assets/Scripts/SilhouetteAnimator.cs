using UnityEngine;
using UnityEngine.UI;

public class SilhouetteAnimator : MonoBehaviour
{
    public RectTransform target;
    public Image silhouetteImage;
    public Image eyeGlow;
    public Image crackOverlay;

    public Color normalColor = Color.black;
    public Color hitColor = Color.white;
    public Color hurtColor = new Color(0.6f, 0f, 0f, 1f);
    public Color domainColor = new Color(0.831f, 0.686f, 0.216f, 1f);

    public float breathSpeed = 1.5f;
    public float breathAmount = 0.03f;
    public bool shakeEnabled = false;
    public float shakeIntensity = 3f;

    private Vector3 baseScale;
    private Vector2 baseAnchoredPosition;
    private float flashTimer;
    private Color flashColor;
    private float domainTimer;
    private float crackAlpha;
    private Color currentEyeGlowColor = new Color(1, 0.2f, 0.2f, 1f);

    void Start()
    {
        if (target == null) target = GetComponent<RectTransform>();
        if (silhouetteImage == null) silhouetteImage = GetComponent<Image>();
        if (target != null)
        {
            baseScale = target.localScale;
            baseAnchoredPosition = target.anchoredPosition;
        }

        EnsureEyeGlow();
        EnsureCrackOverlay();
    }

    void EnsureEyeGlow()
    {
        if (eyeGlow != null) return;
        GameObject go = new GameObject("EyeGlow", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, 120f);
        rt.sizeDelta = new Vector2(60, 60);

        Image img = go.GetComponent<Image>();
        img.sprite = TextureGenerator.CreateCircleSprite(32, new Color(1, 0.2f, 0.2f, 1));
        img.type = Image.Type.Simple;
        Color c = img.color;
        c.a = 0;
        img.color = c;
        eyeGlow = img;
    }

    void EnsureCrackOverlay()
    {
        if (crackOverlay != null) return;
        GameObject go = new GameObject("CrackOverlay", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.GetComponent<Image>();
        img.sprite = TextureGenerator.CreateCrackSprite(256, new Color(0.9f, 0.2f, 0.2f, 1));
        img.type = Image.Type.Simple;
        Color c = Color.white;
        c.a = 0;
        img.color = c;
        crackOverlay = img;
    }

    void Update()
    {
        if (target != null)
        {
            float breath = 1 + Mathf.Sin(Time.time * breathSpeed) * breathAmount;
            target.localScale = baseScale * breath;

            if (shakeEnabled)
            {
                Vector2 offset = Random.insideUnitCircle * shakeIntensity * Time.deltaTime;
                target.anchoredPosition = baseAnchoredPosition + offset;
            }
            else
            {
                target.anchoredPosition = Vector2.Lerp(target.anchoredPosition, baseAnchoredPosition, Time.deltaTime * 10f);
            }
        }

        if (silhouetteImage != null)
        {
            Color c = normalColor;
            if (flashTimer > 0)
            {
                flashTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(flashTimer / 0.15f);
                c = Color.Lerp(normalColor, flashColor, t);
            }
            if (domainTimer > 0)
            {
                domainTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(domainTimer / 0.5f);
                c = Color.Lerp(c, domainColor, t);
            }
            silhouetteImage.color = c;
        }

        if (crackOverlay != null)
        {
            Color c = crackOverlay.color;
            c.a = Mathf.Lerp(c.a, crackAlpha, Time.deltaTime * 3f);
            crackOverlay.color = c;
        }

        if (eyeGlow != null)
        {
            Color c = currentEyeGlowColor;
            float targetAlpha = 0.5f + Mathf.Sin(Time.time * 3f) * 0.2f;
            c.a = Mathf.Lerp(eyeGlow.color.a, targetAlpha, Time.deltaTime * 2f);
            eyeGlow.color = c;
        }
    }

    public void TriggerHit(float intensity = 1f)
    {
        flashColor = hitColor;
        flashTimer = 0.12f;
        if (target != null)
            target.localScale = baseScale * (1 + 0.05f * intensity);
    }

    public void TriggerAttack()
    {
        var anim = GetComponent<SpriteImageAnimator>();
        if (anim != null) anim.PlayAttack();
    }

    public void TriggerTakeHit()
    {
        var anim = GetComponent<SpriteImageAnimator>();
        if (anim != null) anim.PlayHit();
    }

    public void TriggerHurt(float intensity = 1f)
    {
        flashColor = hurtColor;
        flashTimer = 0.2f;
        crackAlpha = Mathf.Min(1f, crackAlpha + 0.15f * intensity);
    }

    public void TriggerDomain()
    {
        domainTimer = 0.8f;
    }

    public void SetExposure(float normalized)
    {
        crackAlpha = normalized * 0.7f;
    }

    public void SetCrackAlpha(float alpha)
    {
        crackAlpha = Mathf.Clamp01(alpha);
    }

    public void SetEyeGlowColor(Color color)
    {
        currentEyeGlowColor = color;
    }
}
