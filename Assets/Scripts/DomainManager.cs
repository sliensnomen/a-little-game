using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class DomainManager : MonoBehaviour
{
    public static DomainManager Instance { get; private set; }

    public GameConfig config;
    public TrueNameSystem trueNameSystem;
    public WordSpawner wordSpawner;
    public InputManager inputManager;
    public GameManager gameManager;

    public UnityEvent OnDomainExpanded = new UnityEvent();

    private bool active = false;
    private Coroutine routine;

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
        if (trueNameSystem == null) trueNameSystem = FindObjectOfType<TrueNameSystem>();
        if (wordSpawner == null) wordSpawner = FindObjectOfType<WordSpawner>();
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (inputManager == null) inputManager = FindObjectOfType<InputManager>();

        if (inputManager != null)
            inputManager.OnDomainPressed.AddListener(TryActivate);
    }

    void OnDestroy()
    {
        if (inputManager != null)
            inputManager.OnDomainPressed.RemoveListener(TryActivate);
    }

    void TryActivate()
    {
        if (active) return;
        if (gameManager != null && gameManager.State != GameState.Combat) return;
        if (trueNameSystem == null || trueNameSystem.DomainCharge < 100f)
        {
            Debug.Log($"[Domain] 无法触发：charge={trueNameSystem?.DomainCharge}, state={gameManager?.State}");
            return;
        }

        Debug.Log("[Domain] 触发！");
        routine = StartCoroutine(DomainExpand());
    }

    IEnumerator DomainExpand()
    {
        active = true;

        if (gameManager != null) gameManager.EnterDomain();

        Time.timeScale = 0.1f;

        wordSpawner?.ClearAllWords();
        trueNameSystem?.RevealAllLettersTemporary(3f);
        trueNameSystem?.ExposePlayer(-config.domainExposureRelief);
        trueNameSystem?.ResetDomainCharge();
        OnDomainExpanded?.Invoke();
        DomainVisual.Instance?.Play();
        ParticleManager.Instance?.PlayDomain();
        AudioManager.Instance?.PlayDomain();

        yield return new WaitForSecondsRealtime(3f);

        Time.timeScale = 1f;
        active = false;

        if (gameManager != null && gameManager.State == GameState.Domain)
            gameManager.ChangeState(GameState.Combat);
    }
}
