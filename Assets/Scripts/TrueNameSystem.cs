using UnityEngine;
using UnityEngine.Events;

public class TrueNameSystem : MonoBehaviour
{
    public static TrueNameSystem Instance { get; private set; }

    public GameManager gameManager;

    public string EnemyTrueName { get; private set; }
    public int RevealedMask { get; private set; }
    public bool IsAllRevealed => RevealedMask == ((1 << EnemyTrueName.Length) - 1);

    public float PlayerExposure { get; private set; }
    public float DomainCharge { get; private set; }
    public int ExposureLayer { get; private set; }

    public UnityEvent<int> OnLetterRevealed = new UnityEvent<int>();
    public UnityEvent OnAllRevealed = new UnityEvent();
    public UnityEvent OnTemporaryReveal = new UnityEvent();
    public UnityEvent OnTemporaryHide = new UnityEvent();
    public UnityEvent<float> OnExposureChanged = new UnityEvent<float>();
    public UnityEvent<float> OnDomainChargeChanged = new UnityEvent<float>();
    public UnityEvent<int> OnExposureLayerChanged = new UnityEvent<int>();
    public UnityEvent OnWin = new UnityEvent();
    public UnityEvent OnPhaseGuessCorrect = new UnityEvent();
    public UnityEvent OnLose = new UnityEvent();

    private bool temporaryRevealActive;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null) ResetValues();
    }

    void Start()
    {
        ResetValues();
    }

    public void ResetValues()
    {
        int maxLen = gameManager != null && gameManager.config != null ? gameManager.config.maxTrueNameLength : 6;
        EnemyTrueName = gameManager != null && !string.IsNullOrEmpty(gameManager.enemyTrueName)
            ? gameManager.enemyTrueName.ToUpperInvariant()
            : "MIRROR";
        if (EnemyTrueName.Length > maxLen)
            EnemyTrueName = EnemyTrueName.Substring(0, maxLen);
        RevealedMask = 0;
        PlayerExposure = 0f;
        DomainCharge = 0f;
        ExposureLayer = 0;
        OnLetterRevealed?.Invoke(-1);
        OnExposureChanged?.Invoke(PlayerExposure);
        OnDomainChargeChanged?.Invoke(DomainCharge);
        OnExposureLayerChanged?.Invoke(ExposureLayer);
    }

    public void SetEnemyTrueName(string name)
    {
        int maxLen = gameManager != null && gameManager.config != null ? gameManager.config.maxTrueNameLength : 6;
        if (string.IsNullOrEmpty(name))
            name = "MIRROR";
        EnemyTrueName = name.Trim().ToUpperInvariant();
        if (EnemyTrueName.Length > maxLen)
            EnemyTrueName = EnemyTrueName.Substring(0, maxLen);
        RevealedMask = 0;
        temporaryRevealActive = false;
        OnLetterRevealed?.Invoke(-1);
    }

    public void RevealLetter(float charge = -1f)
    {
        if (IsAllRevealed) return;
        System.Collections.Generic.List<int> hidden = new System.Collections.Generic.List<int>();
        for (int i = 0; i < EnemyTrueName.Length; i++)
            if ((RevealedMask & (1 << i)) == 0) hidden.Add(i);
        if (hidden.Count == 0) return;
        int index = hidden[Random.Range(0, hidden.Count)];
        RevealedMask |= 1 << index;
        if (charge < 0f)
            charge = gameManager != null && gameManager.config != null
                ? gameManager.config.domainChargePerReveal
                : 8f;
        AddDomainCharge(charge);
        OnLetterRevealed?.Invoke(index);
        if (IsAllRevealed) OnAllRevealed?.Invoke();
    }

    public void RevealAllLettersTemporary(float duration)
    {
        temporaryRevealActive = true;
        OnTemporaryReveal?.Invoke();
        StopCoroutine(nameof(HideLettersAfterDelay));
        StartCoroutine(HideLettersAfterDelay(duration));
    }

    System.Collections.IEnumerator HideLettersAfterDelay(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        temporaryRevealActive = false;
        OnTemporaryHide?.Invoke();
    }

    public void HideAllLetters()
    {
        temporaryRevealActive = false;
        OnTemporaryHide?.Invoke();
    }

    public bool IsLetterRevealed(int index)
    {
        if (temporaryRevealActive) return true;
        return (RevealedMask & (1 << index)) != 0;
    }

    public bool GuessTrueName(string guess)
    {
        string clean = guess.Trim().ToUpperInvariant();
        if (clean == EnemyTrueName)
        {
            OnPhaseGuessCorrect?.Invoke();
            return true;
        }
        float penalty = gameManager != null && gameManager.config != null
            ? gameManager.config.exposurePerWrongGuess
            : 25f;
        ExposePlayer(penalty);
        return false;
    }

    public void ExposePlayer(float amount)
    {
        float max = gameManager != null && gameManager.config != null
            ? gameManager.config.playerExposureMax
            : 100f;
        PlayerExposure = Mathf.Clamp(PlayerExposure + amount, 0, max);
        OnExposureChanged?.Invoke(PlayerExposure);
        if (PlayerExposure >= max) OnLose?.Invoke();
    }

    public void ReduceExposure(float amount)
    {
        PlayerExposure = Mathf.Max(0f, PlayerExposure - amount);
        OnExposureChanged?.Invoke(PlayerExposure);
    }

    public void AddDomainCharge(float amount)
    {
        DomainCharge = Mathf.Min(100f, DomainCharge + amount);
        OnDomainChargeChanged?.Invoke(DomainCharge);
    }

    public void ResetDomainCharge()
    {
        DomainCharge = 0f;
        OnDomainChargeChanged?.Invoke(DomainCharge);
    }

    public void AddExposureLayer(int amount)
    {
        int maxLayer = gameManager != null && gameManager.config != null
            ? gameManager.config.exposureLayerMax
            : 3;
        ExposureLayer = Mathf.Min(maxLayer, ExposureLayer + amount);
        OnExposureLayerChanged?.Invoke(ExposureLayer);
        if (ExposureLayer >= maxLayer)
        {
            int extra = gameManager != null && gameManager.config != null
                ? gameManager.config.exposureLayerExtraReveal
                : 1;
            float relief = gameManager != null && gameManager.config != null
                ? gameManager.config.exposureLayerRelief
                : 5f;
            float extraCharge = gameManager != null && gameManager.config != null
                ? gameManager.config.domainChargePerExtraReveal
                : 12f;
            for (int i = 0; i < extra; i++) RevealLetter(extraCharge);
            ReduceExposure(relief);
            ExposureLayer = 0;
            OnExposureLayerChanged?.Invoke(ExposureLayer);
        }
    }
}
