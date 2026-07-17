using System.Collections;
using UnityEngine;

public class CinematicCamera : MonoBehaviour
{
    public static CinematicCamera Instance { get; private set; }

    public Canvas canvas;
    public RectTransform worldFrame;
    public float driftAmount = 5f;
    public float driftSpeed = 0.2f;
    public float breathScaleAmount = 0.01f;
    public float breathSpeed = 0.5f;

    private Vector3 baseAnchoredPosition;
    private Vector3 baseScale;
    private bool frameInitialized;
    private Coroutine punchRoutine;
    private Coroutine zoomRoutine;
    private Coroutine slowMotionRoutine;

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
        InitFrame();
    }

    void Update()
    {
        if (!frameInitialized)
        {
            InitFrame();
            if (!frameInitialized) return;
        }
        TryReparentWorldObjects();
        ApplyDrift();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static CinematicCamera EnsureExists()
    {
        if (Instance != null) return Instance;
        var existing = FindObjectOfType<CinematicCamera>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }
        GameObject go = new GameObject("CinematicCamera");
        return go.AddComponent<CinematicCamera>();
    }

    void InitFrame()
    {
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        if (worldFrame == null)
        {
            GameObject go = new GameObject("CinematicFrame", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            worldFrame = go.GetComponent<RectTransform>();
            worldFrame.anchorMin = Vector2.zero;
            worldFrame.anchorMax = Vector2.one;
            worldFrame.pivot = new Vector2(0.5f, 0.5f);
            worldFrame.anchoredPosition = Vector2.zero;
            worldFrame.sizeDelta = Vector2.zero;
            worldFrame.localScale = Vector3.one;
            worldFrame.SetAsFirstSibling();
        }

        baseAnchoredPosition = worldFrame.anchoredPosition3D;
        baseScale = worldFrame.localScale;

        TryReparentWorldObjects();

        frameInitialized = true;
    }

    void TryReparentWorldObjects()
    {
        if (worldFrame == null) return;

        int index = 0;
        if (TryReparentWorld("Background", index)) index++;
        if (TryReparentWorld("ParallaxBackground", index)) index++;
        if (TryReparentWorld("WitchKingPlaceholder", index)) index++;
        if (TryReparentWorld("MirrorPlaceholder", index)) index++;
        if (TryReparentWorld("DefenseLine", index)) index++;

        var spawner = WordSpawner.Instance ?? FindObjectOfType<WordSpawner>();
        if (spawner != null && spawner.wordContainer != null)
            Reparent(spawner.wordContainer, index);
    }

    bool TryReparentWorld(string name, int index)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) return false;
        Reparent(go.GetComponent<RectTransform>(), index);
        return true;
    }

    void Reparent(RectTransform rt, int index)
    {
        if (rt == null || worldFrame == null) return;
        if (rt.parent != worldFrame)
            rt.SetParent(worldFrame, false);
        rt.SetSiblingIndex(index);
    }

    void ApplyDrift()
    {
        if (worldFrame == null) return;
        float t = Time.unscaledTime;
        float dx = Mathf.Sin(t * driftSpeed) * driftAmount;
        float dy = Mathf.Cos(t * driftSpeed * 0.7f) * driftAmount;
        float scale = 1f + Mathf.Sin(t * breathSpeed) * breathScaleAmount;
        worldFrame.anchoredPosition3D = baseAnchoredPosition + new Vector3(dx, dy, 0f);
        worldFrame.localScale = baseScale * scale;
    }

    public void Punch(Vector2 direction, float amount, float duration)
    {
        if (worldFrame == null) return;
        if (punchRoutine != null) StopCoroutine(punchRoutine);
        punchRoutine = StartCoroutine(PunchRoutine(direction.normalized * amount, duration));
    }

    IEnumerator PunchRoutine(Vector2 offset, float duration)
    {
        Vector3 start = worldFrame.anchoredPosition3D;
        Vector3 end = start + new Vector3(offset.x, offset.y, 0f);
        float half = duration * 0.5f;
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = half > 0f ? elapsed / half : 1f;
            worldFrame.anchoredPosition3D = Vector3.Lerp(start, end, t);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = half > 0f ? elapsed / half : 1f;
            worldFrame.anchoredPosition3D = Vector3.Lerp(end, start, t);
            yield return null;
        }
        worldFrame.anchoredPosition3D = start;
    }

    public void Zoom(float scale, float duration)
    {
        if (worldFrame == null) return;
        if (zoomRoutine != null) StopCoroutine(zoomRoutine);
        zoomRoutine = StartCoroutine(ZoomRoutine(scale, duration));
    }

    IEnumerator ZoomRoutine(float targetScale, float duration)
    {
        Vector3 start = worldFrame.localScale;
        Vector3 end = baseScale * targetScale;
        float half = duration * 0.5f;
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = half > 0f ? elapsed / half : 1f;
            worldFrame.localScale = Vector3.Lerp(start, end, t);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = half > 0f ? elapsed / half : 1f;
            worldFrame.localScale = Vector3.Lerp(end, start, t);
            yield return null;
        }
        worldFrame.localScale = start;
    }

    public void SlowMotion(float targetTimeScale, float duration)
    {
        if (slowMotionRoutine != null) StopCoroutine(slowMotionRoutine);
        slowMotionRoutine = StartCoroutine(SlowMotionRoutine(targetTimeScale, duration));
    }

    IEnumerator SlowMotionRoutine(float targetTimeScale, float duration)
    {
        float original = Time.timeScale;
        Time.timeScale = targetTimeScale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = original;
    }
}
