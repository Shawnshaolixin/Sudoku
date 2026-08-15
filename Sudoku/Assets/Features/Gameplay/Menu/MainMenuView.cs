using System.Collections.Generic;
using Sudoku.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Gameplay
{
    /// <summary>主菜单:选择难度、开始游戏、进入设置。</summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        [Header("配色")]
        [SerializeField] private Color _primaryColor = new Color(0.35f, 0.55f, 0.95f, 1f);
        [SerializeField] private Color _secondaryColor = new Color(0.90f, 0.92f, 1f, 1f);
        [SerializeField] private Color _textColor = new Color(0.12f, 0.12f, 0.18f, 1f);

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
            var layout = UiFactory.Vertical(root, 22f, TextAnchor.MiddleCenter);
            layout.padding = new RectOffset(24, 24, 80, 48);

            var title = UiFactory.CreateText("Title", transform, 72, TextAnchor.MiddleCenter, _textColor);
            title.text = "数独";

            var subtitle = UiFactory.CreateText("Subtitle", transform, 26, TextAnchor.MiddleCenter, new Color(0.5f, 0.5f, 0.55f, 1f));
            subtitle.text = "Sudoku · 锻炼脑力,每天一局";

            var diffLabel = UiFactory.CreateText("DiffLabel", transform, 28, TextAnchor.MiddleCenter, _textColor);
            diffLabel.text = "选择难度";

            BuildDifficultyRow(transform);

            UiFactory.CreateButton("StartBtn", transform, "开始游戏", _primaryColor, () => SceneNavigator.LoadGameplay(_selected), 340, 84);
            UiFactory.CreateButton("SettingsBtn", transform, "设置", _secondaryColor, () => _settingsPanel?.Show(), 200, 64);

            _statsText = UiFactory.CreateText("Stats", transform, 22, TextAnchor.MiddleCenter, new Color(0.45f, 0.45f, 0.5f, 1f));
        }

        private void BuildDifficultyRow(Transform parent)
        {
            var row = UiFactory.CreateRect("DifficultyRow", parent);
            UiFactory.Horizontal(row, 14f);
            AddDifficultyButton(row, "简单", Difficulty.Easy);
            AddDifficultyButton(row, "中等", Difficulty.Medium);
            AddDifficultyButton(row, "困难", Difficulty.Hard);
        }

        private void AddDifficultyButton(Transform parent, string label, Difficulty d)
        {
            var image = UiFactory.CreateImage($"Diff_{d}", parent, _secondaryColor);
            var le = image.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 150;
            le.preferredHeight = 64;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => { _selected = d; RefreshDifficulty(); });

            var text = UiFactory.CreateText("Label", image.transform, 26, TextAnchor.MiddleCenter, _textColor);
            UiFactory.Stretch(text.rectTransform);
            text.text = label;

            _difficultyEntries.Add(new DiffEntry { Difficulty = d, Image = image });
        }

        private void RefreshDifficulty()
        {
            for (int i = 0; i < _difficultyEntries.Count; i++)
            {
                var e = _difficultyEntries[i];
                e.Image.color = e.Difficulty == _selected ? _primaryColor : _secondaryColor;
            }

            if (_statsText != null)
            {
                var s = StatisticsStore.Load();
                _statsText.text = $"总局 {s.TotalGames} · 完成 {s.CompletedGames}";
            }
        }
    }
}
