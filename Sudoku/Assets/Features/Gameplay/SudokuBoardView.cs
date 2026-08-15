using System.Text;
using Sudoku.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 棋盘视图:运行时用 UGUI 动态构建 9x9 盘面、数字键盘与工具栏,
    /// 在 Awake 中自动寻找 <see cref="SudokuGameController"/> 并订阅事件刷新。
    /// </summary>
    public sealed class SudokuBoardView : MonoBehaviour
    {
        [Header("棋盘尺寸与线宽")]
        [SerializeField] private float _cellSize = 60f;     // 单元格边长
        [SerializeField] private float _cellSpacing = 1f;   // 格间细线宽
        [SerializeField] private float _boxSpacing = 4f;    // 3×3 宫之间的粗线宽
        [SerializeField] private int _boardBorder = 4;      // 棋盘外边框宽

        private SudokuGameController _controller;
        private bool _built;

        private Cell[] _cells;
        private Text _statusText;
        private Text _modeText;
        private Text _statsText;
        private Text _resultText;

        private struct Cell
        {
            public Image Background;
            public Text ValueText;
            public Text NoteText;
        }

        private void Awake()
        {
            if (_controller == null)
                _controller = FindFirstObjectByType<SudokuGameController>();
            if (_controller != null)
                Bind(_controller);
            EnsureOnboarding();
        }

        /// <summary>
        /// 保证新手引导一定生效:若尚未完成且场景里没有预置 OnboardingView(例如未重建场景),
        /// 就动态补一个,避免引导因场景缺少组件而失效。
        /// </summary>
        private void EnsureOnboarding()
        {
            if (SettingsService.OnboardingCompleted) return;
            if (FindFirstObjectByType<OnboardingView>() != null) return;

            var canvas = GetComponentInParent<Canvas>();
            var go = new GameObject("OnboardingView", typeof(RectTransform));
            go.transform.SetParent(canvas != null ? canvas.transform : transform, false);
            go.AddComponent<OnboardingView>();
        }

        /// <summary>绑定控制器并构建 UI(幂等,重复调用会被忽略)。</summary>
        public void Bind(SudokuGameController controller)
        {
            if (_built || controller == null) return;
            _controller = controller;
            controller.BoardChanged += Refresh;
            controller.GameFinished += OnGameFinished;
            BuildUi();
            _built = true;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_controller == null) return;
            _controller.BoardChanged -= Refresh;
            _controller.GameFinished -= OnGameFinished;
        }

        private void Update()
        {
            if (_controller == null || !_controller.IsReady) return;

            if (_statusText != null)
                _statusText.text = Localization.F("game.status", DifficultyName(_controller.Difficulty), FormatTime(_controller.ElapsedSeconds), _controller.HintCount);
            if (_modeText != null)
                _modeText.text = _controller.InputMode == GameInputMode.Number
                    ? Localization.T("game.mode.number")
                    : Localization.T("game.mode.note");
        }

        // ---------- UI 构建 ----------
        private void BuildUi()
        {
            var root = (RectTransform)transform;
            UiFactory.Stretch(root);
            root.gameObject.AddComponent<Image>().color = Theme.Background; // 铺满背景色

            var rootLayout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.spacing = 12;
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = false; // 棋盘保持自身宽度居中,不横向拉伸
            rootLayout.childForceExpandHeight = false;
            rootLayout.padding = new RectOffset(16, 16, 24, 16);

            var title = UiFactory.CreateText("Title", transform, 40, TextAnchor.MiddleCenter, Theme.Text);
            title.text = Localization.T("menu.title");

            _statusText = UiFactory.CreateText("Status", transform, 22, TextAnchor.MiddleCenter, Theme.TextMuted);
            _modeText = UiFactory.CreateText("Mode", transform, 20, TextAnchor.MiddleCenter, Theme.TextMuted);

            BuildBoard(transform);

            _resultText = UiFactory.CreateText("Result", transform, 26, TextAnchor.MiddleCenter, Theme.Success);
            _resultText.text = "";

            BuildNumberPad(transform);
            BuildToolbar(transform);
            BuildDifficultyBar(transform);

            _statsText = UiFactory.CreateText("Stats", transform, 18, TextAnchor.MiddleCenter, Theme.TextMuted);
        }

        private void BuildBoard(Transform parent)
        {
            // 一个宫(box)的边长 = 3 格 + 2 条格间细线
            float boxSize = SudokuBoard.BoxSize * _cellSize + (SudokuBoard.BoxSize - 1) * _cellSpacing;

            // 外层:棋盘容器,深色背景作为「网格线」颜色,3×3 排列 9 个宫
            var board = UiFactory.CreateRect("Board", parent);
            board.gameObject.AddComponent<Image>().color = Theme.GridLine;
            var boardLayout = board.gameObject.AddComponent<GridLayoutGroup>();
            boardLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            boardLayout.constraintCount = SudokuBoard.BoxSize;
            boardLayout.cellSize = new Vector2(boxSize, boxSize);
            boardLayout.spacing = new Vector2(_boxSpacing, _boxSpacing); // 宫间粗线
            boardLayout.padding = new RectOffset(_boardBorder, _boardBorder, _boardBorder, _boardBorder); // 外边框
            boardLayout.childAlignment = TextAnchor.MiddleCenter;

            _cells = new Cell[SudokuBoard.CellCount];
            for (int box = 0; box < SudokuBoard.BoxSize * SudokuBoard.BoxSize; box++)
            {
                var boxGo = UiFactory.CreateRect($"Box_{box}", board);
                var boxLayout = boxGo.gameObject.AddComponent<GridLayoutGroup>();
                boxLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                boxLayout.constraintCount = SudokuBoard.BoxSize;
                boxLayout.cellSize = new Vector2(_cellSize, _cellSize);
                boxLayout.spacing = new Vector2(_cellSpacing, _cellSpacing); // 格间细线
                boxLayout.childAlignment = TextAnchor.MiddleCenter;

                int boxRow = box / SudokuBoard.BoxSize;
                int boxCol = box % SudokuBoard.BoxSize;
                for (int r = 0; r < SudokuBoard.BoxSize; r++)
                for (int c = 0; c < SudokuBoard.BoxSize; c++)
                {
                    int index = SudokuBoard.Index(boxRow * SudokuBoard.BoxSize + r, boxCol * SudokuBoard.BoxSize + c);

                    var cellRect = UiFactory.CreateRect($"Cell_{index}", boxGo);
                    var image = cellRect.gameObject.AddComponent<Image>();

                    var button = cellRect.gameObject.AddComponent<Button>();
                    button.transition = Selectable.Transition.None; // 手动配色,避免按钮过渡覆盖高亮
                    button.onClick.AddListener(() => _controller.SelectCell(index));

                    var valueText = UiFactory.CreateText("Value", cellRect, 30, TextAnchor.MiddleCenter, Theme.PlayerText);
                    UiFactory.Stretch(valueText.rectTransform);
                    var noteText = UiFactory.CreateText("Note", cellRect, 12, TextAnchor.MiddleCenter, Theme.NoteText);
                    UiFactory.Stretch(noteText.rectTransform);

                    _cells[index] = new Cell { Background = image, ValueText = valueText, NoteText = noteText };
                }
            }
        }

        private void BuildNumberPad(Transform parent)
        {
            var pad = UiFactory.CreateRect("NumberPad", parent);
            UiFactory.Horizontal(pad, 6f);

            for (int d = 1; d <= 9; d++)
            {
                int n = d;
                AddButton(pad, n.ToString(), () => _controller.InputNumber(n));
            }
            AddButton(pad, "⌫", () => _controller.Erase());
            AddButton(pad, "✎", () => _controller.ToggleInputMode());
        }

        private void BuildToolbar(Transform parent)
        {
            var bar = UiFactory.CreateRect("Toolbar", parent);
            UiFactory.Horizontal(bar, 8f);
            AddButton(bar, Localization.T("game.undo"), () => _controller.Undo(), 88f);
            AddButton(bar, Localization.T("game.hint"), () => _controller.TryUseHint(), 88f);
            AddButton(bar, Localization.T("game.menu"), () => SceneNavigator.LoadMenu(), 88f);
        }

        private void BuildDifficultyBar(Transform parent)
        {
            var bar = UiFactory.CreateRect("DifficultyBar", parent);
            UiFactory.Horizontal(bar, 8f);
            AddButton(bar, Localization.T("difficulty.easy"), () => StartNew(Difficulty.Easy), 120f);
            AddButton(bar, Localization.T("difficulty.medium"), () => StartNew(Difficulty.Medium), 120f);
            AddButton(bar, Localization.T("difficulty.hard"), () => StartNew(Difficulty.Hard), 120f);
        }

        /// <summary>统一创建棋盘里的按钮(默认 64×64,可指定宽度)。</summary>
        private static Button AddButton(Transform parent, string label, System.Action onClick, float width = 64f)
        {
            return UiFactory.CreateButton($"Btn_{label}", parent, label, Theme.Secondary, onClick, width, 64);
        }

        // ---------- 刷新 ----------
        private void Refresh()
        {
            if (_controller == null || !_controller.IsReady || _cells == null) return;

            for (int i = 0; i < SudokuBoard.CellCount; i++)
            {
                int value = _controller.GetValue(i);
                int notes = _controller.GetNotes(i);
                var cell = _cells[i];

                if (value != 0)
                {
                    cell.ValueText.text = value.ToString();
                    cell.NoteText.text = "";
                    cell.ValueText.color = _controller.IsGiven(i) ? Theme.GivenText : Theme.PlayerText;
                }
                else
                {
                    cell.ValueText.text = "";
                    cell.NoteText.text = NotesToString(notes);
                }

                cell.Background.color = ResolveBackground(i);
            }

            if (_statsText != null && _controller.Statistics != null)
                _statsText.text = StatsLine(_controller.Statistics, _controller.Difficulty);
        }

        private Color ResolveBackground(int index)
        {
            if (_controller.IsMistake(index)) return Theme.CellMistake;
            if (_controller.IsSelected(index)) return Theme.CellSelected;
            if (_controller.IsSameNumber(index)) return Theme.CellSameNumber;
            if (_controller.IsPeer(index)) return Theme.CellPeer;

            int box = SudokuBoard.BoxOf(SudokuBoard.RowOf(index), SudokuBoard.ColOf(index));
            return box % 2 == 0 ? Theme.CellBase : Theme.CellAltBox;
        }

        private void OnGameFinished(bool won)
        {
            if (_resultText == null) return;
            _resultText.text = won ? Localization.F("game.win", FormatTime(_controller.ElapsedSeconds)) : "";
        }

        private void StartNew(Difficulty difficulty)
        {
            if (_resultText != null) _resultText.text = "";
            _controller.NewGame(difficulty);
        }

        // ---------- 工具 ----------
        private static string NotesToString(int mask)
        {
            if (mask == 0) return "";
            var sb = new StringBuilder();
            for (int d = 1; d <= 9; d++)
                if ((mask & (1 << d)) != 0) sb.Append(d).Append(' ');
            return sb.ToString().TrimEnd();
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return string.Format("{0:00}:{1:00}", total / 60, total % 60);
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

        private static string StatsLine(GameStatistics s, Difficulty d)
        {
            int best = s.BestSecondsFor(d);
            string bestStr = best == int.MaxValue ? "--:--" : FormatTime(best);
            return Localization.F("game.stats", s.TotalGames, s.CompletedGames, bestStr);
        }
    }
}
