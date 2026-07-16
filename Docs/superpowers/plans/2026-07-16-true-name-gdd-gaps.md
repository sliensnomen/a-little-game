# 真名剥露 GDD 缺口补齐计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把 GDD 剩余的体验缺口补齐，让游戏从“可跑通”提升到“符合设计规格”。

**Architecture:** 在现有 `GameManager/TrueNameSystem/HolyLightSystem/WordSpawner/InputManager/PhaseManager/DomainManager/UIManager` 基础上扩展，新增 `ParticleManager` 和 `AudioManager`，补全输入判定、演出时序、UI 字体。

**Tech Stack:** 团结引擎 1.9.3 / Built-in 2D / C# / Unity UI / TextMeshPro / ScriptableObjects / 对象池。

## Global Constraints

- 平台：macOS Apple Silicon，Metal，1920×1080 固定分辨率。
- 渲染：Built-in 2D，无复杂 Shader，粒子总数 ≤ 500。
- 不引入 `Rigidbody2D`。
- 所有动态 UI 在代码中创建，避免场景合并冲突。
- `Time.unscaledDeltaTime` 用于 `Time.timeScale` 变化时的 UI/动画。
- 每 30 分钟编译/Play 后休息 5 分钟（MacBook Air M3）。

---

### Task 1: 开始界面验证与 PlayerPrefs

**Files:**
- Modify: `Assets/Scripts/UIManager.cs`
- Modify: `Assets/Scripts/GameManager.cs`

**Interfaces:**
- Consumes: `UIManager.nameInput`, `UIManager.startButton`, `GameManager.SetPlayerName(string name)`.
- Produces: `PlayerPrefs.SetString("PlayerTrueName", playerTrueName)`, `startButton.interactable`, `startButton.GetComponent<Image>().color` alpha + glow.

- [ ] **Step 1: 在 `UIManager.Start` 中配置 `nameInput` 限制**
  - `nameInput.contentType = InputField.ContentType.Alphanumeric;`
  - `nameInput.characterLimit = 12;`
  - 监听 `nameInput.onValueChanged`，在回调中调用 `UpdateStartButtonState(string text)`。

- [ ] **Step 2: 实现 `UpdateStartButtonState`**
  - 如果 `string.IsNullOrWhiteSpace(text)` 或包含非字母数字字符：
    - `startButton.interactable = false;`
    - 按钮 `Image.color.a = 0.3f;`
    - 若已创建提示文本，显示红色提示：“无名者无法进入王座之间。”
  - 否则：
    - `startButton.interactable = true;`
    - 按钮 `Image.color.a = 1f;`
    - 添加金色外发光（Outline 组件或修改按钮颜色）。

- [ ] **Step 3: 在 `GameManager.SetPlayerName` 中持久化玩家真名**
  - 在方法末尾添加 `PlayerPrefs.SetString("PlayerTrueName", playerTrueName);`。
  - （可选）在 `GameManager.Awake` 或 `Start` 中读取 `PlayerPrefs.GetString("PlayerTrueName", "")` 并赋值给 `playerTrueName`。

- [ ] **Step 4: 验证**
  - 进入 Play 模式，测试空输入、空格、非法字符、超长字符、合法输入。
  - 检查 `PlayerPrefs` 中是否保存了 `PlayerTrueName`。

- [ ] **Step 5: 提交**

---

### Task 2: E 反击窗口与双诵蓄力

**Files:**
- Modify: `Assets/Scripts/GameConfig.cs`
- Modify: `Assets/ScriptableObjects/GameConfig.asset`
- Modify: `Assets/Scripts/InputManager.cs`
- Modify: `Assets/Scripts/WordSpawner.cs`

**Interfaces:**
- Consumes: `WordProjectile.holdElapsed`, `InputManager.LastHolyTime`, `InputManager.LastDarkTime`, `InputManager.IsDualChanting`.
- Produces: `InputManager.OnDualChantStarted`, `InputManager.OnDualChantCompleted`, `WordSpawner.ECounter(WordProjectile word)`, `WordSpawner.OnDualChantCompleted()`.

- [ ] **Step 1: 在 `GameConfig.cs` 新增字段**
  ```csharp
  public float eCounterWindow = 0.2f;
  public float dualChantHoldTime = 3f;
  ```

- [ ] **Step 2: 更新 `GameConfig.asset` YAML**
  - 在末尾添加：
    ```yaml
    eCounterWindow: 0.2
    dualChantHoldTime: 3
    ```

- [ ] **Step 3: 扩展 `InputManager.cs`**
  - 将 `LastQTime`/`LastETime` 重命名为 `LastHolyTime`/`LastDarkTime`（保持原有引用可更新）。
  - 新增事件：
    ```csharp
    public UnityEvent OnDualChantStarted = new UnityEvent();
    public UnityEvent OnDualChantCompleted = new UnityEvent();
    public bool IsDualChanting { get; private set; }
    ```
  - 在 `Update` 中检测 `Q` 和 `E` 同时按住：
    - 若刚进入双诵状态，触发 `OnDualChantStarted` 并计时。
    - 持续按住 `dualChantHoldTime` 秒后触发 `OnDualChantCompleted`，重置状态。
    - 若任一键松开，取消计时并退出双诵状态。

- [ ] **Step 4: 修改 `WordSpawner.cs`**
  - 在 `OnEPressed` 的魔文判定路径中，增加 `word.holdElapsed <= config.eCounterWindow` 检查；超时视为 Miss。
  - 新增 `OnDualChantCompleted()` 方法并注册到 `InputManager.OnDualChantCompleted`：
    - 调用 `ClearAllWords();`
    - 调用 `TrueNameSystem.Instance?.RevealLetter();`
    - 调用 `TrueNameSystem.Instance?.AddDomainCharge(config.domainChargePerDualChant);`（若 GDD 中无该字段，可用 `domainChargePerDual` 或新增 `domainChargePerDualChant`）。
    - 播放 `DomainVisual.Instance?.Play()` 或简单的屏幕闪光。

- [ ] **Step 5: 验证**
  - 测试 E 在单词刚到线 0.2s 内按下成功，超过后失败。
  - 测试按住 Q+E 3 秒后全场清空并揭示一个字母。

- [ ] **Step 6: 提交**

---

### Task 3: 粒子系统

**Files:**
- Create: `Assets/Scripts/ParticleManager.cs`
- Modify: `Assets/Scripts/WordSpawner.cs`
- Modify: `Assets/Scripts/DomainManager.cs`
- Modify: `Assets/Scripts/UIManager.cs`

**Interfaces:**
- Consumes: `WordProjectile.language`, `WordProjectile.rectTransform.anchoredPosition`, `WordType`.
- Produces: `ParticleManager.PlayHit(Vector2 pos, LanguageType lang)`, `ParticleManager.PlayMiss(Vector2 pos, LanguageType lang)`, `ParticleManager.PlayDomain()`, `ParticleManager.PlayWin()`, `ParticleManager.PlayLose()`.

- [ ] **Step 1: 创建 `ParticleManager`**
  - 单例，使用对象池（预生成 100 个 ParticleSystem）。
  - 提供按语言/事件触发 burst 的方法。
  - 使用 `UnityEngine.ParticleSystem` 或 `Sprite` 动画实现，总数 ≤ 500。

- [ ] **Step 2: 在 `WordSpawner` 中调用粒子**
  - `ResolveHit` 中调用 `ParticleManager.Instance?.PlayHit(...)`。
  - `ResolveMiss` 中调用 `ParticleManager.Instance?.PlayMiss(...)`。

- [ ] **Step 3: 在 `DomainManager` 和 `UIManager` 中调用粒子**
  - 领域展开时播放 `PlayDomain()`。
  - 胜利/失败时播放 `PlayWin()` / `PlayLose()`。

- [ ] **Step 4: 验证**
  - 命中、Miss、领域、胜负均有粒子效果。
  - 检查 Hierarchy 中粒子数量不超过 500。

- [ ] **Step 5: 提交**

---

### Task 4: 完整胜负演出

**Files:**
- Modify: `Assets/Scripts/UIManager.cs`
- Modify: `Assets/Scripts/GameManager.cs`
- Modify: `Assets/Scripts/SilhouetteDirector.cs`
- Modify: `Assets/Scripts/VisualFeedbackBootstrap.cs`（如需新增演出组件）

**Interfaces:**
- Consumes: `GameManager.playerTrueName`, `TrueNameSystem.EnemyTrueName`, `DialogueController.Instance`。
- Produces: `UIManager.PlayLoseSequence()`, `UIManager.PlayWinSequence()`。

- [ ] **Step 1: 失败演出时序**
  - T+0.0s：冻结时间，清空单词。
  - T+0.3s：裂纹贴图从屏幕中央放射展开，Alpha 0→1，耗时 0.5s。
  - T+0.8s：玩家真名字符逐个浮现（打字机，每字符 0.15s）。
  - T+1.5s：镜像台词浮现（GDD 7.5）。
  - T+3.0s：巫王剪影低头，眼瞳金光熄灭。
  - T+4.0s：屏幕完全碎裂（Alpha 1→0，裂纹加深），显示“再试一次”按钮。

- [ ] **Step 2: 胜利演出时序**
  - 真名爆破粒子 + 领域闪光。
  - 显示胜利面板和 GDD 7.4 独白。

- [ ] **Step 3: 验证**
  - 正确输入真名触发胜利演出；达到 100% 暴露或猜错致死触发失败演出。

- [ ] **Step 4: 提交**

---

### Task 5: TMP 与指定字体

**Files:**
- Modify: `Assets/Scripts/UIManager.cs`
- Modify: `Assets/Scripts/WordProjectile.cs`
- Modify: `Assets/Scripts/DialogueController.cs`
- Project: 导入 TextMeshPro 包和字体资源。

**Interfaces:**
- Consumes: `UnityEngine.UI.Text`（旧）。
- Produces: `TMPro.TextMeshProUGUI`。

- [ ] **Step 1: 导入 TMP 与字体**
  - 导入 TextMeshPro 基础资源。
  - 下载/配置 Cinzel、UnifrakturMaguntia、Source Han Serif SDF。

- [ ] **Step 2: 替换代码中动态创建的 Text**
  - 在 `UIManager`、`WordProjectile`、`DialogueController` 中，把 `new GameObject(..., typeof(Text))` 改为 `typeof(TextMeshProUGUI)`。
  - 调整字体大小、颜色、对齐方式，适配 TMP API。

- [ ] **Step 3: 验证**
  - 所有 UI 文字、单词、对话均使用新字体正常显示。
  - 检查 SDF Atlas 不包含过多字符导致包体过大。

- [ ] **Step 4: 提交**

---

### Task 6: 阶段背景与镜像演出

**Files:**
- Modify: `Assets/Scripts/PhaseManager.cs`
- Modify: `Assets/Scripts/SilhouetteDirector.cs`
- Modify: `Assets/Scripts/SilhouetteAnimator.cs`
- Modify: `Assets/ScriptableObjects/Phase_1.asset`, `Phase_2.asset`, `Phase_3.asset`

**Interfaces:**
- Consumes: `PhaseData.backgroundTint`, `PhaseData.speedMultiplier`, `PhaseData.spawnInterval`。
- Produces: `SilhouetteAnimator.SetCrackAlpha(float)`, `SilhouetteAnimator.SetEyeGlowColor(Color)`。

- [ ] **Step 1: 阶段背景变化**
  - 二阶段：背景变暗 + 边缘雾效贴图。
  - 三阶段：背景裂纹 + 顶部漏光贴图。
  - 在 `PhaseManager.EnterPhase` 中切换背景 Sprite/Overlay。

- [ ] **Step 2: 镜像演出**
  - 二阶段：镜像裂纹发光增强。
  - 三阶段：镜像剧烈抖动、眼瞳变红。
  - 在 `SilhouetteAnimator` 中增加抖动和眼瞳颜色控制接口。
  - 在 `SilhouetteDirector` 中监听阶段切换并调用对应演出。

- [ ] **Step 3: 验证**
  - 揭示 3 个字母进入二阶段，5 个字母进入三阶段，背景与镜像均有变化。

- [ ] **Step 4: 提交**

---

### Task 7: 音效

**Files:**
- Create: `Assets/Scripts/AudioManager.cs`
- Modify: `Assets/Scripts/WordSpawner.cs`
- Modify: `Assets/Scripts/DomainManager.cs`
- Modify: `Assets/Scripts/UIManager.cs`
- Modify: `Assets/Scripts/TrueNameSystem.cs`

**Interfaces:**
- Produces: `AudioManager.PlayHit()`, `AudioManager.PlayMiss()`, `AudioManager.PlayDomain()`, `AudioManager.PlayTypewriter()`, `AudioManager.PlayShatter()`。

- [ ] **Step 1: 创建 `AudioManager`**
  - 单例，使用 `AudioSource` 池或 `AudioSource.PlayOneShot`。
  - 提供对应事件的播放方法。

- [ ] **Step 2: 收集/生成 5 个音效**
  - 击碎（玻璃）、错误（低音嗡鸣）、领域（钟声）、打字机（按键）、碎裂（镜子）。
  - 放入 `Assets/Audio/SFX/`。

- [ ] **Step 3: 在对应事件处播放音效**
  - `WordSpawner` 命中/失败时播放 Hit/Miss。
  - `DomainManager` 展开时播放 Domain。
  - `UIManager` 打字机显示台词时播放 Typewriter。
  - `UIManager` 失败碎裂时播放 Shatter。

- [ ] **Step 4: 验证**
  - 每个事件都有正确音效，无重叠爆音。

- [ ] **Step 5: 提交**

---

## 执行顺序

1. Task 1 与 Task 2 可并行（修改的文件不重叠）。
2. Task 3（粒子）依赖 Task 2 的 WordSpawner 修改。
3. Task 4（胜负演出）依赖 Task 3 的粒子。
4. Task 5（TMP）依赖 Task 4 的 UIManager 修改。
5. Task 6（阶段视觉）与 Task 7（音效）可并行或串行。

**建议：** 1/2 → 3 → 4 → 5 → 6 → 7。

## 验收标准

- 开始界面：非法输入无法进入，合法输入保存玩家真名。
- 战斗：E 有 0.2s 窗口，Q+E 长按 3s 清空全场并揭示字母。
- 粒子：命中、Miss、领域、胜负均有对应特效。
- 胜负：完整演出时序符合 GDD 第 8 章与第 7.4/7.5 节。
- 字体：所有文字使用 TMP + 指定字体。
- 阶段：背景与镜像随阶段变化。
- 音效：5 类事件触发对应音效。
- 最终做一次完整 Mac Build 跑通。

---

> 文档版本：1.0
> 最后更新：2026-07-16
