using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WordSpawner : MonoBehaviour
{
    public static WordSpawner Instance { get; private set; }

    public GameObject wordPrefab;
    public RectTransform wordContainer;
    public GameConfig config;
    public WordDatabase wordDatabase;
    public TrueNameSystem trueNameSystem;
    public InputManager inputManager;
    public PhaseManager phaseManager;

    public float defenseLineX = 640f;
    public float spawnRightX = 2020f;
    public float minY = 300f;
    public float maxY = 780f;
    public int poolSize = 30;

    public float phaseSpeedMultiplier = 1f;
    public float patternInterval = 2f;

    private List<WordProjectile> activeWords = new List<WordProjectile>();
    private Queue<WordProjectile> holdQueue = new Queue<WordProjectile>();
    private int combo;
    private bool inCombat;
    private Coroutine loop;
    private Queue<WordProjectile> pool = new Queue<WordProjectile>();
    private DefenseLine defenseLine;
    private GameManager gameManager;

    TrueNameSystem TrueNameSystemInstance => trueNameSystem ?? (trueNameSystem = FindObjectOfType<TrueNameSystem>());

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
        if (config == null) config = GameManager.Instance?.config;

        HolyLightSystem.EnsureExists();
        ParticleManager.EnsureExists();
        AudioManager.EnsureExists();
        FontManager.EnsureExists();

        gameManager = GameManager.Instance ?? FindObjectOfType<GameManager>();
        if (gameManager != null)
            gameManager.OnStateChanged.AddListener(OnStateChanged);

        if (inputManager == null) inputManager = FindObjectOfType<InputManager>();
        if (inputManager != null)
        {
            inputManager.OnQPressed.AddListener(OnQPressed);
            inputManager.OnEPressed.AddListener(OnEPressed);
            inputManager.OnDualChantCompleted.AddListener(OnDualChantCompleted);
        }

        if (trueNameSystem == null) trueNameSystem = FindObjectOfType<TrueNameSystem>();
        if (phaseManager == null) phaseManager = FindObjectOfType<PhaseManager>();

        PrewarmPool();
        EnsureDefenseLine();
    }

    void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnStateChanged.RemoveListener(OnStateChanged);
        if (inputManager != null)
        {
            inputManager.OnQPressed.RemoveListener(OnQPressed);
            inputManager.OnEPressed.RemoveListener(OnEPressed);
            inputManager.OnDualChantCompleted.RemoveListener(OnDualChantCompleted);
        }
    }

    void OnStateChanged(GameState state)
    {
        if (state == GameState.Combat)
            StartCombat();
        else
            StopCombat();
    }

    void StartCombat()
    {
        if (inCombat) return;
        inCombat = true;
        if (defenseLine != null) defenseLine.gameObject.SetActive(true);
        loop = StartCoroutine(CombatLoop());
    }

    void StopCombat()
    {
        inCombat = false;
        if (defenseLine != null) defenseLine.gameObject.SetActive(false);
        if (loop != null)
        {
            StopCoroutine(loop);
            loop = null;
        }
        ClearAllWords();
    }

    public void ClearAllWords()
    {
        holdQueue.Clear();
        foreach (var word in activeWords.ToList())
            Release(word);
        activeWords.Clear();
    }

    void PrewarmPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = Instantiate(wordPrefab, wordContainer);
            go.SetActive(false);
            WordProjectile wp = go.GetComponent<WordProjectile>();
            if (wp != null) pool.Enqueue(wp);
            else Destroy(go);
        }
    }

    IEnumerator CombatLoop()
    {
        while (inCombat)
        {
            int phase = phaseManager != null ? phaseManager.CurrentPhaseIndex : 0;
            if (phase == 0)
                yield return PatternA();
            else if (phase == 1)
                yield return Random.Range(0, 3) == 0 ? PatternB() : PatternA();
            else
            {
                int r = Random.Range(0, 5);
                if (r == 0) yield return PatternC();
                else if (r <= 2) yield return PatternB();
                else yield return PatternA();
            }
            yield return new WaitForSeconds(patternInterval);
        }
    }

    IEnumerator PatternA()
    {
        SpawnWord(config.baseWordSpeed, false, WordPattern.A);
        yield return new WaitUntil(() => activeWords.Count == 0);
    }

    IEnumerator PatternB()
    {
        for (int i = 0; i < 4; i++)
        {
            SpawnWord(config.patternBWordSpeed, false, WordPattern.B);
            yield return new WaitForSeconds(config.patternBSpawnInterval);
        }
        yield return new WaitUntil(() => activeWords.Count == 0);
    }

    IEnumerator PatternC()
    {
        SpawnWord(config.patternCWordSpeed, true, WordPattern.C);
        yield return new WaitUntil(() => activeWords.Count == 0);
    }

    void SpawnWord(float speed, bool isLong, WordPattern pattern)
    {
        if (wordDatabase == null || config == null) return;

        LanguageType lang;
        string word;
        WordType type;

        if (combo >= config.comboThreshold)
        {
            type = WordType.Dual;
            word = GetRandomWord(wordDatabase.dualWords);
            lang = LanguageType.Dual;
            combo = 0;
            UIManager.Instance?.UpdateCombo(combo);
        }
        else
        {
            type = Random.value < GetInterferenceChance() ? WordType.Interference : WordType.Normal;
            lang = type == WordType.Interference ? LanguageType.Interference : (Random.Range(0, 2) == 0 ? LanguageType.Sacred : LanguageType.Demonic);
            word = lang == LanguageType.Sacred ? GetRandomWord(wordDatabase.holyWords) : GetRandomWord(wordDatabase.darkWords);
        }

        if (isLong && type != WordType.Dual)
            word = MakeLongWord(word);

        WordProjectile wp = GetWord();
        if (wp == null) return;
        wp.defenseLineX = defenseLineX;
        Vector2 startPos = new Vector2(spawnRightX, Random.Range(minY, maxY) - 540f);
        wp.Init(lang, pattern, type, word, speed * phaseSpeedMultiplier, startPos);

        activeWords.Add(wp);
    }

    float GetInterferenceChance()
    {
        int phase = phaseManager != null ? phaseManager.CurrentPhaseIndex : 0;
        if (config != null && config.interferenceChances != null && phase < config.interferenceChances.Count)
            return config.interferenceChances[phase];
        return phase == 0 ? 0.2f : (phase == 1 ? 0.4f : 0.6f);
    }

    string GetRandomWord(List<string> list)
    {
        if (list == null || list.Count == 0) return "???";
        return list[Random.Range(0, list.Count)];
    }

    string MakeLongWord(string word)
    {
        int len = Random.Range(config.patternCMinLength, config.patternCMaxLength + 1);
        if (word.Length >= len) return word.Substring(0, len);
        while (word.Length < len)
            word += word;
        return word.Substring(0, len);
    }

    WordProjectile GetWord()
    {
        if (pool.Count > 0)
        {
            WordProjectile wp = pool.Dequeue();
            wp.gameObject.SetActive(true);
            return wp;
        }
        GameObject go = Instantiate(wordPrefab, wordContainer);
        return go.GetComponent<WordProjectile>();
    }

    public void RegisterHold(WordProjectile word)
    {
        if (!holdQueue.Contains(word))
            holdQueue.Enqueue(word);
    }

    void OnQPressed()
    {
        if (holdQueue.Count == 0) return;
        foreach (var word in GetHoldWords())
        {
            if (word == null || word.State != WordState.Hold) continue;
            if (word.wordType == WordType.Dual)
            {
                if (TryDualWord(word)) return;
                continue;
            }
            if (word.wordType == WordType.Interference || word.language == LanguageType.Demonic)
            {
                QBlock(word);
                return;
            }
            if (word.language == LanguageType.Sacred)
            {
                ResolveHit(word, true);
                return;
            }
        }
    }

    void OnEPressed()
    {
        if (holdQueue.Count == 0) return;
        foreach (var word in GetHoldWords())
        {
            if (word == null || word.State != WordState.Hold) continue;
            if (word.wordType == WordType.Dual)
            {
                if (TryDualWord(word)) return;
                continue;
            }
            if (word.wordType == WordType.Interference)
            {
                ResolveMiss(word, true);
                return;
            }
            if (word.language == LanguageType.Demonic)
            {
                if (config == null || word.holdElapsed <= config.eCounterWindow)
                {
                    ResolveHit(word, true, true);
                }
                else
                {
                    ResolveMiss(word, true);
                }
                return;
            }
        }

        WordProjectile first = GetFirstHoldWord();
        if (first != null) ResolveMiss(first, true);
    }

    void OnDualChantCompleted()
    {
        ClearAllWords();
        TrueNameSystem.Instance?.RevealLetter();
        TrueNameSystem.Instance?.AddDomainCharge(config != null ? config.domainChargePerDualChant : 10f);
        DomainVisual.Instance?.Play();
        CameraShake.Instance?.Shake(0.18f, 0.3f);
        SilhouetteDirector.Instance?.playerSilhouette?.TriggerAttack();
    }

    bool TryDualWord(WordProjectile word)
    {
        if (Mathf.Abs(inputManager.LastHolyTime - inputManager.LastDarkTime) <= config.dualWordWindow)
        {
            ResolveHit(word, true, true);
            return true;
        }
        return false;
    }

    List<WordProjectile> GetHoldWords()
    {
        return new List<WordProjectile>(holdQueue);
    }

    WordProjectile GetFirstHoldWord()
    {
        while (holdQueue.Count > 0)
        {
            WordProjectile w = holdQueue.Peek();
            if (w != null && w.State == WordState.Hold) return w;
            holdQueue.Dequeue();
        }
        return null;
    }

    void QBlock(WordProjectile word)
    {
        var hls = HolyLightSystem.Instance;
        bool consumed = hls?.TryConsume(config.holyLightBlockCost) == true;
        if (!consumed)
        {
            Debug.LogWarning($"Q盾未生效：hls={hls}, holyLight={hls?.HolyLight}, cost={config?.holyLightBlockCost}");
            ResolveMiss(word, true);
        }
        else
        {
            ResolveHit(word, false);
        }
    }

    void ResolveHit(WordProjectile word, bool reveal, bool addLayer = false)
    {
        if (TrueNameSystemInstance == null)
        {
            Debug.LogError("TrueNameSystemInstance is null in ResolveHit!");
            return;
        }
        if (!activeWords.Contains(word)) return;

        if (word.wordType == WordType.Interference)
        {
            // Q shield blocked an interference word; no penalty.
        }
        else if (reveal)
        {
            int revealCount = word.pattern == WordPattern.C ? Mathf.RoundToInt(config.patternCSuccessReveal) : 1;
            float charge = addLayer ? config.domainChargePerECounter : config.domainChargePerReveal;
            for (int i = 0; i < revealCount; i++) TrueNameSystemInstance?.RevealLetter(charge);
            if (addLayer) TrueNameSystemInstance?.AddExposureLayer(1);
            if (word.wordType == WordType.Dual)
            {
                TrueNameSystemInstance?.AddDomainCharge(config.domainChargePerDual);
            }
            else
            {
                combo++;
                var tns = TrueNameSystemInstance;
                if (tns != null && tns.IsAllRevealed)
                    tns.AddDomainCharge(config.domainChargePerReveal);
            }
            UIManager.Instance?.UpdateCombo(combo);
        }
        // else: Q shield blocked a demonic word; no penalty.

        CameraShake.Instance?.Shake(word.wordType == WordType.Dual ? 0.3f : 0.05f,
            word.wordType == WordType.Dual ? 0.15f : 0.08f);
        WordHitEffect.Instance?.Play(word.rectTransform.anchoredPosition, word.language, true);
        ParticleManager.Instance?.PlayHit(word.rectTransform.anchoredPosition, word.language);
        AudioManager.Instance?.PlayHit();
        if (reveal)
        {
            SilhouetteDirector.Instance?.playerSilhouette?.TriggerAttack();
        }
        RemoveFromHold(word);
        activeWords.Remove(word);
        Release(word);
    }

    public void HandleMiss(WordProjectile word)
    {
        ResolveMiss(word, false);
    }

    void ResolveMiss(WordProjectile word, bool wrongInput)
    {
        if (!activeWords.Contains(word)) return;

        float exposure = wrongInput ? config.exposurePerWrongHit : GetMissExposure(word);
        if (exposure > 0f) TrueNameSystemInstance?.ExposePlayer(exposure);
        combo = 0;
        UIManager.Instance?.UpdateCombo(combo);
        CameraShake.Instance?.Shake(0.12f, 0.2f);
        WordHitEffect.Instance?.Play(word.rectTransform.anchoredPosition, word.language, false);
        ParticleManager.Instance?.PlayMiss(word.rectTransform.anchoredPosition, word.language);
        AudioManager.Instance?.PlayMiss();
        SilhouetteDirector.Instance?.enemySilhouette?.TriggerAttack();
        SilhouetteDirector.Instance?.playerSilhouette?.TriggerTakeHit();
        RemoveFromHold(word);
        activeWords.Remove(word);
        Release(word);
    }

    float GetMissExposure(WordProjectile word)
    {
        if (word.wordType == WordType.Interference) return 0f;
        if (word.wordType == WordType.Dual) return config.exposurePerMiss;
        if (word.pattern == WordPattern.B) return config.patternBFailureExposure;
        if (word.pattern == WordPattern.C) return config.patternCFailureExposure;
        return config.exposurePerMiss;
    }

    void RemoveFromHold(WordProjectile word)
    {
        Queue<WordProjectile> temp = new Queue<WordProjectile>();
        while (holdQueue.Count > 0)
        {
            WordProjectile w = holdQueue.Dequeue();
            if (w != word) temp.Enqueue(w);
        }
        holdQueue = temp;
    }

    void Release(WordProjectile word)
    {
        word.gameObject.SetActive(false);
        word.State = WordState.Fly;
        word.holdElapsed = 0f;
        pool.Enqueue(word);
    }

    void EnsureDefenseLine()
    {
        if (defenseLine == null)
            defenseLine = FindObjectOfType<DefenseLine>();
        if (defenseLine == null)
        {
            GameObject lineObj = new GameObject("DefenseLine");
            defenseLine = lineObj.AddComponent<DefenseLine>();
        }
        defenseLine.SetPosition(defenseLineX);
        if (defenseLine.gameObject != null)
            defenseLine.gameObject.SetActive(false);
    }
}
