using UnityEngine;
using UnityEngine.Events;

public class HolyLightSystem : MonoBehaviour
{
    public static HolyLightSystem Instance { get; private set; }

    public static void EnsureExists()
    {
        if (Instance != null) return;
        if (FindObjectOfType<HolyLightSystem>() != null) return;
        GameObject go = new GameObject("HolyLightSystem");
        go.AddComponent<HolyLightSystem>();
    }

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
        HolyLight = config != null ? config.holyLightMax : 100f;
        OnHolyLightChanged?.Invoke(HolyLight);
    }

    void Update()
    {
        if (config == null) return;

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
            depletionTimer = config != null ? config.holyLightDepletionCooldown : 3f;
        }
        OnHolyLightChanged?.Invoke(HolyLight);
        return true;
    }
}
