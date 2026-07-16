# TrueName Puzzle + Risk Combat Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:executing-plans` (recommended) or `superpowers:subagent-driven-development` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the integrity-based win condition with a 6-letter enemy true-name puzzle, add risk-based Q/E inputs (Q block, E counter), dual-chant charge, and domain letter reveal.

**Architecture:** Keep the existing `WordSpawner`/`WordProjectile` flight and pool infrastructure. Introduce `WordType` (Normal/Interference/Dual) and decouple input from language: Q is always block, E is always counter. `TrueNameSystem` tracks the hidden enemy name and reveals letters. `HolyLightSystem` manages the Q resource. `PhaseManager` switches phases by revealed letter count.

**Tech Stack:** Unity 2022.3 (Built-in 2D), C# Scripts, Unity UI (`Text`/`InputField`), ScriptableObjects, object pooling.

## Global Constraints

- Platform: macOS Apple Silicon, Metal, 1920×1080 fixed.
- Engine: 团结引擎 1.9.3.
- Rendering: Built-in 2D.
- No `Rigidbody2D`, no complex shaders, particles ≤ 500.
- All dynamic UI created in code to avoid scene merge conflicts.
- `Time.unscaledDeltaTime` for UI/animations during `Time.timeScale` changes.
- Full GDD reference: `Docs/正式设计文档（2D版）-技术补充.md` (version 3.0).

---

### Task 1: Core Data & Config

**Files:**
- Modify: `Assets/Scripts/GameConfig.cs`
- Modify: `Assets/Scripts/TrueNameSystem.cs`
- Create: `Assets/Scripts/HolyLightSystem.cs`
- Modify: `Assets/Scripts/GameManager.cs`
- Modify: `Assets/ScriptableObjects/GameConfig.asset`

**Interfaces:**
- Consumes: `GameManager.config.playerExposureMax`, `GameManager.playerTrueName`.
- Produces: `TrueNameSystem.EnemyTrueName`, `RevealedMask`, `RevealLetter()`, `GuessTrueName()`, `RevealAllLettersTemporary()`, `HideAllLetters()`; `HolyLightSystem.HolyLight`, `TryConsume()`, `Recover()`.

- [ ] **Step 1: Update `GameConfig.cs`**

Replace unused integrity/damage fields with true-name and holy-light fields.

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "TrueName/GameConfig")]
public class GameConfig : ScriptableObject
{
    public string enemyTrueName = "MIRROR";
    public int maxTrueNameLength = 6;

    public float playerExposureMax = 100f;
    public float exposurePerMiss = 4f;
    public float exposurePerWrongHit = 8f;
    public float exposurePerWrongGuess = 25f;

    public float domainChargePerReveal = 8f;
    public float domainChargePerDual = 15f;
    public float domainChargePerDualChant = 10f;
    public float domainExposureRelief = 10f;

    public float holyLightMax = 100f;
    public float holyLightRegenPerSecond = 5f;
    public float holyLightBlockCost = 20f;
    public float holyLightDepletionCooldown = 3f;
    public float eCounterWindow = 0.2f;
    public float dualChantHoldTime = 3f;
    public float dualChantHitExposure = 15f;

    public int comboThreshold = 3;
    public float dualWordWindow = 0.15f;
    public float baseWordSpeed = 120f;
    public float patternBWordSpeed = 180f;
    public float patternCWordSpeed = 80f;
    public float patternBSpawnInterval = 0.4f;
    public int patternCMinLength = 6;
    public int patternCMaxLength = 8;
    public int maxPlayerNameLength = 12;

    public float patternBFailureExposure = 8f;
    public float patternCFailureExposure = 15f;
    public float patternCSuccessReveal = 2f;
}
```

- [ ] **Step 2: Rewrite `TrueNameSystem.cs`**

```csharp
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

    public UnityEvent<int> OnLetterRevealed = new UnityEvent<int>();
    public UnityEvent OnAllRevealed = new UnityEvent();
    public UnityEvent OnTemporaryReveal = new UnityEvent();
    public UnityEvent OnTemporaryHide = new UnityEvent();
    public UnityEvent<float> OnExposureChanged = new UnityEvent<float>();
    public UnityEvent<float> OnDomainChargeChanged = new UnityEvent<float>();
    public UnityEvent OnWin = new UnityEvent();
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
    }

    void Start()
    {
        ResetValues();
    }

    public void ResetValues()
    {
        EnemyTrueName = gameManager != null && !string.IsNullOrEmpty(gameManager.enemyTrueName)
            ? gameManager.enemyTrueName.ToUpperInvariant()
            : "MIRROR";
        if (EnemyTrueName.Length > gameManager.config.maxTrueNameLength)
            EnemyTrueName = EnemyTrueName.Substring(0, gameManager.config.maxTrueNameLength);
        RevealedMask = 0;
        PlayerExposure = 0f;
        DomainCharge = 0f;
        OnLetterRevealed?.Invoke(-1);
        OnExposureChanged?.Invoke(PlayerExposure);
        OnDomainChargeChanged?.Invoke(DomainCharge);
    }

    public void RevealLetter()
    {
        if (IsAllRevealed) return;
        var hidden = new System.Collections.Generic.List<int>();
        for (int i = 0; i < EnemyTrueName.Length; i++)
            if ((RevealedMask & (1 << i)) == 0) hidden.Add(i);
        if (hidden.Count == 0) return;
        int index = hidden[Random.Range(0, hidden.Count)];
        RevealedMask |= 1 << index;
        AddDomainCharge(gameManager.config.domainChargePerReveal);
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
            OnWin?.Invoke();
            return true;
        }
        ExposePlayer(gameManager.config.exposurePerWrongGuess);
        return false;
    }

    public void ExposePlayer(float amount)
    {
        PlayerExposure = Mathf.Clamp(PlayerExposure + amount, 0, gameManager.config.playerExposureMax);
        OnExposureChanged?.Invoke(PlayerExposure);
        if (PlayerExposure >= gameManager.config.playerExposureMax) OnLose?.Invoke();
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
}
```

- [ ] **Step 3: Create `HolyLightSystem.cs`**

```csharp
using UnityEngine;
using UnityEngine.Events;

public class HolyLightSystem : MonoBehaviour
{
    public static HolyLightSystem Instance { get; private set; }

    public GameConfig config;

    public float HolyLight { get; private set; }
    public bool IsDepleted { get; private set; }

    public UnityEvent<float> OnHolyLightChanged = new UnityEvent<float>();

    private float depletionTimer;

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
        if (config == null) config = FindObjectOfType<GameManager>()?.config;
        HolyLight = config.holyLightMax;
        OnHolyLightChanged?.Invoke(HolyLight);
    }

    void Update()
    {
        if (IsDepleted)
        {
            depletionTimer -= Time.deltaTime;
            if (depletionTimer <= 0f)
            {
                IsDepleted = false;
                HolyLight = 0f;
            }
            return;
        }

        if (HolyLight < config.holyLightMax)
        {
            HolyLight = Mathf.Min(config.holyLightMax, HolyLight + config.holyLightRegenPerSecond * Time.deltaTime);
            OnHolyLightChanged?.Invoke(HolyLight);
        }
    }

    public bool TryConsume(float amount)
    {
        if (IsDepleted || amount <= 0) return false;
        if (HolyLight < amount) return false;
        HolyLight -= amount;
        if (HolyLight <= 0f)
        {
            HolyLight = 0f;
            IsDepleted = true;
            depletionTimer = config.holyLightDepletionCooldown;
        }
        OnHolyLightChanged?.Invoke(HolyLight);
        return true;
    }
}
```

- [ ] **Step 4: Update `GameManager.cs`**

Add `enemyTrueName` and generation. Call `GenerateEnemyTrueName` from `SetPlayerName`.

```csharp
public string enemyTrueName = "MIRROR";

public void GenerateEnemyTrueName(string playerName)
{
    if (string.IsNullOrWhiteSpace(playerName))
    {
        enemyTrueName = config.enemyTrueName;
        return;
    }
    string reversed = new string(playerName.Trim().ToUpperInvariant().ToCharArray().Reverse().ToArray());
    if (reversed.Length > config.maxTrueNameLength)
        reversed = reversed.Substring(0, config.maxTrueNameLength);
    while (reversed.Length < 6) reversed += "R";
    enemyTrueName = reversed;
}

public void SetPlayerName(string name)
{
    ...existing logic...
    GenerateEnemyTrueName(playerTrueName);
}
```

- [ ] **Step 5: Update `GameConfig.asset`**

Append the following YAML fields to `Assets/ScriptableObjects/GameConfig.asset` (after existing fields):

```yaml
  enemyTrueName: MIRROR
  maxTrueNameLength: 6
  exposurePerWrongHit: 8
  exposurePerWrongGuess: 25
  domainChargePerReveal: 8
  domainChargePerDualChant: 10
  domainExposureRelief: 10
  holyLightMax: 100
  holyLightRegenPerSecond: 5
  holyLightBlockCost: 20
  holyLightDepletionCooldown: 3
  eCounterWindow: 0.2
  dualChantHoldTime: 3
  dualChantHitExposure: 15
  patternCSuccessReveal: 2
```

- [ ] **Step 6: Verification**

Run the Unity scene in Play mode. Check the Inspector:
- `TrueNameSystem.EnemyTrueName` equals `MIRROR` (or reversed player name).
- `HolyLightSystem.HolyLight` starts at 100 and slowly regenerates if reduced via Inspector.

---

### Task 2: Word & Spawner

**Files:**
- Modify: `Assets/Scripts/WordProjectile.cs`
- Modify: `Assets/Scripts/WordSpawner.cs`

**Interfaces:**
- Consumes: `TrueNameSystem.RevealLetter()`, `HolyLightSystem.TryConsume()`, `GameConfig` values.
- Produces: `WordProjectile.wordType`, `WordSpawner.HandleQBlock()`, `HandleECounter()`, `HandleDualChant()`, `ResolveHit()`, `ResolveMiss()`.

- [ ] **Step 1: Add `WordType` to `WordProjectile.cs`**

```csharp
public enum WordType { Normal, Interference, Dual }

public class WordProjectile : MonoBehaviour
{
    ...
    public WordType wordType;
    public float holdElapsed;
    ...

    public void Init(LanguageType lang, WordPattern pat, WordType type, string word, float sp, Vector2 startPos)
    {
        ...
        wordType = type;
        ...
        if (wordType == WordType.Interference)
        {
            label.color = new Color(0.5f, 0.5f, 0.5f);
            background.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
        }
        ...
    }

    void Update()
    {
        if (State == WordState.Fly)
        {
            Vector2 pos = rectTransform.anchoredPosition;
            pos.x -= speed * Time.deltaTime;
            oscillationTime += Time.deltaTime;
            pos.y = baseY + Mathf.Sin(oscillationTime * 2f * Mathf.PI * sineFrequency) * sineAmplitude;
            rectTransform.anchoredPosition = pos;
            if (pos.x <= defenseLineX + defenseLineHitRange)
            {
                pos.x = defenseLineX;
                rectTransform.anchoredPosition = pos;
                State = WordState.Hold;
                holdElapsed = 0f;
                WordSpawner.Instance?.RegisterHold(this);
                StartCoroutine(HoldRoutine());
            }
        }
        else if (State == WordState.Hold)
        {
            holdElapsed += Time.deltaTime;
        }
    }
}
```

- [ ] **Step 2: Rewrite `WordSpawner` input handling**

Replace the holy/dark input listeners with Q/E/dual-chant listeners and implement the new resolution logic.

```csharp
void OnEnable()
{
    if (inputManager == null) inputManager = FindObjectOfType<InputManager>();
    if (inputManager != null)
    {
        inputManager.OnQPressed.AddListener(OnQPressed);
        inputManager.OnEPressed.AddListener(OnEPressed);
        inputManager.OnDualChantCompleted.AddListener(OnDualChantCompleted);
    }
}

void OnDisable()
{
    if (inputManager != null)
    {
        inputManager.OnQPressed.RemoveListener(OnQPressed);
        inputManager.OnEPressed.RemoveListener(OnEPressed);
        inputManager.OnDualChantCompleted.RemoveListener(OnDualChantCompleted);
    }
}

void OnQPressed()
{
    if (focusWord == null || focusWord.State != WordState.Hold) return;
    if (focusWord.wordType == WordType.Dual)
    {
        TryDualWord(focusWord);
        return;
    }
    QBlock(focusWord);
}

void OnEPressed()
{
    if (focusWord == null || focusWord.State != WordState.Hold) return;
    if (focusWord.wordType == WordType.Dual)
    {
        TryDualWord(focusWord);
        return;
    }
    ECounter(focusWord);
}

void TryDualWord(WordProjectile word)
{
    if (Mathf.Abs(inputManager.LastHolyTime - inputManager.LastDarkTime) <= config.dualWordWindow)
        ResolveHit(word, true);
}

void QBlock(WordProjectile word)
{
    if (HolyLightSystem.Instance?.TryConsume(config.holyLightBlockCost) == true)
        ResolveHit(word, false);
}

void ECounter(WordProjectile word)
{
    if (word.holdElapsed <= config.eCounterWindow)
        ResolveHit(word, true);
    else
        ResolveMiss(word);
}

void ResolveHit(WordProjectile word, bool reveal)
{
    if (word.wordType == WordType.Interference)
    {
        trueNameSystem?.ExposePlayer(config.exposurePerWrongHit);
        combo = 0;
        UIManager.Instance?.UpdateCombo(combo);
    }
    else if (reveal)
    {
        int revealCount = word.pattern == WordPattern.C ? Mathf.RoundToInt(config.patternCSuccessReveal) : 1;
        for (int i = 0; i < revealCount; i++) trueNameSystem?.RevealLetter();
        if (word.wordType == WordType.Dual)
            trueNameSystem?.AddDomainCharge(config.domainChargePerDual);
        if (word.wordType != WordType.Dual) combo++;
        UIManager.Instance?.UpdateCombo(combo);
    }
    // Q block on normal word: no reveal, no combo, no penalty
    WordHitEffect.Instance?.Play(word.rectTransform.anchoredPosition, word.language, true);
    RemoveFromHold(word);
    activeWords.Remove(word);
    Release(word);
}

void ResolveMiss(WordProjectile word)
{
    if (word.wordType == WordType.Normal || word.wordType == WordType.Dual)
    {
        float exposure = word.pattern == WordPattern.B ? config.patternBFailureExposure
            : (word.pattern == WordPattern.C ? config.patternCFailureExposure : config.exposurePerMiss);
        if (inputManager.IsDualChanting) exposure += config.dualChantHitExposure;
        trueNameSystem?.ExposePlayer(exposure);
    }
    combo = 0;
    UIManager.Instance?.UpdateCombo(combo);
    WordHitEffect.Instance?.Play(word.rectTransform.anchoredPosition, word.language, false);
    RemoveFromHold(word);
    activeWords.Remove(word);
    Release(word);
}

void OnDualChantCompleted()
{
    trueNameSystem?.AddDomainCharge(config.domainChargePerDualChant);
    trueNameSystem?.RevealLetter();
    ClearAllWords();
}
```

- [ ] **Step 3: Update `SpawnWord` to assign `WordType`**

```csharp
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
        combo = 0;
        UIManager.Instance?.UpdateCombo(combo);
        lang = LanguageType.Dual;
    }
    else
    {
        type = Random.value < GetInterferenceChance() ? WordType.Interference : WordType.Normal;
        lang = Random.Range(0, 2) == 0 ? LanguageType.Sacred : LanguageType.Demonic;
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
    return phase == 0 ? 0.2f : (phase == 1 ? 0.4f : 0.6f);
}
```

- [ ] **Step 4: Verification**

Play in editor:
- Normal words are gold/purple; Q destroys with no reveal; E in the first 0.2s reveals a letter.
- Interference words are gray; pressing Q/E destroys them but adds exposure; letting them pass does nothing.
- After 3 successful E counters, a dual word appears; Q+E within 0.15s reveals a letter and grants extra charge.

---

### Task 3: Input System

**Files:**
- Modify: `Assets/Scripts/InputManager.cs`

**Interfaces:**
- Consumes: `GameManager.State`, `GameConfig.dualChantHoldTime`.
- Produces: `OnQPressed`, `OnEPressed`, `OnDualChantStarted`, `OnDualChantCompleted`, `OnGuessPressed`, `OnDomainPressed`, `IsDualChanting`, `LastHolyTime`, `LastDarkTime`.

- [ ] **Step 1: Rewrite `InputManager.cs`**

```csharp
using UnityEngine;
using UnityEngine.Events;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public UnityEvent OnQPressed = new UnityEvent();
    public UnityEvent OnEPressed = new UnityEvent();
    public UnityEvent OnDualChantStarted = new UnityEvent();
    public UnityEvent OnDualChantCompleted = new UnityEvent();
    public UnityEvent OnGuessPressed = new UnityEvent();
    public UnityEvent OnDomainPressed = new UnityEvent();

    public float LastHolyTime { get; private set; }
    public float LastDarkTime { get; private set; }
    public bool IsDualChanting { get; private set; }

    private GameManager gameManager;
    private GameConfig config;
    private float dualChantTimer;
    private bool qHeld;
    private bool eHeld;

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
        gameManager = GameManager.Instance ?? FindObjectOfType<GameManager>();
        config = gameManager?.config;
    }

    void Update()
    {
        GameState state = gameManager != null ? gameManager.State : GameState.Intro;
        if (state != GameState.Combat && state != GameState.Domain) return;

        if (Input.GetKeyDown(KeyCode.Q)) { LastHolyTime = Time.time; OnQPressed?.Invoke(); }
        if (Input.GetKeyDown(KeyCode.E)) { LastDarkTime = Time.time; OnEPressed?.Invoke(); }
        if (Input.GetKeyDown(KeyCode.Space)) OnDomainPressed?.Invoke();
        if (Input.GetKeyDown(KeyCode.Return)) OnGuessPressed?.Invoke();

        qHeld = Input.GetKey(KeyCode.Q);
        eHeld = Input.GetKey(KeyCode.E);

        if (qHeld && eHeld && !IsDualChanting)
        {
            IsDualChanting = true;
            dualChantTimer = 0f;
            OnDualChantStarted?.Invoke();
        }

        if (IsDualChanting)
        {
            if (!qHeld || !eHeld)
            {
                IsDualChanting = false;
                dualChantTimer = 0f;
            }
            else
            {
                dualChantTimer += Time.deltaTime;
                if (dualChantTimer >= (config?.dualChantHoldTime ?? 3f))
                {
                    IsDualChanting = false;
                    dualChantTimer = 0f;
                    OnDualChantCompleted?.Invoke();
                }
            }
        }
    }
}
```

- [ ] **Step 2: Verification**

Add temporary `Debug.Log` calls inside each `UnityEvent` listener or in the methods invoked by them. Press Play, then press Q, E, Space, Return, and hold Q+E for 3 seconds. Confirm each event fires exactly once per intended action.

---

### Task 4: Domain & Phase

**Files:**
- Modify: `Assets/Scripts/DomainManager.cs`
- Modify: `Assets/Scripts/PhaseManager.cs`
- Modify: `Assets/Scripts/PhaseData.cs`
- Modify: `Assets/ScriptableObjects/Phase_1.asset`, `Phase_2.asset`, `Phase_3.asset`

**Interfaces:**
- Consumes: `TrueNameSystem.RevealAllLettersTemporary()`, `ExposePlayer()`, `ResetDomainCharge()`; `TrueNameSystem.OnLetterRevealed`.
- Produces: domain expansion that reveals letters; phase transitions based on letter count.

- [ ] **Step 1: Update `DomainManager.cs`**

Replace the damage-based domain with letter reveal.

```csharp
IEnumerator DomainExpand()
{
    active = true;
    if (gameManager != null) gameManager.EnterDomain();

    float originalTimeScale = Time.timeScale;
    Time.timeScale = 0.1f;

    wordSpawner?.ClearAllWords();
    trueNameSystem?.RevealAllLettersTemporary(3f);
    trueNameSystem?.ExposePlayer(-config.domainExposureRelief);
    trueNameSystem?.ResetDomainCharge();
    OnDomainExpanded?.Invoke();
    DomainVisual.Instance?.Play();

    yield return new WaitForSecondsRealtime(3f);

    Time.timeScale = originalTimeScale;
    active = false;

    if (gameManager != null && gameManager.State == GameState.Domain)
        gameManager.ChangeState(GameState.Combat);
}
```

- [ ] **Step 2: Update `PhaseData.cs` and `PhaseManager.cs`**

`PhaseData.cs`:

```csharp
public int letterThreshold = 0;
```

`PhaseManager.cs`:

```csharp
void Start()
{
    ...
    phases = phases.Where(p => p != null).OrderByDescending(p => p.letterThreshold).ToList();
    ...
    if (trueNameSystem != null)
        trueNameSystem.OnLetterRevealed.AddListener(CheckPhase);
    ...
}

void OnDestroy()
{
    if (trueNameSystem != null)
        trueNameSystem.OnLetterRevealed.RemoveListener(CheckPhase);
}

void CheckPhase(int letterIndex)
{
    if (transitioning) return;
    int revealedCount = trueNameSystem != null ? CountBits(trueNameSystem.RevealedMask) : 0;
    int target = 0;
    for (int i = 0; i < phases.Count; i++)
    {
        if (revealedCount >= phases[i].letterThreshold)
            target = i;
    }
    if (target > CurrentPhaseIndex)
        StartCoroutine(TransitionTo(target));
}

int CountBits(int mask)
{
    int count = 0;
    while (mask != 0)
    {
        count += mask & 1;
        mask >>= 1;
    }
    return count;
}
```

- [ ] **Step 3: Update `Phase_*.asset`**

Replace `integrityThreshold` with `letterThreshold` in each asset YAML. Set values:
- `Phase_1.asset`: `letterThreshold: 0`
- `Phase_2.asset`: `letterThreshold: 3`
- `Phase_3.asset`: `letterThreshold: 5`

- [ ] **Step 4: Verification**

Play scene, reveal letters via debug or by hitting words, and confirm phase transitions at 3 and 5 letters. Press Space when domain charge is full and confirm all letters appear for 3 seconds.

---

### Task 5: UI

**Files:**
- Modify: `Assets/Scripts/UIManager.cs`

**Interfaces:**
- Consumes: `TrueNameSystem.OnLetterRevealed`, `OnTemporaryReveal`, `OnTemporaryHide`, `EnemyTrueName`, `RevealedMask`; `HolyLightSystem.OnHolyLightChanged`; `InputManager.OnGuessPressed`.
- Produces: true name slot UI, holy light bar, guess input panel.

- [ ] **Step 1: Add True Name Slots**

Add the following to `UIManager`:

```csharp
public Text trueNameSlotTemplate;
private Text[] trueNameSlots;

void EnsureTrueNameSlots()
{
    if (trueNameSlots != null) return;
    if (hudPanel == null) return;

    GameObject container = new GameObject("TrueNameSlots", typeof(RectTransform));
    container.transform.SetParent(hudPanel.transform, false);
    RectTransform crt = container.GetComponent<RectTransform>();
    crt.anchorMin = new Vector2(0.5f, 1f);
    crt.anchorMax = new Vector2(0.5f, 1f);
    crt.pivot = new Vector2(0.5f, 1f);
    crt.anchoredPosition = new Vector2(0f, -100f);

    int len = TrueNameSystem.Instance?.EnemyTrueName.Length ?? 6;
    trueNameSlots = new Text[len];
    for (int i = 0; i < len; i++)
    {
        GameObject slot = new GameObject("Slot" + i, typeof(RectTransform), typeof(Text));
        slot.transform.SetParent(container.transform, false);
        RectTransform rt = slot.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2((i - (len - 1) / 2f) * 100f, 0f);
        rt.sizeDelta = new Vector2(80f, 80f);
        Text t = slot.GetComponent<Text>();
        t.fontSize = 48;
        t.color = new Color(0.4f, 0.4f, 0.4f);
        t.alignment = TextAnchor.MiddleCenter;
        t.text = "_";
        trueNameSlots[i] = t;
    }
}

void UpdateTrueNameSlots(int index)
{
    if (trueNameSlots == null || TrueNameSystem.Instance == null) return;
    string name = TrueNameSystem.Instance.EnemyTrueName;
    for (int i = 0; i < trueNameSlots.Length; i++)
    {
        if (i >= name.Length) continue;
        bool revealed = TrueNameSystem.Instance.IsLetterRevealed(i);
        trueNameSlots[i].text = revealed ? name[i].ToString() : "_";
        trueNameSlots[i].color = revealed ? new Color(0.831f, 0.686f, 0.216f) : new Color(0.4f, 0.4f, 0.4f);
    }
}
```

In `Start`, wire events:

```csharp
if (TrueNameSystem.Instance != null)
{
    TrueNameSystem.Instance.OnLetterRevealed.AddListener(UpdateTrueNameSlots);
    TrueNameSystem.Instance.OnTemporaryReveal.AddListener(() => UpdateTrueNameSlots(-1));
    TrueNameSystem.Instance.OnTemporaryHide.AddListener(() => UpdateTrueNameSlots(-1));
    TrueNameSystem.Instance.OnExposureChanged.AddListener(UpdateExposure);
    TrueNameSystem.Instance.OnDomainChargeChanged.AddListener(UpdateDomain);
    TrueNameSystem.Instance.OnWin.AddListener(() => GameManager.Instance?.TriggerWin());
    TrueNameSystem.Instance.OnLose.AddListener(() => GameManager.Instance?.TriggerLose());
}
if (HolyLightSystem.Instance != null)
    HolyLightSystem.Instance.OnHolyLightChanged.AddListener(UpdateHolyLight);
if (InputManager.Instance != null)
    InputManager.Instance.OnGuessPressed.AddListener(ShowGuessPanel);
```

- [ ] **Step 2: Add Holy Light Bar**

```csharp
public Text holyLightText;

void EnsureHolyLightText()
{
    if (holyLightText != null) return;
    if (hudPanel == null) return;
    GameObject go = new GameObject("HolyLightText", typeof(RectTransform), typeof(Text));
    go.transform.SetParent(hudPanel.transform, false);
    RectTransform rt = go.GetComponent<RectTransform>();
    rt.anchorMin = new Vector2(0f, 0f);
    rt.anchorMax = new Vector2(0f, 0f);
    rt.pivot = new Vector2(0f, 0f);
    rt.anchoredPosition = new Vector2(40f, 40f);
    rt.sizeDelta = new Vector2(250f, 40f);
    Text t = go.GetComponent<Text>();
    t.fontSize = 24;
    t.color = new Color(0.831f, 0.686f, 0.216f);
    t.alignment = TextAnchor.MiddleLeft;
    holyLightText = t;
}

void UpdateHolyLight(float value)
{
    if (holyLightText != null) holyLightText.text = "圣光: " + value.ToString("0") + "%";
}
```

- [ ] **Step 3: Add Guess Input Panel**

```csharp
public GameObject guessPanel;
public InputField guessInput;
public Button guessConfirmButton;

void EnsureGuessPanel()
{
    if (guessPanel != null) return;
    if (hudPanel == null) return;
    GameObject panel = new GameObject("GuessPanel", typeof(RectTransform), typeof(Image));
    panel.transform.SetParent(hudPanel.transform, false);
    panel.transform.SetAsLastSibling();
    RectTransform rt = panel.GetComponent<RectTransform>();
    rt.anchorMin = new Vector2(0.5f, 0.5f);
    rt.anchorMax = new Vector2(0.5f, 0.5f);
    rt.pivot = new Vector2(0.5f, 0.5f);
    rt.anchoredPosition = Vector2.zero;
    rt.sizeDelta = new Vector2(500f, 240f);
    Image img = panel.GetComponent<Image>();
    img.color = new Color(0f, 0f, 0f, 0.85f);
    img.raycastTarget = true;

    GameObject input = new GameObject("GuessInput", typeof(RectTransform), typeof(InputField));
    input.transform.SetParent(panel.transform, false);
    RectTransform inputRT = input.GetComponent<RectTransform>();
    inputRT.anchorMin = new Vector2(0.5f, 0.6f);
    inputRT.anchorMax = new Vector2(0.5f, 0.6f);
    inputRT.pivot = new Vector2(0.5f, 0.5f);
    inputRT.anchoredPosition = Vector2.zero;
    inputRT.sizeDelta = new Vector2(400f, 60f);
    InputField field = input.GetComponent<InputField>();
    field.textComponent = input.GetComponent<Text>();
    if (field.textComponent == null)
    {
        field.textComponent = input.AddComponent<Text>();
        field.textComponent.fontSize = 32;
        field.textComponent.color = Color.white;
        field.textComponent.alignment = TextAnchor.MiddleCenter;
    }
    field.contentType = InputField.ContentType.Alphanumeric;
    field.characterLimit = TrueNameSystem.Instance?.EnemyTrueName.Length ?? 6;
    guessInput = field;

    GameObject btn = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button));
    btn.transform.SetParent(panel.transform, false);
    RectTransform btnRT = btn.GetComponent<RectTransform>();
    btnRT.anchorMin = new Vector2(0.5f, 0.3f);
    btnRT.anchorMax = new Vector2(0.5f, 0.3f);
    btnRT.pivot = new Vector2(0.5f, 0.5f);
    btnRT.anchoredPosition = Vector2.zero;
    btnRT.sizeDelta = new Vector2(180f, 50f);
    Text btnText = btn.AddComponent<Text>();
    btnText.text = "确认";
    btnText.fontSize = 24;
    btnText.color = Color.white;
    btnText.alignment = TextAnchor.MiddleCenter;
    guessConfirmButton = btn.GetComponent<Button>();
    guessConfirmButton.onClick.AddListener(OnGuessConfirm);

    guessPanel = panel;
    guessPanel.SetActive(false);
}

void ShowGuessPanel()
{
    if (GameManager.Instance != null && GameManager.Instance.State != GameState.Combat) return;
    EnsureGuessPanel();
    if (guessPanel == null) return;
    guessPanel.SetActive(true);
    guessInput.text = "";
    guessInput.ActivateInputField();
    Time.timeScale = 0f;
}

void OnGuessConfirm()
{
    if (TrueNameSystem.Instance != null)
    {
        bool correct = TrueNameSystem.Instance.GuessTrueName(guessInput.text);
        if (!correct)
            DialogueController.Instance?.Show("不是这个真名。暴露度 +25%。", 2.5f);
    }
    guessPanel.SetActive(false);
    Time.timeScale = 1f;
}
```

- [ ] **Step 4: Remove obsolete integrity UI**

Remove or comment out the `integrityText` field and `UpdateIntegrity` method. Change `UpdateDomain` to label "领域" and `UpdateExposure` to label "暴露度".

- [ ] **Step 5: Verification**

Play scene, reveal letters, confirm slots update. Press Enter, type a wrong guess, confirm exposure increases. Type the correct guess, confirm win panel appears.

---

### Task 6: Integration & Final Verification

**Files:**
- Modify: `Assets/Scripts/UIManager.cs` (simulate buttons)
- Modify: `Assets/Scripts/GameManager.cs` (if needed)

- [ ] **Step 1: Update debug simulate buttons**

Change `SimulateHit` to reveal a letter and `SimulateMiss` to add exposure.

```csharp
void SimulateHit()
{
    TrueNameSystem.Instance?.RevealLetter();
    TrueNameSystem.Instance?.AddDomainCharge(GameManager.Instance.config.domainChargePerReveal);
}

void SimulateMiss()
{
    TrueNameSystem.Instance?.ExposePlayer(GameManager.Instance.config.exposurePerMiss);
}
```

- [ ] **Step 2: Full play-through checklist**

Run the scene in Play mode and verify:
1. Start game, enter player name.
2. Enter Combat; true name slots appear as `_ _ _ _ _ _`.
3. Hit normal words with E within 0.2s to reveal letters; combo UI increments.
4. Let interference words pass without penalty; hitting them adds exposure.
5. Use Q block when low on exposure; holy light decreases and regenerates.
6. Hold Q+E for 3 seconds to clear all words and reveal one letter.
7. Build domain charge to 100% and press Space; all letters reveal for 3 seconds, exposure decreases by 10%.
8. Press Enter and guess the correct true name → Win panel.
9. Press Enter and guess incorrectly → +25% exposure, dialogue feedback.
10. Reach 100% exposure → Lose panel.

---

## Spec Coverage Self-Check

| GDD Section | Implementing Task | Notes |
|-------------|-------------------|-------|
| 真名拼图系统 | Task 1 | `TrueNameSystem` |
| 普通/干扰词 | Task 2 | `WordType` + `WordSpawner` |
| Q 圣盾 / E 反击 | Task 2, 3 | `InputManager` + `WordSpawner` |
| 连携与双诵 | Task 2 | combo + dual word handling |
| 圣光资源 | Task 1 | `HolyLightSystem` |
| 长按 Q+E 双诵蓄力 | Task 3 | `InputManager` + `WordSpawner` |
| 领域展开（揭示字母） | Task 4 | `DomainManager` |
| 阶段按字母数 | Task 4 | `PhaseManager` + `PhaseData` |
| 顶部真名槽 / HUD | Task 5 | `UIManager` |
| 猜名输入 | Task 5 | `UIManager` guess panel |

No placeholders remain in this plan. Every task ends with a manual verification step.
