using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ParticleManager : MonoSingleton<ParticleManager>
{
    public int prewarmCount = 200;
    public int maxActive = 500;
    public Vector2 particleSize = new Vector2(16f, 16f);

    private Canvas canvas;
    private Transform container;
    private Sprite circleSprite;
    private Queue<GameObject> pool = new Queue<GameObject>();
    private List<Particle> particles = new List<Particle>();

    struct Particle
    {
        public GameObject go;
        public RectTransform rt;
        public Image img;
        public Vector2 velocity;
        public float life;
        public float maxLife;
        public Color startColor;
    }

    void Start()
    {
        EnsureCanvas();
        EnsureSprite();
        EnsureContainer();
        Prewarm();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            Particle p = particles[i];
            p.life -= dt;
            if (p.life <= 0f)
            {
                Return(p);
                particles.RemoveAt(i);
                continue;
            }

            float t = 1f - Mathf.Clamp01(p.life / p.maxLife);
            Color c = p.startColor;
            c.a = p.startColor.a * (1f - t);
            p.img.color = c;

            p.rt.anchoredPosition += p.velocity * dt;
            particles[i] = p;
        }
    }

    public void PlayHit(Vector2 anchoredPosition, LanguageType lang)
    {
        Color color;
        switch (lang)
        {
            case LanguageType.Sacred:
                color = new Color(0.831f, 0.686f, 0.216f);
                break;
            case LanguageType.Demonic:
                color = new Color(0.58f, 0f, 0.827f);
                break;
            case LanguageType.Dual:
                color = new Color(0.706f, 0.343f, 0.522f);
                break;
            case LanguageType.Interference:
                color = new Color(0.333f, 0.333f, 0.333f);
                break;
            default:
                color = Color.white;
                break;
        }
        SpawnBurst(anchoredPosition, color, 15, 80f, 240f, 0.4f, 0.6f);
    }

    public void PlayMiss(Vector2 anchoredPosition, LanguageType lang)
    {
        Color color = new Color(0.5f, 0.25f, 0.25f);
        SpawnBurst(anchoredPosition, color, 8, 60f, 180f, 0.4f, 0.6f);
    }

    public void PlayDomain()
    {
        Color color = new Color(0.831f, 0.686f, 0.216f);
        SpawnBurst(Vector2.zero, color, 30, 120f, 360f, 0.8f, 1.0f);
    }

    public void PlayWin()
    {
        Color color = new Color(0.831f, 0.686f, 0.216f);
        SpawnBurst(Vector2.zero, color, 40, 150f, 450f, 1.0f, 1.2f);
    }

    public void PlayLose()
    {
        Color color = new Color(0.4f, 0.1f, 0.2f);
        SpawnBurst(Vector2.zero, color, 40, 150f, 450f, 1.0f, 1.2f);
    }

    void SpawnBurst(Vector2 anchoredPosition, Color color, int count, float speedMin, float speedMax, float lifeMin, float lifeMax)
    {
        count = Mathf.Min(count, maxActive);
        int room = maxActive - particles.Count;
        int toReuse = Mathf.Max(0, count - room);

        for (int i = 0; i < toReuse; i++)
        {
            if (particles.Count == 0) break;
            Return(particles[0]);
            particles.RemoveAt(0);
        }

        for (int i = 0; i < count; i++)
        {
            GameObject go = Get();
            Particle p = new Particle();
            p.go = go;
            p.rt = go.GetComponent<RectTransform>();
            p.img = go.GetComponent<Image>();
            p.rt.anchoredPosition = anchoredPosition;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(speedMin, speedMax);
            p.velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            p.maxLife = Random.Range(lifeMin, lifeMax);
            p.life = p.maxLife;
            p.startColor = color;
            p.img.color = color;
            p.go.SetActive(true);
            particles.Add(p);
        }
    }

    GameObject Get()
    {
        if (pool.Count > 0)
        {
            GameObject go = pool.Dequeue();
            go.SetActive(true);
            return go;
        }
        return CreateParticle();
    }

    void Return(Particle p)
    {
        p.go.SetActive(false);
        p.img.color = Color.clear;
        pool.Enqueue(p.go);
    }

    void EnsureCanvas()
    {
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject go = new GameObject("Canvas");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
        }
    }

    void EnsureSprite()
    {
        if (circleSprite != null) return;
        circleSprite = TextureGenerator.CreateCircleSprite(16, Color.white);
    }

    void EnsureContainer()
    {
        if (container != null) return;
        GameObject go = new GameObject("ParticleContainer");
        go.transform.SetParent(canvas.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        container = rt;
    }

    void Prewarm()
    {
        for (int i = 0; i < prewarmCount; i++)
        {
            GameObject go = CreateParticle();
            go.SetActive(false);
            pool.Enqueue(go);
        }
    }

    GameObject CreateParticle()
    {
        GameObject go = new GameObject("Particle", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(container, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = particleSize;
        Image img = go.GetComponent<Image>();
        img.sprite = circleSprite;
        img.color = Color.clear;
        img.raycastTarget = false;
        return go;
    }
}
