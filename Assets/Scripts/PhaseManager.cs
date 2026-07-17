using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PhaseManager : MonoBehaviour
{
    public static PhaseManager Instance { get; private set; }

    public List<PhaseData> phases = new List<PhaseData>();
    public GameManager gameManager;
    public TrueNameSystem trueNameSystem;
    public WordSpawner wordSpawner;
    public Image backgroundImage;

    public int CurrentPhaseIndex { get; private set; } = 0;

    public UnityEvent<int> OnPhaseChanged = new UnityEvent<int>();

    public bool IsTransitioning => transitioning;

    private bool transitioning = false;

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
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (trueNameSystem == null) trueNameSystem = FindObjectOfType<TrueNameSystem>();
        if (wordSpawner == null) wordSpawner = FindObjectOfType<WordSpawner>();

        phases = phases.Where(p => p != null).ToList();

        if (phases.Count == 0)
        {
            Debug.LogWarning("PhaseManager: no phases assigned.");
            return;
        }

        if (trueNameSystem != null)
            trueNameSystem.OnPhaseGuessCorrect.AddListener(OnPhaseGuessCorrect);

        EnterPhase(0, false);
    }

    void OnDestroy()
    {
        if (trueNameSystem != null)
            trueNameSystem.OnPhaseGuessCorrect.RemoveListener(OnPhaseGuessCorrect);
    }

    void OnPhaseGuessCorrect()
    {
        if (transitioning) return;

        if (CurrentPhaseIndex >= phases.Count - 1)
        {
            gameManager?.TriggerWin();
            return;
        }

        StartCoroutine(TransitionTo(CurrentPhaseIndex + 1));
    }

    IEnumerator TransitionTo(int targetIndex)
    {
        transitioning = true;

        CinematicCamera.Instance?.SlowMotion(0f, 0.3f);
        yield return new WaitForSecondsRealtime(0.3f);

        EnterPhase(targetIndex, true);
        CinematicCamera.Instance?.Zoom(1.05f, 0.3f);

        string line = GetRandomLine(phases[targetIndex]);
        if (!string.IsNullOrWhiteSpace(line))
        {
            DialogueController.Instance?.Show(line, 1.5f);
            yield return new WaitForSecondsRealtime(2f);
        }
        else
        {
            yield return new WaitForSecondsRealtime(0.8f);
        }

        transitioning = false;
    }

    void SetPhaseVignette(int phase)
    {
        if (CinematicOverlay.Instance == null) return;
        Color tint;
        float alpha;
        switch (phase)
        {
            default:
            case 0:
                tint = new Color(0.2f, 0.1f, 0.3f);
                alpha = 0.25f;
                break;
            case 1:
                tint = new Color(0.3f, 0.0f, 0.2f);
                alpha = 0.4f;
                break;
            case 2:
                tint = new Color(0.4f, 0.0f, 0.05f);
                alpha = 0.55f;
                break;
        }
        CinematicOverlay.Instance.SetVignette(tint, alpha, 0.6f);
    }

    string GetRandomLine(PhaseData data)
    {
        if (data == null || data.dialogues == null || data.dialogues.Count == 0) return null;
        return data.dialogues[Random.Range(0, data.dialogues.Count)];
    }

    void EnterPhase(int index, bool animate)
    {
        if (index < 0 || index >= phases.Count) return;
        PhaseData data = phases[index];
        CurrentPhaseIndex = index;

        GameManager.Instance?.SetCurrentPhase(index);
        OnPhaseChanged?.Invoke(index);

        SetPhaseVignette(index);

        if (backgroundImage != null)
        {
            if (data.backgroundSprite != null)
                backgroundImage.sprite = data.backgroundSprite;

            if (animate)
                StartCoroutine(AnimateBackground(data.backgroundTint));
            else
                backgroundImage.color = data.backgroundTint;
        }

        SilhouetteDirector.Instance?.SetPhaseVisuals(index);

        if (wordSpawner != null)
        {
            wordSpawner.phaseSpeedMultiplier = data.speedMultiplier;
            wordSpawner.patternInterval = data.spawnInterval;
        }

        Debug.Log("Phase changed to " + (index + 1));
    }

    IEnumerator AnimateBackground(Color target)
    {
        if (backgroundImage == null) yield break;
        Color start = backgroundImage.color;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime;
            backgroundImage.color = Color.Lerp(start, target, t);
            yield return null;
        }
        backgroundImage.color = target;
    }
}
