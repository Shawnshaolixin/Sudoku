using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Gameplay.Editor
{
    /// <summary>
    /// 一键生成全部 UI Prefab(菜单:Sudoku → UI → 2. Build UI Prefabs)。
    /// 生成后 Prefab 即为真源,可在编辑器里直接改布局/配色/换美术素材。
    /// 文本使用 Unity 原生 UGUI Text + 直接引用 MiSans 字体文件,运行时动态渲染字形,不烘焙。
    /// 命名规范:帕斯卡 + 类型后缀(Button/Text/Panel/Row/Bar/Pad/Overlay),差异项用下划线。
    /// 说明:棋盘 81 格是动态内容,Prefab 里只留 <c>BoardGrid</c> 空容器,由 SudokuBoardView 运行时填充。
    /// </summary>
    public static class UiPrefabBuilder
    {
        private const string UiRoot = "Assets/App/Prefabs/UI";
        private const string CommonDir = UiRoot + "/Common";
        private const string MenuDir = UiRoot + "/Menu";
        private const string GameplayDir = UiRoot + "/Gameplay";

        private const string ButtonPrefabPath = CommonDir + "/Button.prefab";
        private const string ModalPrefabPath = CommonDir + "/ModalPanel.prefab";

        // Kenney UI Pack 素材(Assets/Resources/Art/UI/,SpriteImportSettings 自动 9-slice 切图)
        private const string ArtUiDir = "Assets/Resources/Art/UI";
        private const string SpritePrimaryPath = ArtUiDir + "/button_rectangle_depth_flat.png"; // 主要按钮(立体下边)
        private const string SpriteFlatPath = ArtUiDir + "/button_rectangle_flat.png";          // 常规按钮(平面)
        private const string SlideGreyPath = ArtUiDir + "/slide_horizontal_grey.png";           // 开关轨道(关)
        private const string SlideColorPath = ArtUiDir + "/slide_horizontal_color.png";         // 开关轨道(开)
        private const string SlideHandlePath = ArtUiDir + "/slide_hangle.png";                  // 开关把手
        private const string StarFilledPath = ArtUiDir + "/star.png";                           // 实心星
        private const string StarOutlinePath = ArtUiDir + "/star_outline.png";                  // 空心星

        private static Sprite _primarySprite;
        private static Sprite _flatSprite;
        private static Sprite _slideGrey;
        private static Sprite _slideColor;
        private static Sprite _slideHandle;
        private static Sprite _starFilled;
        private static Sprite _starOutline;
        private static Sprite PrimarySprite => Cache(ref _primarySprite, SpritePrimaryPath);
        private static Sprite FlatSprite => Cache(ref _flatSprite, SpriteFlatPath);
        private static Sprite SlideGrey => Cache(ref _slideGrey, SlideGreyPath);
        private static Sprite SlideColor => Cache(ref _slideColor, SlideColorPath);
        private static Sprite SlideHandle => Cache(ref _slideHandle, SlideHandlePath);
        private static Sprite StarFilled => Cache(ref _starFilled, StarFilledPath);
        private static Sprite StarOutline => Cache(ref _starOutline, StarOutlinePath);

        private static Sprite Cache(ref Sprite cached, string path) =>
            cached != null ? cached : (cached = LoadSprite(path));

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"[UiPrefabBuilder] 缺少 Sprite:{path}\n请把 Kenney UI Pack 的 PNG 放到 {ArtUiDir} 下(SpriteImportSettings 会自动切图)。");
            return sprite;
        }

        public const string MainMenuPrefabPath = MenuDir + "/MainMenuView.prefab";
        public const string SettingsPrefabPath = MenuDir + "/SettingsPanelView.prefab";
        public const string GameplayPrefabPath = GameplayDir + "/SudokuBoardView.prefab";
        public const string OnboardingPrefabPath = GameplayDir + "/OnboardingView.prefab";

        /// <summary>一键重建全部:字体检查 → Prefab → 两个场景。适合迁移/CI 全量验证。</summary>
        [MenuItem("Sudoku/UI/Rebuild Everything")]
        public static void RebuildEverything()
        {
            FontSetup.EnsureFontFiles();
            BuildAll();
            MenuSceneBuilder.Create();
            GameplaySceneBuilder.Create();
            Debug.Log("[UiPrefabBuilder] 全部重建完成。");
        }

        [MenuItem("Sudoku/UI/2. Build UI Prefabs")]
        public static void BuildAll()
        {
            if (!FontSetup.EnsureFontFiles())
            {
                if (!Application.isBatchMode)
                    EditorUtility.DisplayDialog("Sudoku", "缺少 MiSans 字体文件,请先放置到 Assets/App/Prefabs/UI/Assets/Fonts/。", "好的");
                return;
            }

            var semibold = FontSetup.LoadSemibold();
            var regular = FontSetup.LoadRegular();
            AssetDatabase.Refresh(); // 确保 Kenney PNG 已导入(SpriteImportSettings 切图后才有 Sprite)

            EnsureFolder(CommonDir);
            EnsureFolder(MenuDir);
            EnsureFolder(GameplayDir);

            var button = BuildButtonPrefab(semibold);
            var modal = BuildModalPrefab();

            BuildMainMenu(button, semibold, regular);
            BuildSettings(button, modal, semibold, regular);
            BuildGameplay(button, modal, semibold, regular);
            BuildOnboarding(button, modal, semibold, regular);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Sudoku", "UI Prefab 已生成:\n" +
                    $"• {MainMenuPrefabPath}\n• {SettingsPrefabPath}\n• {GameplayPrefabPath}\n• {OnboardingPrefabPath}\n\n" +
                    "接着重新执行 Sudoku → Create Menu Scene / Create Gameplay Scene 重建场景。", "好的");
        }

        // ---------- Common 组件 ----------

        /// <summary>通用按钮:背景图 + 按钮 + 标签,尺寸/配色/文案按实例覆盖。</summary>
        private static GameObject BuildButtonPrefab(Font semibold)
        {
            var root = CreateRect("Button", null);
            var image = root.gameObject.AddComponent<Image>();
            image.sprite = PrimarySprite; // 默认主按钮外观,实例按需覆盖
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.94f, 0.94f, 0.94f, 1f);
            colors.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            var le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 64;
            le.preferredHeight = 64;

            var label = CreateText("Label", root, semibold, 26, Theme.ButtonLabel);
            Stretch(label.rectTransform);

            var asset = PrefabUtility.SaveAsPrefabAsset(root.gameObject, ButtonPrefabPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            return asset;
        }

        /// <summary>模态弹窗容器:全屏遮罩 + 居中 Panel(垂直布局),尺寸/间距按实例覆盖。</summary>
        private static GameObject BuildModalPrefab()
        {
            var root = CreateRect("ModalPanel", null);
            Stretch(root);
            root.gameObject.AddComponent<Image>().color = Theme.OverlayDim;

            var panel = CreateRect("Panel", root);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(620, 800);
            panel.anchoredPosition = Vector2.zero;
            panel.gameObject.AddComponent<Image>().color = Theme.Panel;
            AddVBox(panel, 16f, TextAnchor.UpperCenter, new RectOffset(28, 28, 28, 28));

            var asset = PrefabUtility.SaveAsPrefabAsset(root.gameObject, ModalPrefabPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            return asset;
        }

        // ---------- 界面 ----------

        private static void BuildMainMenu(GameObject button, Font semibold, Font regular)
        {
            var root = CreateRect("MainMenuView", null);
            Stretch(root);
            root.gameObject.AddComponent<Image>().color = Theme.Background;
            AddVBox(root, 22f, TextAnchor.MiddleCenter, new RectOffset(24, 24, 80, 48));
            var view = root.gameObject.AddComponent<MainMenuView>();

            var title = CreateText("Title", root, semibold, 72, Theme.Text);
            title.text = Localization.T("menu.title");
            var subtitle = CreateText("Subtitle", root, regular, 26, Theme.TextMuted);
            subtitle.text = Localization.T("menu.subtitle");
            var diffLabel = CreateText("DiffLabel", root, semibold, 28, Theme.Text);
            diffLabel.text = Localization.T("menu.chooseDifficulty");

            var row = CreateRect("DifficultyRow", root);
            AddHBox(row, 14f);
            var diffEasy = AddButton(button, row, "Diff_Easy", Localization.T("difficulty.easy"), FlatSprite, 150, 64, semibold, 26);
            var diffMedium = AddButton(button, row, "Diff_Medium", Localization.T("difficulty.medium"), FlatSprite, 150, 64, semibold, 26);
            var diffHard = AddButton(button, row, "Diff_Hard", Localization.T("difficulty.hard"), FlatSprite, 150, 64, semibold, 26);

            var startButton = AddButton(button, root, "StartButton", Localization.T("menu.start"), PrimarySprite, 340, 84, semibold, 30);
            var settingsButton = AddButton(button, root, "SettingsButton", Localization.T("menu.settings"), FlatSprite, 200, 64, semibold, 26);
            var stats = CreateText("Stats", root, regular, 22, Theme.TextMuted);

            AssignView(view, so =>
            {
                SetRef(so, "_title", title);
                SetRef(so, "_subtitle", subtitle);
                SetRef(so, "_diffLabel", diffLabel);
                SetRef(so, "_statsText", stats);
                SetRef(so, "_diffEasy", diffEasy);
                SetRef(so, "_diffMedium", diffMedium);
                SetRef(so, "_diffHard", diffHard);
                SetRef(so, "_diffSpriteNormal", FlatSprite);
                SetRef(so, "_diffSpriteSelected", PrimarySprite);
                SetRef(so, "_startButton", startButton);
                SetRef(so, "_settingsButton", settingsButton);
            });

            PrefabUtility.SaveAsPrefabAsset(root.gameObject, MainMenuPrefabPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
        }

        private static void BuildSettings(GameObject button, GameObject modal, Font semibold, Font regular)
        {
            var root = CreateRect("SettingsPanelView", null);
            var view = root.gameObject.AddComponent<SettingsPanelView>();

            var overlay = InstantiateUnder(modal, root);
            overlay.name = "Overlay";
            var panel = (RectTransform)overlay.Find("Panel");
            panel.sizeDelta = new Vector2(620, 920); // 12 个子元素按文本行高需 ~892px,留余量
            var vbox = panel.GetComponent<VerticalLayoutGroup>();
            vbox.spacing = 16f;
            vbox.padding = new RectOffset(28, 28, 28, 28);

            var title = CreateText("Title", panel, semibold, 36, Theme.Text);
            title.text = Localization.T("settings.title");

            // 四项开关:左侧名称 + 右侧真 Switch(轨道 + 把手)
            var (sound, soundLabel) = AddSwitchRow(button, panel, "Toggle_Sound", Localization.T("settings.sound"), regular, 24);
            var (vibration, vibrationLabel) = AddSwitchRow(button, panel, "Toggle_Vibration", Localization.T("settings.vibration"), regular, 24);
            var (music, musicLabel) = AddSwitchRow(button, panel, "Toggle_Music", Localization.T("settings.music"), regular, 24);
            var (mistakes, mistakesLabel) = AddSwitchRow(button, panel, "Toggle_Mistakes", Localization.T("settings.mistakes"), regular, 24);

            var clearStats = AddButton(button, panel, "ClearStatsButton", Localization.T("settings.clearStats"), FlatSprite, 400, 60, regular, 24);
            var deleteData = AddButton(button, panel, "DeleteDataButton", Localization.T("settings.deleteData"), FlatSprite, 400, 60, regular, 24);
            var buyRemoveAds = AddButton(button, panel, "BuyRemoveAdsButton", Localization.T("settings.buyRemoveAds"), FlatSprite, 400, 60, regular, 24);
            var restore = AddButton(button, panel, "RestorePurchaseButton", Localization.T("settings.restore"), FlatSprite, 400, 60, regular, 24);
            var privacy = AddButton(button, panel, "PrivacyButton", Localization.T("settings.privacy"), FlatSprite, 400, 60, regular, 24);

            var feedback = CreateText("Feedback", panel, regular, 20, Theme.Feedback);
            var close = AddButton(button, panel, "CloseButton", Localization.T("settings.close"), PrimarySprite, 400, 64, semibold, 26);

            overlay.gameObject.SetActive(false);

            AssignView(view, so =>
            {
                SetRef(so, "_overlay", overlay.gameObject);
                SetRef(so, "_title", title);
                SetRef(so, "_feedbackText", feedback);
                SetSwitch(so, "_soundToggle", sound, soundLabel);
                SetSwitch(so, "_vibrationToggle", vibration, vibrationLabel);
                SetSwitch(so, "_musicToggle", music, musicLabel);
                SetSwitch(so, "_mistakesToggle", mistakes, mistakesLabel);
                SetRef(so, "_trackOnSprite", SlideColor);
                SetRef(so, "_trackOffSprite", SlideGrey);
                SetRef(so, "_clearStatsButton", clearStats);
                SetRef(so, "_deleteDataButton", deleteData);
                SetRef(so, "_buyRemoveAdsButton", buyRemoveAds);
                SetRef(so, "_restoreButton", restore);
                SetRef(so, "_privacyButton", privacy);
                SetRef(so, "_closeButton", close);
            });

            PrefabUtility.SaveAsPrefabAsset(root.gameObject, SettingsPrefabPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
        }

        private static void BuildGameplay(GameObject button, GameObject modal, Font semibold, Font regular)
        {
            var root = CreateRect("SudokuBoardView", null);
            Stretch(root);
            root.gameObject.AddComponent<Image>().color = Theme.Background;
            AddVBox(root, 12f, TextAnchor.UpperCenter, new RectOffset(16, 16, 24, 16));
            var view = root.gameObject.AddComponent<SudokuBoardView>();

            var title = CreateText("Title", root, semibold, 40, Theme.Text);
            title.text = Localization.T("menu.title");
            var status = CreateText("Status", root, regular, 22, Theme.TextMuted);
            var mode = CreateText("Mode", root, regular, 20, Theme.TextMuted);

            var boardGrid = CreateRect("BoardGrid", root); // 棋盘 81 格由运行时填充

            var pad = CreateRect("NumberPad", root);
            AddHBox(pad, 6f);
            var numberButtons = new Button[9];
            for (int d = 0; d < 9; d++)
            {
                numberButtons[d] = AddButton(button, pad, $"Btn_{d + 1}", (d + 1).ToString(), FlatSprite, 64, 64, semibold, 30);
            }
            // 图标字符由运行时动态渲染,无需预烘焙
            var eraseButton = AddButton(button, pad, "Btn_Erase", "←", FlatSprite, 64, 64, semibold, 30);
            var modeButton = AddButton(button, pad, "Btn_Mode", "＋", FlatSprite, 64, 64, semibold, 30);

            var bar = CreateRect("Toolbar", root);
            AddHBox(bar, 8f);
            var undoButton = AddButton(button, bar, "UndoButton", Localization.T("game.undo"), FlatSprite, 88, 64, semibold, 26);
            var hintButton = AddButton(button, bar, "HintButton", Localization.T("game.hint"), FlatSprite, 88, 64, semibold, 26);
            var menuButton = AddButton(button, bar, "MenuButton", Localization.T("game.menu"), FlatSprite, 88, 64, semibold, 26);

            var diffBar = CreateRect("DifficultyBar", root);
            AddHBox(diffBar, 8f);
            var easyButton = AddButton(button, diffBar, "EasyButton", Localization.T("difficulty.easy"), FlatSprite, 120, 64, semibold, 26);
            var mediumButton = AddButton(button, diffBar, "MediumButton", Localization.T("difficulty.medium"), FlatSprite, 120, 64, semibold, 26);
            var hardButton = AddButton(button, diffBar, "HardButton", Localization.T("difficulty.hard"), FlatSprite, 120, 64, semibold, 26);

            var stats = CreateText("Stats", root, regular, 18, Theme.TextMuted);

            // 胜利结算弹窗(默认隐藏,由 SudokuBoardView 在胜利时填充并显示;放最后保证盖住键盘/工具栏)
            var overlay = InstantiateUnder(modal, root);
            overlay.name = "ResultOverlay";
            var panel = (RectTransform)overlay.Find("Panel");
            panel.sizeDelta = new Vector2(620, 560);
            var vbox = panel.GetComponent<VerticalLayoutGroup>();
            vbox.spacing = 10f;
            vbox.padding = new RectOffset(32, 32, 28, 28);

            var stars = CreateRect("Stars", panel);
            AddFixedHBox(stars, 16f);
            var starImages = new Image[3];
            for (int s = 0; s < 3; s++)
            {
                var starRect = CreateRect($"Star_{s + 1}", stars);
                var starImage = starRect.gameObject.AddComponent<Image>();
                starImage.sprite = StarFilled;
                starImage.type = Image.Type.Simple;
                starRect.sizeDelta = new Vector2(52, 52);
                starImages[s] = starImage;
            }

            var resultTitle = CreateText("ResultTitle", panel, semibold, 44, Theme.Text);
            resultTitle.text = Localization.T("result.title");
            var subtitle = CreateText("ResultSubtitle", panel, regular, 22, Theme.TextMuted);

            var divider = CreateRect("Divider", panel);
            divider.gameObject.AddComponent<Image>().color = new Color(0.4f, 0.4f, 0.5f, 0.25f);
            var dividerLe = divider.gameObject.AddComponent<LayoutElement>();
            dividerLe.preferredHeight = 2;

            var timeRow = AddStatRow(panel, "Stat_Time", Localization.T("result.time"), regular, semibold);
            var bestRow = AddStatRow(panel, "Stat_Best", Localization.T("result.best"), regular, semibold);
            var hintsRow = AddStatRow(panel, "Stat_Hints", Localization.T("result.hints"), regular, semibold);

            var resultButtons = CreateRect("ResultButtons", panel);
            AddHBox(resultButtons, 12f);
            var resultNext = AddButton(button, resultButtons, "ResultNext", Localization.T("result.next"), PrimarySprite, 180, 64, semibold, 26);
            var resultHome = AddButton(button, resultButtons, "ResultHome", Localization.T("result.home"), FlatSprite, 180, 64, semibold, 26);

            overlay.gameObject.SetActive(false);

            // 调试:右上角小按钮,直接触发胜利撒花(正式发布前删掉此段)
            var testBtn = AddButton(button, root, "TestConfettiButton", "Test", FlatSprite, 72, 72, semibold, 18);
            testBtn.transform.SetAsLastSibling();
            var testLe = testBtn.GetComponent<LayoutElement>();
            testLe.ignoreLayout = true; // 不参与 VBox 排版,自由定位到右上角
            var testRt = (RectTransform)testBtn.transform;
            testRt.anchorMin = new Vector2(1f, 1f);
            testRt.anchorMax = new Vector2(1f, 1f);
            testRt.anchoredPosition = new Vector2(-16f, -16f);
            testRt.sizeDelta = new Vector2(72, 72);

            AssignView(view, so =>
            {
                SetRef(so, "_boardGrid", boardGrid);
                SetRef(so, "_title", title);
                SetRef(so, "_statusText", status);
                SetRef(so, "_modeText", mode);
                SetRef(so, "_statsText", stats);
                SetRef(so, "_resultOverlay", overlay.gameObject);
                SetRef(so, "_resultSubtitle", subtitle);
                SetRef(so, "_resultTime", timeRow.Value);
                SetRef(so, "_resultBest", bestRow.Value);
                SetRef(so, "_resultHints", hintsRow.Value);
                SetRef(so, "_starFilled", StarFilled);
                SetRef(so, "_starOutline", StarOutline);
                SetRef(so, "_resultNextButton", resultNext);
                SetRef(so, "_resultHomeButton", resultHome);
                SetRef(so, "_confettiTestButton", testBtn);
                var starArr = so.FindProperty("_starImages");
                starArr.arraySize = 3;
                for (int i = 0; i < 3; i++) starArr.GetArrayElementAtIndex(i).objectReferenceValue = starImages[i];
                SetRef(so, "_eraseButton", eraseButton);
                SetRef(so, "_modeButton", modeButton);
                SetRef(so, "_undoButton", undoButton);
                SetRef(so, "_hintButton", hintButton);
                SetRef(so, "_menuButton", menuButton);
                SetRef(so, "_easyButton", easyButton);
                SetRef(so, "_mediumButton", mediumButton);
                SetRef(so, "_hardButton", hardButton);
                SetRef(so, "_semiboldFont", semibold);
                SetRef(so, "_regularFont", regular);
                var arr = so.FindProperty("_numberButtons");
                arr.arraySize = 9;
                for (int i = 0; i < 9; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = numberButtons[i];
            });

            PrefabUtility.SaveAsPrefabAsset(root.gameObject, GameplayPrefabPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
        }

        private static void BuildOnboarding(GameObject button, GameObject modal, Font semibold, Font regular)
        {
            var root = CreateRect("OnboardingView", null);
            var view = root.gameObject.AddComponent<OnboardingView>();

            var overlay = InstantiateUnder(modal, root);
            overlay.name = "Overlay";
            var panel = (RectTransform)overlay.Find("Panel");
            panel.sizeDelta = new Vector2(720, 560);
            var vbox = panel.GetComponent<VerticalLayoutGroup>();
            vbox.spacing = 20f;
            vbox.padding = new RectOffset(32, 32, 32, 32);

            var title = CreateText("Title", panel, semibold, 34, Theme.Text);
            title.text = Localization.T("onboarding.title");

            var stepText = CreateText("StepText", panel, regular, 26, Theme.Text, TextAnchor.UpperLeft, wrap: true); // 多行步骤文案需要换行
            var stepLe = stepText.gameObject.AddComponent<LayoutElement>();
            stepLe.preferredWidth = 640;
            stepLe.preferredHeight = 220;

            var row = CreateRect("Buttons", panel);
            AddHBox(row, 16f);
            var skipButton = AddButton(button, row, "SkipButton", Localization.T("onboarding.skip"), FlatSprite, 160, 64, semibold, 26);
            var nextButton = AddButton(button, row, "NextButton", Localization.T("onboarding.next"), PrimarySprite, 200, 64, semibold, 26);
            var nextLabel = nextButton.GetComponentInChildren<Text>();

            AssignView(view, so =>
            {
                SetRef(so, "_overlay", overlay.gameObject);
                SetRef(so, "_title", title);
                SetRef(so, "_stepText", stepText);
                SetRef(so, "_nextLabel", nextLabel);
                SetRef(so, "_skipButton", skipButton);
                SetRef(so, "_nextButton", nextButton);
            });

            PrefabUtility.SaveAsPrefabAsset(root.gameObject, OnboardingPrefabPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
        }

        // ---------- 构建工具 ----------

        /// <summary>确保资产目录存在(递归)。</summary>
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            var parent = path.Substring(0, slash);
            var name = path.Substring(slash + 1);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        /// <summary>设置面板一行开关:左侧名称 + 右侧真 Switch(轨道图 + 把手),返回开关 Button 与名称 Text。</summary>
        private static (Button Button, Text Label) AddSwitchRow(GameObject buttonPrefab, Transform parent, string name,
            string label, Font font, int fontSize)
        {
            var row = CreateRect(name, parent);
            AddFixedHBox(row, 12f);

            var labelText = CreateText("Label", row, font, fontSize, Theme.Text, TextAnchor.MiddleLeft);
            var labelLe = labelText.gameObject.AddComponent<LayoutElement>();
            labelLe.preferredWidth = 280;
            labelLe.preferredHeight = 32;
            labelText.text = label;

            var sw = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, row);
            sw.name = "Switch";
            // 去掉按钮自带的文字子节点,开关不需要标签
            var swLabel = sw.transform.Find("Label");
            if (swLabel != null) UnityEngine.Object.DestroyImmediate(swLabel.gameObject);
            var swImage = sw.GetComponent<Image>();
            swImage.sprite = SlideGrey;
            swImage.type = Image.Type.Sliced;
            var swLe = sw.GetComponent<LayoutElement>();
            swLe.preferredWidth = 64;
            swLe.preferredHeight = 32;

            var knob = CreateRect("Knob", sw.transform);
            var knobImage = knob.gameObject.AddComponent<Image>();
            knobImage.sprite = SlideHandle;
            knobImage.type = Image.Type.Simple;
            knob.anchorMin = new Vector2(0f, 0.5f);
            knob.anchorMax = new Vector2(0f, 0.5f);
            knob.anchoredPosition = new Vector2(2f, 0f);
            knob.sizeDelta = new Vector2(28, 28);

            return (sw.GetComponent<Button>(), labelText);
        }

        /// <summary>结算弹窗一行统计:左侧标签 + 右侧数值(固定宽度,不随内容伸缩)。</summary>
        private static (Text Label, Text Value) AddStatRow(Transform parent, string name, string label, Font labelFont, Font valueFont)
        {
            var row = CreateRect(name, parent);
            var layout = AddFixedHBox(row, 8f);
            layout.childAlignment = TextAnchor.MiddleLeft;
            var rowLe = row.gameObject.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 34;

            var labelText = CreateText("Label", row, labelFont, 22, Theme.TextMuted, TextAnchor.MiddleLeft);
            var labelLe = labelText.gameObject.AddComponent<LayoutElement>();
            labelLe.preferredWidth = 260;
            labelText.text = label;

            var valueText = CreateText("Value", row, valueFont, 22, Theme.Text, TextAnchor.MiddleRight);
            var valueLe = valueText.gameObject.AddComponent<LayoutElement>();
            valueLe.preferredWidth = 180;

            return (labelText, valueText);
        }

        /// <summary>固定宽度横向布局:子元素按 LayoutElement 的 preferred 尺寸摆放,不拉伸占满。</summary>
        private static HorizontalLayoutGroup AddFixedHBox(RectTransform rt, float spacing)
        {
            var layout = AddHBox(rt, spacing);
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;
            return layout;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>创建 UGUI Text(原生动态字体渲染,直接引用 MiSans 字体文件,不烘焙)。</summary>
        private static Text CreateText(string name, Transform parent, Font font, int size,
            Color color, TextAnchor align = TextAnchor.MiddleCenter, bool wrap = false)
        {
            var rt = CreateRect(name, parent);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = align;
            text.color = color;
            text.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false; // 文本不拦截点击
            return text;
        }

        private static VerticalLayoutGroup AddVBox(RectTransform rt, float spacing, TextAnchor align, RectOffset padding)
        {
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = align;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = padding;
            return layout;
        }

        private static HorizontalLayoutGroup AddHBox(RectTransform rt, float spacing)
        {
            var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        /// <summary>实例化 Prefab 并返回其 RectTransform。</summary>
        private static RectTransform InstantiateUnder(GameObject prefab, Transform parent)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            return (RectTransform)go.transform;
        }

        /// <summary>实例化通用按钮并覆盖:名称、标签文案、背景 Sprite、尺寸、字体。Sprite 默认用常规(平面)外观。</summary>
        private static Button AddButton(GameObject prefab, Transform parent, string name, string label, Sprite sprite,
            float width, float height, Font font, int fontSize)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            var image = go.GetComponent<Image>();
            image.sprite = sprite != null ? sprite : FlatSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            var labelText = go.GetComponentInChildren<Text>();
            labelText.text = label;
            labelText.font = font;
            labelText.fontSize = fontSize;
            return go.GetComponent<Button>();
        }

        private static void AssignView(MonoBehaviour view, Action<SerializedObject> assign)
        {
            var so = new SerializedObject(view);
            assign(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRef(SerializedObject so, string field, UnityEngine.Object value)
        {
            so.FindProperty(field).objectReferenceValue = value;
        }

        private static void SetSwitch(SerializedObject so, string field, Button button, Text label)
        {
            var p = so.FindProperty(field);
            p.FindPropertyRelative("Button").objectReferenceValue = button;
            p.FindPropertyRelative("Label").objectReferenceValue = label;
            p.FindPropertyRelative("Track").objectReferenceValue = button.GetComponent<Image>();
            p.FindPropertyRelative("Knob").objectReferenceValue = button.transform.Find("Knob");
        }
    }
}
