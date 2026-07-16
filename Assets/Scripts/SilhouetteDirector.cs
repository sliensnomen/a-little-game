using UnityEngine;
using UnityEngine.UI;

public class SilhouetteDirector : MonoBehaviour
{
    public static SilhouetteDirector Instance { get; private set; }

    public SilhouetteAnimator playerSilhouette;
    public SilhouetteAnimator enemySilhouette;

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
        EnsureSilhouettes();
        LogPlaceholderStatus();
        if (TrueNameSystem.Instance != null)
        {
            TrueNameSystem.Instance.OnExposureChanged.AddListener(OnExposureChanged);
            TrueNameSystem.Instance.OnLetterRevealed.AddListener(OnLetterRevealed);
        }
        if (DomainManager.Instance != null)
            DomainManager.Instance.OnDomainExpanded.AddListener(OnDomainExpanded);
    }

    void LogPlaceholderStatus()
    {
        LogSingleStatus("主角", playerSilhouette);
        LogSingleStatus("敌人", enemySilhouette);
    }

    void LogSingleStatus(string label, SilhouetteAnimator sa)
    {
        if (sa == null)
        {
            Debug.LogWarning($"SilhouetteDirector: {label} 占位符缺失。");
            return;
        }
        Image img = sa.GetComponent<Image>();
        if (img == null)
        {
            Debug.LogWarning($"SilhouetteDirector: {label} 的 GameObject 上没有 Image 组件。");
            return;
        }
        if (img.sprite == null)
            Debug.LogWarning($"SilhouetteDirector: {label} 的 Image 上没有 Sprite。");
        else
            Debug.Log($"SilhouetteDirector: {label} 已设置 Sprite = {img.sprite.name}");
    }

    void EnsureSilhouettes()
    {
        if (playerSilhouette == null)
        {
            GameObject go = GameObject.Find("WitchKingPlaceholder");
            if (go != null)
            {
                playerSilhouette = go.GetComponent<SilhouetteAnimator>();
                if (playerSilhouette == null) playerSilhouette = go.AddComponent<SilhouetteAnimator>();
                InitSilhouette(playerSilhouette, go);
            }
        }
        if (enemySilhouette == null)
        {
            GameObject go = GameObject.Find("MirrorPlaceholder");
            if (go != null)
            {
                enemySilhouette = go.GetComponent<SilhouetteAnimator>();
                if (enemySilhouette == null) enemySilhouette = go.AddComponent<SilhouetteAnimator>();
                InitSilhouette(enemySilhouette, go);
            }
        }
    }

    void InitSilhouette(SilhouetteAnimator sa, GameObject go)
    {
        if (sa == null) return;
        sa.target = go.GetComponent<RectTransform>();
        sa.silhouetteImage = go.GetComponent<Image>();
        sa.normalColor = Color.white;
    }

    void OnLetterRevealed(int index)
    {
        if (enemySilhouette != null)
        {
            enemySilhouette.TriggerHit(0.2f);
            if (index >= 0)
                enemySilhouette.TriggerTakeHit();
        }
    }

    void OnExposureChanged(float exposure)
    {
        if (playerSilhouette != null) playerSilhouette.SetExposure(exposure / 100f);
    }

    void OnDomainExpanded()
    {
        if (playerSilhouette != null) playerSilhouette.TriggerDomain();
        if (enemySilhouette != null) enemySilhouette.TriggerDomain();
    }

    public void SetPhaseVisuals(int phaseIndex)
    {
        if (enemySilhouette != null)
        {
            enemySilhouette.shakeEnabled = phaseIndex >= 2;
            enemySilhouette.breathSpeed = phaseIndex >= 1 ? 2.5f : 1.5f;
            enemySilhouette.SetCrackAlpha(phaseIndex >= 2 ? 0.7f : (phaseIndex >= 1 ? 0.3f : 0f));

            if (phaseIndex <= 0)
                enemySilhouette.SetEyeGlowColor(new Color(1, 0.2f, 0.2f, 1f));
            else if (phaseIndex == 1)
                enemySilhouette.SetEyeGlowColor(new Color(0.58f, 0f, 0.83f, 1f));
            else
                enemySilhouette.SetEyeGlowColor(new Color(1f, 0f, 0.25f, 1f));
        }

        if (playerSilhouette != null)
        {
            Color golden = new Color(1f, 0.8f, 0.2f, 1f);
            if (phaseIndex >= 2)
                golden.a = 0.3f;
            else if (phaseIndex >= 1)
                golden.a = 0.5f;
            playerSilhouette.SetEyeGlowColor(golden);
            playerSilhouette.shakeEnabled = false;
        }
    }
}
