using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = (T)this;
        OnInit();
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    protected virtual void OnInit() { }

    public static bool Exists() => Instance != null;

    public static T EnsureExists()
    {
        if (Instance != null) return Instance;
        T existing = FindObjectOfType<T>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }
        GameObject go = new GameObject(typeof(T).Name);
        return go.AddComponent<T>();
    }
}
