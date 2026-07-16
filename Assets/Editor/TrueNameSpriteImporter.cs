using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor.U2D.Sprites;

public class TrueNameSpriteImporter
{
    const string ProtagonistSource = "/Users/alex/Downloads/EVil Wizard 2/Sprites/Idle.png";
    const string EnemySource = "/Users/alex/Downloads/Evil Wizard/Sprites/Idle.png";

    const string ProtagonistAttackSource = "/Users/alex/Downloads/EVil Wizard 2/Sprites/Attack1.png";
    const string EnemyAttackSource = "/Users/alex/Downloads/Evil Wizard/Sprites/Attack.png";

    const string ProtagonistHitSource = "/Users/alex/Downloads/EVil Wizard 2/Sprites/Take hit.png";
    const string EnemyHitSource = "/Users/alex/Downloads/Evil Wizard/Sprites/Take Hit.png";

    const string ProtagonistTarget = "Assets/Sprites/WitchKing_Idle.png";
    const string EnemyTarget = "Assets/Sprites/Mirror_Idle.png";

    const string ProtagonistAttackTarget = "Assets/Sprites/WitchKing_Attack.png";
    const string EnemyAttackTarget = "Assets/Sprites/Mirror_Attack.png";

    const string ProtagonistHitTarget = "Assets/Sprites/WitchKing_Hit.png";
    const string EnemyHitTarget = "Assets/Sprites/Mirror_Hit.png";

    const float PlaceholderSize = 500f;
    static readonly Color BackgroundColor = new Color(0.05f, 0.04f, 0.07f, 1f);

    [MenuItem("TrueName/Import Sprite Placeholders")]
    static void Import()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("需要先退出 Play 模式", "请在 Unity 里按停止按钮退出 Play 模式，再运行 TrueName/Import Sprite Placeholders。", "OK");
            return;
        }

        string[] sources = { ProtagonistSource, EnemySource, ProtagonistAttackSource, EnemyAttackSource, ProtagonistHitSource, EnemyHitSource };
        if (sources.Any(s => !File.Exists(s)))
        {
            EditorUtility.DisplayDialog("找不到源文件", $"请确认:\n{string.Join("\n", sources)}", "OK");
            return;
        }

        EnsureFolder("Assets/Sprites");

        File.Copy(ProtagonistSource, ProtagonistTarget, true);
        File.Copy(EnemySource, EnemyTarget, true);
        File.Copy(ProtagonistAttackSource, ProtagonistAttackTarget, true);
        File.Copy(EnemyAttackSource, EnemyAttackTarget, true);
        File.Copy(ProtagonistHitSource, ProtagonistHitTarget, true);
        File.Copy(EnemyHitSource, EnemyHitTarget, true);
        AssetDatabase.Refresh();

        Sprite[] protagonistSprites = ImportAndSlice(ProtagonistTarget, "WitchKing_Idle");
        Sprite[] enemySprites = ImportAndSlice(EnemyTarget, "Mirror_Idle");
        Sprite[] protagonistAttackSprites = ImportAndSlice(ProtagonistAttackTarget, "WitchKing_Attack");
        Sprite[] enemyAttackSprites = ImportAndSlice(EnemyAttackTarget, "Mirror_Attack");
        Sprite[] protagonistHitSprites = ImportAndSlice(ProtagonistHitTarget, "WitchKing_Hit");
        Sprite[] enemyHitSprites = ImportAndSlice(EnemyHitTarget, "Mirror_Hit");

        if (protagonistSprites.Length == 0 || enemySprites.Length == 0)
        {
            EditorUtility.DisplayDialog("切片失败", "Idle Sprite 表切片没有产生子精灵，请检查原图尺寸。", "OK");
            return;
        }

        AssignToPlaceholder("WitchKingPlaceholder", protagonistSprites, protagonistAttackSprites, protagonistHitSprites, false);
        AssignToPlaceholder("MirrorPlaceholder", enemySprites, enemyAttackSprites, enemyHitSprites, true);
        ApplyBackgroundColor();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("完成", "主角与敌人占位符已替换为 Idle 循环动画。", "OK");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }

    static Sprite[] ImportAndSlice(string targetPath, string prefix)
    {
        TextureImporter importer = AssetImporter.GetAtPath(targetPath) as TextureImporter;
        if (importer == null) return new Sprite[0];

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.filterMode = FilterMode.Point;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(targetPath);
        if (tex == null)
        {
            Debug.LogError($"无法加载纹理: {targetPath}");
            return new Sprite[0];
        }

        int frameSize = tex.height;
        int frameCount = tex.width / frameSize;
        if (frameCount <= 0)
        {
            Debug.LogError($"无法计算帧数: {targetPath} 尺寸 {tex.width}x{tex.height}");
            return new Sprite[0];
        }
        Debug.Log($"{targetPath} 检测到 {frameCount} 帧，每帧 {frameSize}x{frameSize}");

        importer.spriteImportMode = SpriteImportMode.Multiple;
        AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);

        SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (provider == null)
        {
            Debug.LogError($"无法从 {targetPath} 获取 ISpriteEditorDataProvider");
            return new Sprite[0];
        }
        provider.InitSpriteEditorDataProvider();

        SpriteRect[] rects = new SpriteRect[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            rects[i] = new SpriteRect
            {
                name = $"{prefix}_{i}",
                rect = new Rect(i * frameSize, 0, frameSize, frameSize),
                pivot = new Vector2(0.5f, 0.5f),
                alignment = SpriteAlignment.Custom,
                border = Vector4.zero,
                spriteID = GUID.Generate()
            };
        }
        provider.SetSpriteRects(rects);
        provider.Apply();

        AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);

        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(targetPath)
            .OfType<Sprite>()
            .OrderBy(s => s.rect.x)
            .ToArray();

        Debug.Log($"{targetPath} 切片完成，共 {sprites.Length} 个 Sprite");
        return sprites;
    }

    static void AssignToPlaceholder(string name, Sprite[] idleFrames, Sprite[] attackFrames, Sprite[] hitFrames, bool flipX)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            Debug.LogWarning($"找不到占位符: {name}");
            return;
        }

        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.sprite = idleFrames[0];
        img.color = Color.white;
        img.preserveAspect = true;
        img.type = Image.Type.Simple;

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(PlaceholderSize, PlaceholderSize);
            if (flipX)
                rt.localScale = new Vector3(-1, 1, 1);
            else
                rt.localScale = Vector3.one;
        }

        SpriteImageAnimator anim = go.GetComponent<SpriteImageAnimator>();
        if (anim == null) anim = go.AddComponent<SpriteImageAnimator>();
        anim.image = img;
        anim.frames = idleFrames;
        anim.attackFrames = attackFrames;
        anim.hitFrames = hitFrames;
        anim.fps = 10f;
        anim.loop = true;

        SilhouetteAnimator sa = go.GetComponent<SilhouetteAnimator>();
        if (sa == null) sa = go.AddComponent<SilhouetteAnimator>();
        sa.target = rt;
        sa.silhouetteImage = img;
        sa.normalColor = Color.white;

        Transform label = go.transform.Find("Label");
        if (label != null) label.gameObject.SetActive(false);

        EditorUtility.SetDirty(go);
    }

    static void ApplyBackgroundColor()
    {
        GameObject bg = GameObject.Find("Background");
        if (bg == null) return;
        Image img = bg.GetComponent<Image>();
        if (img != null)
        {
            img.color = BackgroundColor;
            EditorUtility.SetDirty(img);
        }
    }
}
