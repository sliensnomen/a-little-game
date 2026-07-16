using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "TrueName/GameConfig")]
public class GameConfig : ScriptableObject
{
    public string enemyTrueName = "MIRROR";
    public List<string> phaseTrueNames = new List<string>();
    public bool autoAdvanceOnAllRevealed = true;
    public int maxTrueNameLength = 6;

    public float playerExposureMax = 100f;
    public float exposurePerMiss = 4f;
    public float exposurePerWrongHit = 8f;
    public float exposurePerWrongGuess = 25f;

    public float domainChargePerReveal = 8f;
    public float domainChargePerDual = 15f;
    public float domainChargePerECounter = 12f;
    public float domainChargePerExtraReveal = 12f;
    public float domainChargePerDualChant = 10f;
    public float domainExposureRelief = 10f;

    public float holyLightMax = 100f;
    public float holyLightRegenPerSecond = 5f;
    public float holyLightBlockCost = 20f;
    public float holyLightDepletionCooldown = 3f;

    public int exposureLayerMax = 3;
    public float exposureLayerRelief = 5f;
    public int exposureLayerExtraReveal = 1;
    public float guessTimeScale = 0.2f;

    public int comboThreshold = 3;
    public float dualWordWindow = 0.15f;
    public float eCounterWindow = 0.2f;
    public float dualChantHoldTime = 3f;
    public List<float> interferenceChances = new List<float> { 0.05f, 0.1f, 0.15f };
    public float baseWordSpeed = 220f;
    public float patternBWordSpeed = 300f;
    public float patternCWordSpeed = 130f;
    public float patternBSpawnInterval = 0.4f;
    public int patternCMinLength = 6;
    public int patternCMaxLength = 8;
    public int maxPlayerNameLength = 12;

    public float patternBFailureExposure = 8f;
    public float patternCFailureExposure = 15f;
    public float patternCSuccessReveal = 2f;
}
