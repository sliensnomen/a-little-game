using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PhaseData", menuName = "TrueName/PhaseData")]
public class PhaseData : ScriptableObject
{
    public int letterThreshold = 0;
    public float speedMultiplier = 1f;
    public float spawnInterval = 1f;
    public Color backgroundTint = new Color(0.05f, 0.04f, 0.07f, 0f);
    public Sprite backgroundSprite;
    public Color eyeGlowColor = new Color(1, 0.2f, 0.2f, 1f);
    public float crackAlpha = 0f;
    public List<string> dialogues = new List<string>();
}
