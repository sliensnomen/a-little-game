using System;
using System.Collections;
using UnityEngine;

public class AudioManager : MonoSingleton<AudioManager>
{
    public AudioClip hitClip;
    public AudioClip missClip;
    public AudioClip domainClip;
    public AudioClip typewriterClip;
    public AudioClip shatterClip;

    [Header("BGM")]
    public AudioClip bgmClip;
    public float bgmVolume = 0.7f;
    public float bgmFadeDuration = 0.8f;
    public bool bgmLoop = true;

    private AudioSource source;
    private AudioSource bgmSource;
    private Coroutine bgmFadeRoutine;

    protected override void OnInit()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.volume = 0f;
    }

    public void PlayHit()
    {
        if (hitClip == null) hitClip = GenerateHitClip();
        if (source != null) source.PlayOneShot(hitClip);
    }

    public void PlayMiss()
    {
        if (missClip == null) missClip = GenerateMissClip();
        if (source != null) source.PlayOneShot(missClip);
    }

    public void PlayDomain()
    {
        if (domainClip == null) domainClip = GenerateDomainClip();
        if (source != null) source.PlayOneShot(domainClip);
    }

    public void PlayTypewriter()
    {
        if (typewriterClip == null) typewriterClip = GenerateTypewriterClip();
        if (source != null) source.PlayOneShot(typewriterClip);
    }

    public void PlayShatter()
    {
        if (shatterClip == null) shatterClip = GenerateShatterClip();
        if (source != null) source.PlayOneShot(shatterClip);
    }

    public void PlayBGM(AudioClip clip = null, bool fade = true)
    {
        if (clip == null) clip = GetDefaultBGM();
        if (clip == null) return;

        float targetVolume = GetBGMVolume();

        if (bgmSource.isPlaying && bgmSource.clip == clip)
        {
            if (fade) FadeTo(targetVolume, bgmFadeDuration);
            else bgmSource.volume = targetVolume;
            return;
        }

        if (fade)
        {
            if (bgmFadeRoutine != null) StopCoroutine(bgmFadeRoutine);
            bgmFadeRoutine = StartCoroutine(FadeSwitchBGM(clip, targetVolume, bgmFadeDuration));
        }
        else
        {
            bgmSource.Stop();
            bgmSource.clip = clip;
            bgmSource.loop = bgmLoop;
            bgmSource.volume = targetVolume;
            bgmSource.Play();
        }
    }

    public void StopBGM(bool fade = true)
    {
        if (bgmSource == null || !bgmSource.isPlaying) return;

        if (fade)
        {
            if (bgmFadeRoutine != null) StopCoroutine(bgmFadeRoutine);
            bgmFadeRoutine = StartCoroutine(FadeToAndStop(bgmFadeDuration));
        }
        else
        {
            bgmSource.Stop();
            bgmSource.volume = 0f;
        }
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.volume = bgmVolume;
    }

    AudioClip GetDefaultBGM()
    {
        if (bgmClip != null) return bgmClip;
        bgmClip = Resources.Load<AudioClip>("Audio/BGM/dark-mantras");
        return bgmClip;
    }

    float GetBGMVolume()
    {
        var config = GameManager.Instance?.config;
        if (config != null) return Mathf.Clamp01(config.bgmVolume);
        return Mathf.Clamp01(bgmVolume);
    }

    void FadeTo(float target, float duration)
    {
        if (bgmFadeRoutine != null) StopCoroutine(bgmFadeRoutine);
        bgmFadeRoutine = StartCoroutine(FadeVolumeRoutine(bgmSource, bgmSource.volume, target, duration));
    }

    IEnumerator FadeVolumeRoutine(AudioSource audioSource, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration > 0f ? elapsed / duration : 1f;
            audioSource.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }
        audioSource.volume = to;
    }

    IEnumerator FadeSwitchBGM(AudioClip clip, float targetVolume, float duration)
    {
        if (bgmSource.isPlaying)
        {
            float start = bgmSource.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration > 0f ? elapsed / duration : 1f;
                bgmSource.volume = Mathf.Lerp(start, 0f, t);
                yield return null;
            }
            bgmSource.Stop();
        }

        bgmSource.clip = clip;
        bgmSource.loop = bgmLoop;
        bgmSource.volume = 0f;
        bgmSource.Play();

        float e = 0f;
        while (e < duration)
        {
            e += Time.unscaledDeltaTime;
            float t = duration > 0f ? e / duration : 1f;
            bgmSource.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }
        bgmSource.volume = targetVolume;
    }

    IEnumerator FadeToAndStop(float duration)
    {
        float start = bgmSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration > 0f ? elapsed / duration : 1f;
            bgmSource.volume = Mathf.Lerp(start, 0f, t);
            yield return null;
        }
        bgmSource.Stop();
        bgmSource.volume = 0f;
    }

    AudioClip GenerateHitClip()
    {
        return GenerateClip(i =>
        {
            float t = (float)i / 44100f;
            float envelope = Mathf.Exp(-15f * t);
            return Mathf.Sin(2f * Mathf.PI * 880f * t) * envelope;
        }, 0.1f);
    }

    AudioClip GenerateMissClip()
    {
        return GenerateClip(i =>
        {
            float t = (float)i / 44100f;
            float envelope = Mathf.Exp(-8f * t);
            return Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 120f * t)) * envelope;
        }, 0.2f);
    }

    AudioClip GenerateDomainClip()
    {
        return GenerateClip(i =>
        {
            float t = (float)i / 44100f;
            float envelope = Mathf.Exp(-3f * t);
            float phase = 2f * Mathf.PI * (400f * t - 200f * t * t);
            return Mathf.Sin(phase) * envelope;
        }, 0.5f);
    }

    AudioClip GenerateTypewriterClip()
    {
        return GenerateClip(i =>
        {
            float t = (float)i / 44100f;
            float envelope = Mathf.Exp(-40f * t);
            return UnityEngine.Random.Range(-1f, 1f) * envelope;
        }, 0.05f);
    }

    AudioClip GenerateShatterClip()
    {
        return GenerateClip(i =>
        {
            float t = (float)i / 44100f;
            float envelope = Mathf.Exp(-8f * t);
            return UnityEngine.Random.Range(-1f, 1f) * envelope;
        }, 0.4f);
    }

    AudioClip GenerateClip(Func<int, float> sampleFn, float duration, int frequency = 44100)
    {
        int samples = Mathf.RoundToInt(duration * frequency);
        AudioClip clip = AudioClip.Create("Procedural", samples, 1, frequency, false);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
            data[i] = Mathf.Clamp(sampleFn(i), -1f, 1f);
        clip.SetData(data, 0);
        return clip;
    }
}
