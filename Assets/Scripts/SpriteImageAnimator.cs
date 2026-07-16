using UnityEngine;
using UnityEngine.UI;

public class SpriteImageAnimator : MonoBehaviour
{
    public Image image;
    public Sprite[] frames;
    public float fps = 10f;
    public bool loop = true;

    public Sprite[] attackFrames;
    public float attackFps = 12f;
    public Sprite[] hitFrames;
    public float hitFps = 12f;

    private float timer;
    private int index;
    private bool finished;

    private enum ClipMode { Idle, Attack, Hit }
    private ClipMode mode = ClipMode.Idle;
    private float clipTimer;
    private int clipIndex;

    void Start()
    {
        if (image == null) image = GetComponent<Image>();
        if (image == null)
        {
            Debug.LogError($"[{name}] SpriteImageAnimator: 找不到 Image 组件。", this);
            return;
        }
        if (frames == null || frames.Length == 0)
        {
            Debug.LogError($"[{name}] SpriteImageAnimator: frames 为空。请在 Inspector 中赋值，或运行 TrueName/Import Sprite Placeholders。", this);
            image.color = Color.magenta;
            return;
        }
        image.sprite = frames[0];
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;

        if (mode == ClipMode.Idle)
            UpdateIdle();
        else
            UpdateClip();
    }

    void UpdateIdle()
    {
        if (finished || fps <= 0) return;
        timer += Time.deltaTime;
        float interval = 1f / fps;
        while (timer >= interval)
        {
            timer -= interval;
            index++;
            if (index >= frames.Length)
            {
                if (loop)
                    index = 0;
                else
                {
                    index = frames.Length - 1;
                    finished = true;
                }
            }
            if (image != null)
                image.sprite = frames[index];
        }
    }

    void UpdateClip()
    {
        Sprite[] clip = mode == ClipMode.Attack ? attackFrames : hitFrames;
        float clipFps = mode == ClipMode.Attack ? attackFps : hitFps;
        if (clip == null || clip.Length == 0 || clipFps <= 0)
        {
            ReturnToIdle();
            return;
        }
        clipTimer += Time.deltaTime;
        float interval = 1f / clipFps;
        while (clipTimer >= interval)
        {
            clipTimer -= interval;
            clipIndex++;
            if (clipIndex >= clip.Length)
            {
                ReturnToIdle();
                return;
            }
            if (image != null)
                image.sprite = clip[clipIndex];
        }
    }

    void ReturnToIdle()
    {
        mode = ClipMode.Idle;
        clipTimer = 0f;
        clipIndex = 0;
        timer = 0f;
        index = 0;
        if (image != null && frames != null && frames.Length > 0)
            image.sprite = frames[0];
    }

    public void PlayAttack()
    {
        if (attackFrames == null || attackFrames.Length == 0) return;
        mode = ClipMode.Attack;
        clipTimer = 0f;
        clipIndex = 0;
        if (image != null)
            image.sprite = attackFrames[0];
    }

    public void PlayHit()
    {
        if (hitFrames == null || hitFrames.Length == 0) return;
        mode = ClipMode.Hit;
        clipTimer = 0f;
        clipIndex = 0;
        if (image != null)
            image.sprite = hitFrames[0];
    }

    public void Restart()
    {
        ReturnToIdle();
    }
}
