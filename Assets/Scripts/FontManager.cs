using UnityEngine;

public class FontManager : MonoBehaviour
{
    public static FontManager Instance { get; private set; }

    public Font englishFont;
    public Font demonicFont;
    public Font chineseFont;

    public static void EnsureExists()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("FontManager");
        go.AddComponent<FontManager>();
    }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadFonts();
    }

    void LoadFonts()
    {
        englishFont = Resources.Load<Font>("Fonts/Cinzel");
        demonicFont = Resources.Load<Font>("Fonts/UnifrakturMaguntia");
        chineseFont = Resources.Load<Font>("Fonts/ZCOOLXiaoWei");
    }

    public Font GetFont(LanguageType lang)
    {
        if (lang == LanguageType.Demonic) return demonicFont;
        return englishFont;
    }

    public Font GetUIFont()
    {
        return chineseFont;
    }
}
