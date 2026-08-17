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
        private const string TogglePrefabPath = CommonDir + "/ToggleButton.prefab";
        private const string ModalPrefabPath = CommonDir + "/ModalPanel.prefab";

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

            EnsureFolder(CommonDir);
            EnsureFolder(MenuDir);
            EnsureFolder(GameplayDir);

            var button = BuildButtonPrefab(semibold);
            var toggle = BuildTogglePrefab(button, regular);
            var modal = BuildModalPrefab();

            BuildMainMenu(button, semibold, regular);
            BuildSettings(button, toggle, modal, semibold, regular);
            BuildGameplay(button, semibold, regular);
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

        /// <summary>开关按钮 = 通用按钮变体(宽 400×60,正文字号),供设置面板使用。</summary>
        private static GameObject BuildTogglePrefab(GameObject buttonPrefab, Font regular)
        {
            var root = InstantiateUnder(buttonPrefab, null);
            root.name = "ToggleButton";
            var le = root.GetComponent<LayoutElement>();
            le.preferredWidth = 400;
            le.preferredHeight = 60;
            var label = root.GetComponentInChildren<Text>();
            label.font = regular;
            label.fontSize = 24;

            var asset = PrefabUtility.SaveAsPrefabAsset(root.gameObject, TogglePrefabPath);
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
            var diffEasy = AddButton(button, row, "Diff_Easy", Localization.T("difficulty.easy"), Theme.Secondary, 150, 64, semibold, 26);
            var diffMedium = AddButton(button, row, "Diff_Medium", Localization.T("difficulty.medium"), Theme.Secondary, 150, 64, semibold, 26);
            var diffHard = AddButton(button, row, "Diff_Hard", Localization.T("difficulty.hard"), Theme.Secondary, 150, 64, semibold, 26);

            var startButton = AddButton(button, root, "StartButton", Localization.T("menu.start"), Theme.Primary, 340, 84, semibold, 30);
            var settingsButton = AddButton(button, root, "SettingsButton", Localization.T("menu.settings"), Theme.Secondary, 200, 64, semibold, 26);
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
                SetRef(so, "_startButton", startButton);
                SetRef(so, "_settingsButton", settingsButton);
            });

            PrefabUtility.SaveAsPrefabAsset(root.gameObject, MainMenuPrefabPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
        }

        private static void BuildSettings(GameObject button, GameObject toggle, GameObject modal, Font semibold, Font regular)
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

            var sound = AddButton(toggle, panel, "Toggle_Sound", ToggleLabel("settings.sound"), Theme.Secondary, 400, 60, regular, 24);
            var vibration = AddButton(toggle, panel, "Toggle_Vibration", ToggleLabel("settings.vibration"), Theme.Secondary, 400, 60, regular, 24);
            var music = AddButton(toggle, panel, "Toggle_Music", ToggleLabel("settings.music"), Theme.Secondary, 400, 60, regular, 24);
            var mistakes = AddButton(toggle, panel, "Toggle_Mistakes", ToggleLabel("settings.mistakes"), Theme.Secondary, 400, 60, regular, 24);

            var clearStats = AddButton(button, panel, "ClearStatsButton", Localization.T("settings.clearStats"), Theme.Secondary, 400, 60, regular, 24);
            var deleteData = AddButton(button, panel, "DeleteDataButton", Localization.T("settings.deleteData"), Theme.Secondary, 400, 60, regular, 24);
            var buyRemoveAds = AddButton(button, panel, "BuyRemoveAdsButton", Localization.T("settings.buyRemoveAds"), Theme.Secondary, 400, 60, regular, 24);
            var restore = AddButton(button, panel, "RestorePurchaseButton", Localization.T("settings.restore"), Theme.Secondary, 400, 60, regular, 24);
            var privacy = AddButton(button, panel, "PrivacyButton", Localization.T("settings.privacy"), Theme.Secondary, 400, 60, regular, 24);

            var feedback = CreateText("Feedback", panel, regular, 20, Theme.Feedback);
            var close = AddButton(button, panel, "CloseButton", Localization.T("settings.close"), Theme.Primary, 400, 64, semibold, 26);

            overlay.gameObject.SetActive(false);

            AssignView(view, so =>
            {
                SetRef(so, "_overlay", overlay.gameObject);
                SetRef(so, "_title", title);
                SetRef(so, "_feedbackText", feedback);
                SetToggle(so, "_soundToggle", sound);
                SetToggle(so, "_vibrationToggle", vibration);
                SetToggle(so, "_musicToggle", music);
                SetToggle(so, "_mistakesToggle", mistakes);
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

        private static void BuildGameplay(GameObject button, Font semibold, Font regular)
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

            var result = CreateText("Result", root, semibold, 26, Theme.Success);

            var pad = CreateRect("NumberPad", root);
            AddHBox(pad, 6f);
            var numberButtons = new Button[9];
            for (int d = 0; d < 9; d++)
            {
                numberButtons[d] = AddButton(button, pad, $"Btn_{d + 1}", (d + 1).ToString(), Theme.Secondary, 64, 64, semibold, 30);
            }
            // 图标字符由运行时动态渲染,无需预烘焙
            var eraseButton = AddButton(button, pad, "Btn_Erase", "←", Theme.Secondary, 64, 64, semibold, 30);
            var modeButton = AddButton(button, pad, "Btn_Mode", "＋", Theme.Secondary, 64, 64, semibold, 30);

            var bar = CreateRect("Toolbar", root);
            AddHBox(bar, 8f);
            var undoButton = AddButton(button, bar, "UndoButton", Localization.T("game.undo"), Theme.Secondary, 88, 64, semibold, 26);
            var hintButton = AddButton(button, bar, "HintButton", Localization.T("game.hint"), Theme.Secondary, 88, 64, semibold, 26);
            var menuButton = AddButton(button, bar, "MenuButton", Localization.T("game.menu"), Theme.Secondary, 88, 64, semibold, 26);

            var diffBar = CreateRect("DifficultyBar", root);
            AddHBox(diffBar, 8f);
            var easyButton = AddButton(button, diffBar, "EasyButton", Localization.T("difficulty.easy"), Theme.Secondary, 120, 64, semibold, 26);
            var mediumButton = AddButton(button, diffBar, "MediumButton", Localization.T("difficulty.medium"), Theme.Secondary, 120, 64, semibold, 26);
            var hardButton = AddButton(button, diffBar, "HardButton", Localization.T("difficulty.hard"), Theme.Secondary, 120, 64, semibold, 26);

            var stats = CreateText("Stats", root, regular, 18, Theme.TextMuted);

            AssignView(view, so =>
            {
                SetRef(so, "_boardGrid", boardGrid);
                SetRef(so, "_title", title);
                SetRef(so, "_statusText", status);
                SetRef(so, "_modeText", mode);
                SetRef(so, "_resultText", result);
                SetRef(so, "_statsText", stats);
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
            var skipButton = AddButton(button, row, "SkipButton", Localization.T("onboarding.skip"), Theme.Secondary, 160, 64, semibold, 26);
            var nextButton = AddButton(button, row, "NextButton", Localization.T("onboarding.next"), Theme.Primary, 200, 64, semibold, 26);
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

        private static string ToggleLabel(string key) =>
            $"{Localization.T(key)}: {Localization.T("common.on")}";

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

        /// <summary>实例化通用按钮并覆盖:名称、标签文案、背景色、尺寸、字体。</summary>
        private static Button AddButton(GameObject prefab, Transform parent, string name, string label, Color bg,
            float width, float height, Font font, int fontSize)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.GetComponent<Image>().color = bg;
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

        private static void SetToggle(SerializedObject so, string field, Button button)
        {
            var p = so.FindProperty(field);
            p.FindPropertyRelative("Button").objectReferenceValue = button;
            p.FindPropertyRelative("Label").objectReferenceValue = button.GetComponentInChildren<Text>();
        }
    }
}
