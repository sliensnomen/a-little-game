using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class TrueNameSetupWizard
{
    [MenuItem("TrueName/Setup Project")]
    static void SetupProject()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("需要先退出 Play 模式", "请先在 Unity 里按停止按钮退出 Play 模式，再运行 TrueName/Setup Project。", "OK");
            return;
        }

        EnsureFolder("Assets/Scripts");
        EnsureFolder("Assets/ScriptableObjects");
        EnsureFolder("Assets/Editor");
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Scenes");

        GameConfig config = EnsureAsset<GameConfig>("Assets/ScriptableObjects/GameConfig.asset");
        WordDatabase wordDb = EnsureAsset<WordDatabase>("Assets/ScriptableObjects/WordDatabase.asset");
        PhaseData phase1 = EnsureAsset<PhaseData>("Assets/ScriptableObjects/Phase_1.asset");
        PhaseData phase2 = EnsureAsset<PhaseData>("Assets/ScriptableObjects/Phase_2.asset");
        PhaseData phase3 = EnsureAsset<PhaseData>("Assets/ScriptableObjects/Phase_3.asset");

        SetDefaultValues(config, wordDb, phase1, phase2, phase3);

        AssetDatabase.SaveAssets();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        SetupCamera();
        GameObject canvas = CreateCanvas();
        GameObject background = CreateBackground(canvas);
        CreateSilhouettes(canvas);
        RectTransform wordContainer = CreateWordContainer(canvas);
        GameObject wordPrefab = CreateWordPrefab();
        CreateEventSystem();
        GameObject managers = CreateManagers(config, wordPrefab, wordContainer, wordDb, phase1, phase2, phase3, background);
        CreateUI(canvas, managers);

        string scenePath = "Assets/Scenes/MainScene.scene";
        EditorSceneManager.SaveScene(scene, scenePath);

        Debug.Log("TrueName project setup complete. Scene saved at " + scenePath);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }
        AssetDatabase.CreateFolder(parent, folder);
    }

    static T EnsureAsset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static void SetDefaultValues(GameConfig config, WordDatabase db, PhaseData p1, PhaseData p2, PhaseData p3)
    {
        config.enemyTrueName = "MIRROR";
        config.maxTrueNameLength = 6;
        config.playerExposureMax = 100f;
        config.exposurePerMiss = 4f;
        config.exposurePerWrongHit = 8f;
        config.exposurePerWrongGuess = 25f;
        config.domainChargePerReveal = 8f;
        config.domainChargePerDual = 15f;
        config.domainExposureRelief = 10f;
        config.holyLightMax = 100f;
        config.holyLightRegenPerSecond = 5f;
        config.holyLightBlockCost = 20f;
        config.holyLightDepletionCooldown = 3f;
        config.exposureLayerMax = 3;
        config.exposureLayerRelief = 5f;
        config.exposureLayerExtraReveal = 1;
        config.guessTimeScale = 0.2f;
        config.comboThreshold = 3;
        config.dualWordWindow = 0.15f;
        config.baseWordSpeed = 120f;
        config.maxPlayerNameLength = 12;
        config.patternBFailureExposure = 8f;
        config.patternCFailureExposure = 15f;
        config.patternCSuccessReveal = 2f;
        EditorUtility.SetDirty(config);

        if (db.holyWords.Count == 0 && db.darkWords.Count == 0 && db.dualWords.Count == 0)
        {
            db.holyWords.AddRange(new[] { "Luce", "Verita", "Fede", "Lux", "Sanctus", "Aeterna", "Light" });
            db.darkWords.AddRange(new[] { "Nacht", "Dunkel", "Krieg", "Tod", "Schmerz", "Blut" });
            db.dualWords.AddRange(new[] { "LuceNacht", "VeritasKrieg", "SanctusTod" });
            EditorUtility.SetDirty(db);
        }

        if (p1.letterThreshold == 0 && p2.letterThreshold == 0 && p3.letterThreshold == 0)
        {
            p1.letterThreshold = 0;
            p1.speedMultiplier = 1f;
            p1.spawnInterval = 4f;
            p1.backgroundTint = new Color(0.05f, 0.04f, 0.07f, 1f);
            p1.dialogues.Add("我不介意在这里剥开你的真名。反正……被剥开的东西已经够多了。");
            EditorUtility.SetDirty(p1);

            p2.letterThreshold = 3;
            p2.speedMultiplier = 1.5f;
            p2.spawnInterval = 4f;
            p2.backgroundTint = new Color(0.04f, 0.03f, 0.06f, 1f);
            p2.dialogues.Add("你念圣文的速度，和那个老疯子一样。");
            p2.dialogues.Add("……别拿我和辛美尔比。他可不会剥你的真名。");
            EditorUtility.SetDirty(p2);

            p3.letterThreshold = 5;
            p3.speedMultiplier = 2f;
            p3.spawnInterval = 4f;
            p3.backgroundTint = new Color(0.03f, 0.02f, 0.04f, 1f);
            p3.dialogues.Add("你的领域能撑几秒？三秒？还是两秒？");
            p3.dialogues.Add("够你死一次了。别废话，继续。");
            EditorUtility.SetDirty(p3);
        }
    }

    static void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camObj.tag = "MainCamera";
            cam = camObj.GetComponent<Camera>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 5.4f;
        cam.backgroundColor = new Color(0.05f, 0.04f, 0.07f, 0f);
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    static GameObject CreateCanvas()
    {
        GameObject canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvasObj;
    }

    static GameObject CreateBackground(GameObject canvas)
    {
        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(canvas.transform, false);
        RectTransform rt = bg.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = bg.GetComponent<Image>();
        img.color = new Color(0.05f, 0.04f, 0.07f, 1f);
        return bg;
    }

    static void CreateEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    static GameObject CreateManagers(GameConfig config, GameObject wordPrefab, RectTransform wordContainer, WordDatabase wordDb, PhaseData phase1, PhaseData phase2, PhaseData phase3, GameObject background)
    {
        GameObject managers = new GameObject("Managers");
        GameManager gm = managers.AddComponent<GameManager>();
        gm.config = config;

        TrueNameSystem tns = managers.AddComponent<TrueNameSystem>();
        HolyLightSystem hls = managers.AddComponent<HolyLightSystem>();
        hls.config = config;
        InputManager im = managers.AddComponent<InputManager>();
        UIManager ui = managers.AddComponent<UIManager>();
        WordSpawner spawner = managers.AddComponent<WordSpawner>();
        spawner.wordPrefab = wordPrefab;
        spawner.wordContainer = wordContainer;
        spawner.config = config;
        spawner.wordDatabase = wordDb;
        spawner.trueNameSystem = tns;
        spawner.inputManager = im;

        PhaseManager pm = managers.AddComponent<PhaseManager>();
        pm.phases = new System.Collections.Generic.List<PhaseData> { phase1, phase2, phase3 };
        pm.gameManager = gm;
        pm.trueNameSystem = tns;
        pm.wordSpawner = spawner;
        if (background != null) pm.backgroundImage = background.GetComponent<Image>();

        DomainManager dm = managers.AddComponent<DomainManager>();
        dm.config = config;
        dm.trueNameSystem = tns;
        dm.wordSpawner = spawner;
        dm.inputManager = im;
        dm.gameManager = gm;

        return managers;
    }

    static void CreateUI(GameObject canvas, GameObject managers)
    {
        UIManager ui = managers.GetComponent<UIManager>();

        GameObject startPanel = CreatePanel("StartPanel", canvas.transform, new Color(0.05f, 0.05f, 0.07f, 1f));
        CreateText("Title", startPanel.transform, "TrueName Unveiled", 60, Color.white, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(600, 80));
        InputField nameInput = CreateInputField("NameInput", startPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(400, 60));
        Button startButton = CreateButton("StartButton", startPanel.transform, "开始", new Vector2(0.5f, 0.35f), new Vector2(200, 60));

        GameObject hudPanel = CreatePanel("HUDPanel", canvas.transform, new Color(0, 0, 0, 0));
        Text comboText = CreateText("ComboText", hudPanel.transform, "连击: 0", 28, Color.white, new Vector2(0.95f, 0.9f), new Vector2(0.95f, 0.9f), Vector2.zero, new Vector2(200, 40));
        Text exposureText = CreateText("ExposureText", hudPanel.transform, "暴露度: 0%", 28, Color.white, new Vector2(0.05f, 0.9f), new Vector2(0.05f, 0.9f), Vector2.zero, new Vector2(300, 40));
        Text domainText = CreateText("DomainText", hudPanel.transform, "领域: 0%", 28, Color.white, new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f), Vector2.zero, new Vector2(300, 40));

        GameObject debugPanel = CreatePanel("DebugPanel", hudPanel.transform, new Color(0, 0, 0, 0.3f));
        RectTransform debugRect = debugPanel.GetComponent<RectTransform>();
        debugRect.anchorMin = new Vector2(0.75f, 0.2f);
        debugRect.anchorMax = new Vector2(1f, 0.6f);
        debugRect.offsetMin = Vector2.zero;
        debugRect.offsetMax = Vector2.zero;

        Button hitButton = CreateButton("SimulateHit", debugPanel.transform, "击中", new Vector2(0.5f, 0.75f), new Vector2(160, 40));
        Button missButton = CreateButton("SimulateMiss", debugPanel.transform, "Miss", new Vector2(0.5f, 0.5f), new Vector2(160, 40));
        Button winButton = CreateButton("SimulateWin", debugPanel.transform, "胜利", new Vector2(0.5f, 0.25f), new Vector2(160, 40));
        Button loseButton = CreateButton("SimulateLose", debugPanel.transform, "失败", new Vector2(0.5f, 0f), new Vector2(160, 40));

        GameObject winPanel = CreatePanel("WinPanel", canvas.transform, new Color(0.05f, 0.05f, 0.07f, 1f));
        CreateText("WinText", winPanel.transform, "胜利", 80, Color.white, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(400, 120));
        Text winNameText = CreateText("WinNameText", winPanel.transform, "无名者", 32, Color.white, new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f), Vector2.zero, new Vector2(600, 60));

        GameObject losePanel = CreatePanel("LosePanel", canvas.transform, new Color(0.05f, 0.05f, 0.07f, 1f));
        CreateText("LoseText", losePanel.transform, "失败", 80, Color.white, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(400, 120));
        Text loseNameText = CreateText("LoseNameText", losePanel.transform, "无名者", 40, Color.white, new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f), Vector2.zero, new Vector2(600, 60));

        ui.startPanel = startPanel;
        ui.hudPanel = hudPanel;
        ui.winPanel = winPanel;
        ui.losePanel = losePanel;
        ui.nameInput = nameInput;
        ui.startButton = startButton;
        ui.comboText = comboText;
        ui.exposureText = exposureText;
        ui.domainText = domainText;
        ui.loseNameText = loseNameText;
        ui.winNameText = winNameText;
        ui.simulateHitButton = hitButton;
        ui.simulateMissButton = missButton;
        ui.simulateWinButton = winButton;
        ui.simulateLoseButton = loseButton;
    }

    static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = panel.GetComponent<Image>();
        img.color = color;
        return panel;
    }

    static Text CreateText(string name, Transform parent, string text, int fontSize, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
        Text t = go.GetComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        return t;
    }

    static InputField CreateInputField(string name, Transform parent, Vector2 anchor, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        Image img = go.GetComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.18f, 1f);

        GameObject placeholder = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
        placeholder.transform.SetParent(go.transform, false);
        RectTransform pRt = placeholder.GetComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero;
        pRt.anchorMax = Vector2.one;
        pRt.offsetMin = new Vector2(10, 0);
        pRt.offsetMax = new Vector2(-10, 0);
        Text pText = placeholder.GetComponent<Text>();
        pText.text = "输入你的真名";
        pText.fontSize = 24;
        pText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        pText.alignment = TextAnchor.MiddleLeft;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(go.transform, false);
        RectTransform tRt = textObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = new Vector2(10, 0);
        tRt.offsetMax = new Vector2(-10, 0);
        Text tText = textObj.GetComponent<Text>();
        tText.text = "";
        tText.fontSize = 24;
        tText.color = Color.white;
        tText.alignment = TextAnchor.MiddleLeft;

        InputField input = go.GetComponent<InputField>();
        input.targetGraphic = img;
        input.placeholder = pText;
        input.textComponent = tText;
        input.characterLimit = 12;
        input.characterValidation = InputField.CharacterValidation.Alphanumeric;
        return input;
    }

    static Button CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        Image img = go.GetComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.3f, 1f);

        Text text = CreateText("Text", go.transform, label, 24, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        RectTransform textRt = text.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        return go.GetComponent<Button>();
    }

    static RectTransform CreateWordContainer(GameObject canvas)
    {
        GameObject container = new GameObject("WordContainer", typeof(RectTransform));
        container.transform.SetParent(canvas.transform, false);
        RectTransform rt = container.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    static GameObject CreateWordPrefab()
    {
        GameObject prefab = new GameObject("WordPrefab", typeof(RectTransform), typeof(Image), typeof(WordProjectile));
        RectTransform rt = prefab.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.sizeDelta = new Vector2(200, 80);
        rt.anchoredPosition = Vector2.zero;

        Image img = prefab.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0.5f);

        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGO.transform.SetParent(prefab.transform, false);
        RectTransform labelRt = labelGO.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        Text tmp = labelGO.GetComponent<Text>();
        tmp.fontSize = 42;
        tmp.alignment = TextAnchor.MiddleCenter;
        tmp.color = Color.white;

        WordProjectile wp = prefab.GetComponent<WordProjectile>();
        wp.rectTransform = rt;
        wp.label = tmp;
        wp.background = img;

        string path = "Assets/Prefabs/WordPrefab.prefab";
        EnsureFolder("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(prefab, path);
        Object.DestroyImmediate(prefab);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    static void CreateSilhouettes(GameObject canvas)
    {
        CreateSilhouette("WitchKingPlaceholder", canvas.transform, new Vector2(200, 0), Color.black, "巫王");
        CreateSilhouette("MirrorPlaceholder", canvas.transform, new Vector2(1720, 0), Color.black, "镜像");
    }

    static void CreateSilhouette(string name, Transform parent, Vector2 pos, Color color, string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(400, 800);
        Image img = go.GetComponent<Image>();
        img.color = color;
        go.AddComponent<SilhouetteAnimator>();
        CreateText("Label", go.transform, label, 28, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 60));
    }
}
