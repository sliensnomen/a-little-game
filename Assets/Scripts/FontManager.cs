using UnityEngine;

public class FontManager : MonoSingleton<FontManager>
{
    public Font englishFont;
    public Font demonicFont;
    public Font chineseFont;

    protected override void OnInit()
    {
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
