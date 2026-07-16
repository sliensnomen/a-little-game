using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum GameState { Intro, Combat, Domain, Win, Lose }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameConfig config;
    public string playerTrueName = "PLAYER";
    public string enemyTrueName = "MIRROR";
    public List<string> currentPhaseNames = new List<string>();

    public GameState State { get; private set; } = GameState.Intro;

    public UnityEvent<GameState> OnStateChanged = new UnityEvent<GameState>();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (string.IsNullOrWhiteSpace(playerTrueName))
        {
            string savedName = PlayerPrefs.GetString("PlayerTrueName", "");
            if (!string.IsNullOrWhiteSpace(savedName))
                playerTrueName = savedName;
        }

        GeneratePhaseNames(!string.IsNullOrWhiteSpace(playerTrueName) ? playerTrueName : "PLAYER");
    }

    void Start()
    {
        OnStateChanged?.Invoke(State);
    }

    public void GeneratePhaseNames(string playerName)
    {
        int maxLen = config != null ? config.maxTrueNameLength : 6;
        currentPhaseNames.Clear();

        if (config != null && config.phaseTrueNames != null && config.phaseTrueNames.Count >= 3)
        {
            for (int i = 0; i < 3; i++)
            {
                string name = (config.phaseTrueNames[i] ?? "").Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(name))
                    name = GetDefaultEnemyName();
                else if (name.Length > maxLen)
                    name = name.Substring(0, maxLen);
                currentPhaseNames.Add(name);
            }
        }
        else
        {
            string clean = GetCleanPlayerName(playerName);
            if (string.IsNullOrEmpty(clean))
            {
                for (int i = 0; i < 3; i++)
                    currentPhaseNames.Add(GetDefaultEnemyName());
            }
            else
            {
                currentPhaseNames.Add(BuildPhaseName(clean, 0, maxLen));
                currentPhaseNames.Add(BuildPhaseName(clean, 1, maxLen));
                currentPhaseNames.Add(BuildPhaseName(clean, 2, maxLen));
            }
        }

        enemyTrueName = currentPhaseNames.Count > 0 ? currentPhaseNames[0] : GetDefaultEnemyName();
    }

    string GetCleanPlayerName(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName)) return null;
        string clean = new string(playerName.Trim().ToUpperInvariant()
            .Where(c => (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')).ToArray());
        return string.IsNullOrEmpty(clean) ? null : clean;
    }

    string BuildPhaseName(string clean, int phase, int maxLen)
    {
        string reversed = new string(clean.ToCharArray().Reverse().ToArray());
        if (phase == 0)
            return PadName(reversed, maxLen);

        string shifted = new string(reversed.Select(c => ShiftChar(c, phase)).ToArray());
        return PadName(shifted, maxLen);
    }

    char ShiftChar(char c, int shift)
    {
        if (c >= 'A' && c <= 'Z')
            return (char)('A' + ((c - 'A' + shift) % 26));
        if (c >= '0' && c <= '9')
            return (char)('0' + ((c - '0' + shift) % 10));
        return c;
    }

    string PadName(string name, int maxLen)
    {
        if (name.Length > maxLen)
            name = name.Substring(0, maxLen);
        while (name.Length < 6) name += "R";
        return name;
    }

    public string GetEnemyNameForPhase(int phaseIndex)
    {
        if (currentPhaseNames == null || currentPhaseNames.Count == 0)
            GeneratePhaseNames(!string.IsNullOrWhiteSpace(playerTrueName) ? playerTrueName : "PLAYER");
        if (phaseIndex >= 0 && phaseIndex < currentPhaseNames.Count)
            return currentPhaseNames[phaseIndex];
        return GetDefaultEnemyName();
    }

    public void SetCurrentPhase(int phaseIndex)
    {
        if (currentPhaseNames == null || currentPhaseNames.Count == 0)
            GeneratePhaseNames(!string.IsNullOrWhiteSpace(playerTrueName) ? playerTrueName : "PLAYER");
        if (phaseIndex >= 0 && phaseIndex < currentPhaseNames.Count)
        {
            enemyTrueName = currentPhaseNames[phaseIndex];
            TrueNameSystem.Instance?.SetEnemyTrueName(enemyTrueName);
        }
    }

    string GetDefaultEnemyName()
    {
        return config != null && !string.IsNullOrEmpty(config.enemyTrueName)
            ? config.enemyTrueName.ToUpperInvariant()
            : "MIRROR";
    }

    public void SetPlayerName(string name)
    {
        int maxLen = config != null ? config.maxPlayerNameLength : 12;
        if (string.IsNullOrWhiteSpace(name))
        {
            playerTrueName = "PLAYER";
            GeneratePhaseNames(playerTrueName);
        }
        else if (name.Length > maxLen)
        {
            playerTrueName = name.Substring(0, maxLen);
            GeneratePhaseNames(playerTrueName);
        }
        else
        {
            playerTrueName = name;
            GeneratePhaseNames(playerTrueName);
        }
        PlayerPrefs.SetString("PlayerTrueName", playerTrueName);
    }

    public void ChangeState(GameState newState)
    {
        if (State == newState) return;
        State = newState;
        OnStateChanged?.Invoke(newState);
    }

    public void StartCombat()
    {
        var tns = TrueNameSystem.Instance ?? FindObjectOfType<TrueNameSystem>();
        if (tns == null)
        {
            Debug.LogError("TrueNameSystem not found in scene when StartCombat.");
            return;
        }
        tns.ResetValues();
        ChangeState(GameState.Combat);
    }
    public void EnterDomain() => ChangeState(GameState.Domain);
    public void ResumeCombat() => ChangeState(GameState.Combat);
    public void TriggerWin() => ChangeState(GameState.Win);
    public void TriggerLose() => ChangeState(GameState.Lose);

    public void RestartGame()
    {
        Time.timeScale = 1f;
        State = GameState.Intro;
        OnStateChanged.RemoveAllListeners();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
