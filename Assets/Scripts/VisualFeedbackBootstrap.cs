using UnityEngine;
using UnityEngine.SceneManagement;

public class VisualFeedbackBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        CreateVisualFeedback();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CreateVisualFeedback();
    }

    static void CreateVisualFeedback()
    {
        if (GameObject.Find("VisualFeedback") != null) return;

        GameObject go = new GameObject("VisualFeedback");
        go.AddComponent<CameraShake>();
        go.AddComponent<WordHitEffect>();
        go.AddComponent<ExposureErosion>();
        go.AddComponent<DomainVisual>();
        go.AddComponent<SilhouetteDirector>();
        go.AddComponent<DialogueController>();
        go.AddComponent<ParallaxBackground>();
    }
}
