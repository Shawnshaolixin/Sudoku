using System.Collections.Generic;
using Sudoku.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 主菜单:选择难度、开始游戏、进入设置。
    /// UI 层级由 Prefab 承载(UiPrefabBuilder 生成),这里只绑定逻辑与文案。
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        [Header("难度选择")]
        [SerializeField] private Button _diffEasy;
        [SerializeField] private Button _diffMedium;
        [SerializeField] private Button _diffHard;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _settingsButton;

        [Header("文案")]
        [SerializeField] private Text _title;
        [SerializeField] private Text _subtitle;
        [SerializeField] private Text _diffLabel;
        [SerializeField] private Text _statsText;

        private readonly List<DiffEntry> _difficultyEntries = new List<DiffEntry>();
        private Difficulty _selected = Difficulty.Easy;
        private SettingsPanelView _settingsPanel;

        private struct DiffEntry
        {
            public Difficulty Difficulty;
            public Button Button;
        }

        private void Awake()
        {
            _settingsPanel = FindFirstObjectByType<SettingsPanelView>();
            WireUi();
            RefreshDifficulty();
        }

        private void WireUi()
        {
            _title.text = Localization.T("menu.title");
            _subtitle.text = Localization.T("menu.subtitle");
            _diffLabel.text = Localization.T("menu.chooseDifficulty");

            AddDifficultyButton(_diffEasy, Difficulty.Easy);
            AddDifficultyButton(_diffMedium, Difficulty.Medium);
            AddDifficultyButton(_diffHard, Difficulty.Hard);

            UiFactory.Wire(_startButton, () => SceneNavigator.LoadGameplay(_selected));
            UiFactory.Wire(_settingsButton, () => _settingsPanel?.Show());
        }

        private void AddDifficultyButton(Button button, Difficulty difficulty)
        {
            button.GetComponentInChildren<Text>().text = DifficultyName(difficulty);
            UiFactory.Wire(button, () =>
            {
                _selected = difficulty;
                RefreshDifficulty();
            });
            _difficultyEntries.Add(new DiffEntry { Difficulty = difficulty, Button = button });
        }

        private void RefreshDifficulty()
        {
            for (int i = 0; i < _difficultyEntries.Count; i++)
            {
                var e = _difficultyEntries[i];
                e.Button.targetGraphic.color = e.Difficulty == _selected ? Theme.Primary : Theme.Secondary;
            }

            if (_statsText != null)
            {
                var s = StatisticsStore.Load();
                _statsText.text = Localization.F("menu.stats", s.TotalGames, s.CompletedGames);
            }
        }

        private static string DifficultyName(Difficulty d) => d switch
        {
            Difficulty.Beginner => Localization.T("difficulty.beginner"),
            Difficulty.Easy => Localization.T("difficulty.easy"),
            Difficulty.Medium => Localization.T("difficulty.medium"),
            Difficulty.Hard => Localization.T("difficulty.hard"),
            Difficulty.Expert => Localization.T("difficulty.expert"),
            Difficulty.Master => Localization.T("difficulty.master"),
            _ => d.ToString()
        };

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
