using System.Collections.Generic;
using Sudoku.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Gameplay
{
    /// <summary>主菜单:选择难度、开始游戏、进入设置。</summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        private readonly List<DiffEntry> _difficultyEntries = new List<DiffEntry>();
        private Difficulty _selected = Difficulty.Easy;
        private SettingsPanelView _settingsPanel;
        private Text _statsText;

        private struct DiffEntry
        {
            public Difficulty Difficulty;
            public Image Image;
        }

        private void Awake()
        {
            _settingsPanel = FindFirstObjectByType<SettingsPanelView>();
            BuildUi();
            RefreshDifficulty();
        }

        private void BuildUi()
        {
            var root = (RectTransform)transform;
            UiFactory.Stretch(root);
            root.gameObject.AddComponent<Image>().color = Theme.Background; // 铺满背景色

            var layout = UiFactory.Vertical(root, 22f, TextAnchor.MiddleCenter);
            layout.padding = new RectOffset(24, 24, 80, 48);

            var title = UiFactory.CreateText("Title", transform, 72, TextAnchor.MiddleCenter, Theme.Text);
            title.text = Localization.T("menu.title");

            var subtitle = UiFactory.CreateText("Subtitle", transform, 26, TextAnchor.MiddleCenter, Theme.TextMuted);
            subtitle.text = Localization.T("menu.subtitle");

            var diffLabel = UiFactory.CreateText("DiffLabel", transform, 28, TextAnchor.MiddleCenter, Theme.Text);
            diffLabel.text = Localization.T("menu.chooseDifficulty");

            BuildDifficultyRow(transform);

            UiFactory.CreateButton("StartBtn", transform, Localization.T("menu.start"), Theme.Primary, () => SceneNavigator.LoadGameplay(_selected), 340, 84);
            UiFactory.CreateButton("SettingsBtn", transform, Localization.T("menu.settings"), Theme.Secondary, () => _settingsPanel?.Show(), 200, 64);

            _statsText = UiFactory.CreateText("Stats", transform, 22, TextAnchor.MiddleCenter, Theme.TextMuted);
        }

        private void BuildDifficultyRow(Transform parent)
        {
            var row = UiFactory.CreateRect("DifficultyRow", parent);
            UiFactory.Horizontal(row, 14f);
            AddDifficultyButton(row, "difficulty.easy", Difficulty.Easy);
            AddDifficultyButton(row, "difficulty.medium", Difficulty.Medium);
            AddDifficultyButton(row, "difficulty.hard", Difficulty.Hard);
        }

        private void AddDifficultyButton(Transform parent, string labelKey, Difficulty d)
        {
            var image = UiFactory.CreateImage($"Diff_{d}", parent, Theme.Secondary);
            var le = image.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 150;
            le.preferredHeight = 64;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => { _selected = d; RefreshDifficulty(); });

            var text = UiFactory.CreateText("Label", image.transform, 26, TextAnchor.MiddleCenter, Theme.Text);
            UiFactory.Stretch(text.rectTransform);
            text.text = Localization.T(labelKey);

            _difficultyEntries.Add(new DiffEntry { Difficulty = d, Image = image });
        }

        private void RefreshDifficulty()
        {
            for (int i = 0; i < _difficultyEntries.Count; i++)
            {
                var e = _difficultyEntries[i];
                e.Image.color = e.Difficulty == _selected ? Theme.Primary : Theme.Secondary;
            }

            if (_statsText != null)
            {
                var s = StatisticsStore.Load();
                _statsText.text = Localization.F("menu.stats", s.TotalGames, s.CompletedGames);
            }
        }

        private void Update()
        {
            // 安卓返回键/侧滑手势 → 退出应用
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }
    }
}
