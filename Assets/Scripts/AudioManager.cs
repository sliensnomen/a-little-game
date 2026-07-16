using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public static void EnsureExists()
    {
        if (Instance != null) return;
        if (FindObjectOfType<AudioManager>() != null) return;
        GameObject go = new GameObject("AudioManager");
        go.AddComponent<AudioManager>();
    }

    public AudioClip hitClip;
    public AudioClip missClip;
    public AudioClip domainClip;
    public AudioClip typewriterClip;
    public AudioClip shatterClip;

    private AudioSource source;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
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
