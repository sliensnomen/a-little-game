using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    public RectTransform target;
    public float shakeAmount = 8f;
    public float shakeDuration = 0.1f;

    private Vector3 basePosition;
    private float remaining;
    private float magnitude;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (target == null)
            target = FindObjectOfType<Canvas>()?.GetComponent<RectTransform>();
        if (target != null)
            basePosition = target.anchoredPosition3D;
    }

    void Update()
    {
        if (target == null || remaining <= 0) return;

        Vector3 shake = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * magnitude;
        target.anchoredPosition3D = basePosition + shake;
        remaining -= Time.deltaTime;
        if (remaining <= 0)
            target.anchoredPosition3D = basePosition;
    }

    public void Shake(float magnitude, float duration)
    {
        if (target == null)
            target = FindObjectOfType<Canvas>()?.GetComponent<RectTransform>();
        if (target == null) return;
        basePosition = target.anchoredPosition3D;
        this.magnitude = magnitude;
        remaining = duration;
    }
}
