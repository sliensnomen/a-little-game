using UnityEngine;
using UnityEngine.Events;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public UnityEvent OnQPressed = new UnityEvent();
    public UnityEvent OnEPressed = new UnityEvent();
    public UnityEvent OnGuessPressed = new UnityEvent();
    public UnityEvent OnDomainPressed = new UnityEvent();
    public UnityEvent OnDualChantStarted = new UnityEvent();
    public UnityEvent OnDualChantCompleted = new UnityEvent();

    public float LastHolyTime { get; private set; }
    public float LastDarkTime { get; private set; }
    public bool IsDualChanting { get; private set; }

    private bool qHeld;
    private bool eHeld;
    private float dualChantTimer;
    private GameManager gameManager;
    private GameConfig config;

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
        config = GameManager.Instance?.config;
    }

    void Update()
    {
        GameState state = gameManager != null ? gameManager.State : GameState.Intro;
        if (state != GameState.Combat && state != GameState.Domain)
        {
            return;
        }

        bool guessOpen = UIManager.Instance != null && UIManager.Instance.IsGuessPanelOpen;

        if (!guessOpen && Input.GetKeyDown(KeyCode.Q)) { LastHolyTime = Time.time; OnQPressed?.Invoke(); }
        if (!guessOpen && Input.GetKeyDown(KeyCode.E)) { LastDarkTime = Time.time; OnEPressed?.Invoke(); }
        if (!guessOpen && Input.GetKeyDown(KeyCode.Space)) { OnDomainPressed?.Invoke(); }
        if (Input.GetKeyDown(KeyCode.Return)) { OnGuessPressed?.Invoke(); }

        qHeld = Input.GetKey(KeyCode.Q);
        eHeld = Input.GetKey(KeyCode.E);

        if (qHeld && eHeld)
        {
            if (!IsDualChanting)
            {
                IsDualChanting = true;
                dualChantTimer = 0f;
                OnDualChantStarted?.Invoke();
            }
            else
            {
                dualChantTimer += Time.deltaTime;
                float holdTime = config != null ? config.dualChantHoldTime : 3f;
                if (dualChantTimer >= holdTime)
                {
                    OnDualChantCompleted?.Invoke();
                    IsDualChanting = false;
                    dualChantTimer = 0f;
                }
            }
        }
        else if (IsDualChanting)
        {
            IsDualChanting = false;
            dualChantTimer = 0f;
        }
    }
}
