using System.Text;
using Sudoku.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 棋盘视图:界面主体(标题、键盘、工具栏等)由 Prefab 承载,
    /// 仅 9×9 盘面在运行时填入 <see cref="_boardGrid"/> 容器(动态内容不落 Prefab)。
    /// Awake 自动寻找 <see cref="SudokuGameController"/> 并订阅事件刷新。
    /// </summary>
    public sealed class SudokuBoardView : MonoBehaviour
    {
        [Header("棋盘尺寸与线宽")]
        [SerializeField] private float _cellSize = 60f;     // 单元格边长
        [SerializeField] private float _cellSpacing = 1f;   // 格间细线宽
        [SerializeField] private float _boxSpacing = 4f;    // 3×3 宫之间的粗线宽
        [SerializeField] private int _boardBorder = 4;      // 棋盘外边框宽

        [Header("Prefab 引用(由 UiPrefabBuilder 生成时绑定)")]
        [SerializeField] private RectTransform _boardGrid;
        [SerializeField] private Text _title;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _modeText;
        [SerializeField] private Text _statsText;

        [Header("胜利结算弹窗")]
        [SerializeField] private GameObject _resultOverlay;
        [SerializeField] private Image[] _starImages;   // 3 颗星,按评级实心/空心
        [SerializeField] private Sprite _starFilled;
        [SerializeField] private Sprite _starOutline;
        [SerializeField] private Text _resultSubtitle;
        [SerializeField] private Text _resultTime;
        [SerializeField] private Text _resultBest;
        [SerializeField] private Text _resultHints;
        [SerializeField] private Button _resultNextButton;
        [SerializeField] private Button _resultHomeButton;
        [SerializeField] private Button _confettiTestButton; // 调试:手动触发胜利撒花(发布前可从 Prefab 移除)
        [SerializeField] private Button[] _numberButtons;   // 数字键盘 1~9
        [SerializeField] private Button _eraseButton;
        [SerializeField] private Button _modeButton;
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _hintButton;
        [SerializeField] private Button _menuButton;
        [SerializeField] private Button _easyButton;
        [SerializeField] private Button _mediumButton;
        [SerializeField] private Button _hardButton;
        [SerializeField] private Font _semiboldFont; // 棋盘数字
        [SerializeField] private Font _regularFont;  // 笔记

        private SudokuGameController _controller;
        private bool _built;

        private Cell[] _cells;

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
            WireUi();
        }

        /// <summary>绑定控制器并构建棋盘 UI(幂等,重复调用会被忽略)。</summary>
        public void Bind(SudokuGameController controller)
        {
            if (_built || controller == null) return;
            _controller = controller;
            controller.BoardChanged += Refresh;
            controller.GameFinished += OnGameFinished;
            BuildGrid();
            _built = true;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_controller == null) return;
            _controller.BoardChanged -= Refresh;
            _controller.GameFinished -= OnGameFinished;
        }

        /// <summary>绑定 Prefab 按钮的点击逻辑与文案。</summary>
        private void WireUi()
        {
            if (_title != null) _title.text = Localization.T("menu.title");

            for (int i = 0; i < _numberButtons.Length; i++)
            {
                int n = i + 1;
                UiFactory.Wire(_numberButtons[i], () => _controller.InputNumber(n));
            }
            UiFactory.Wire(_eraseButton, () => _controller.Erase());
            UiFactory.Wire(_modeButton, () => _controller.ToggleInputMode());
            UiFactory.Wire(_undoButton, () => _controller.Undo());
            UiFactory.Wire(_hintButton, () => _controller.TryUseHint());
            UiFactory.Wire(_menuButton, SceneNavigator.LoadMenu);
            UiFactory.Wire(_easyButton, () => StartNew(Difficulty.Easy));
            UiFactory.Wire(_mediumButton, () => StartNew(Difficulty.Medium));
            UiFactory.Wire(_hardButton, () => StartNew(Difficulty.Hard));
            if (_resultNextButton != null)
                UiFactory.Wire(_resultNextButton, () => StartNew(_controller.Difficulty)); // 下一局:同难度
            if (_resultHomeButton != null)
                UiFactory.Wire(_resultHomeButton, SceneNavigator.LoadMenu);
            if (_confettiTestButton != null)
                UiFactory.Wire(_confettiTestButton, VictoryCelebration.Play); // 调试:直接触发撒花
        }

        private void Update()
        {
            if (_controller == null || !_controller.IsReady) return;

            // 安卓返回键/侧滑手势 → 回到主菜单
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SceneNavigator.LoadMenu();
                return;
            }

            if (_statusText != null)
                _statusText.text = Localization.F("game.status", DifficultyName(_controller.Difficulty), FormatTime(_controller.ElapsedSeconds), _controller.HintCount);
            if (_modeText != null)
                _modeText.text = _controller.InputMode == GameInputMode.Number
                    ? Localization.T("game.mode.number")
                    : Localization.T("game.mode.note");
        }

        // ---------- 棋盘网格构建(运行时,Prefab 只留 BoardGrid 空容器) ----------

        private void BuildGrid()
        {
            var grid = _boardGrid;

            // 棋盘容器:深色背景作为「网格线」颜色,3×3 排列 9 个宫
            var boardImage = grid.gameObject.AddComponent<Image>();
            boardImage.color = Theme.GridLine;
            var boardLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            boardLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            boardLayout.constraintCount = SudokuBoard.BoxSize;

            // 一个宫(box)的边长 = 3 格 + 2 条格间细线
            float boxSize = SudokuBoard.BoxSize * _cellSize + (SudokuBoard.BoxSize - 1) * _cellSpacing;
            boardLayout.cellSize = new Vector2(boxSize, boxSize);
            boardLayout.spacing = new Vector2(_boxSpacing, _boxSpacing); // 宫间粗线
            boardLayout.padding = new RectOffset(_boardBorder, _boardBorder, _boardBorder, _boardBorder); // 外边框
            boardLayout.childAlignment = TextAnchor.MiddleCenter;

            _cells = new Cell[SudokuBoard.CellCount];
            for (int box = 0; box < SudokuBoard.BoxSize * SudokuBoard.BoxSize; box++)
            {
                var boxGo = UiFactory.CreateRect($"Box_{box}", grid);
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

                    var valueText = CreateCellText("Value", cellRect, _semiboldFont, 30, Theme.PlayerText);
                    var noteText = CreateCellText("Note", cellRect, _regularFont, 12, Theme.NoteText);

                    _cells[index] = new Cell { Background = image, ValueText = valueText, NoteText = noteText };
                }
            }
        }

        private static Text CreateCellText(string name, Transform parent, Font font, int size, Color color)
        {
            var rt = UiFactory.CreateRect(name, parent);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            UiFactory.Stretch(rt);
            return text;
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
            if (!won) return;

            VictoryCelebration.Play(); // 胜利撒花(贴图缺失时静默跳过)

            if (_resultOverlay == null) return;

            // 星级:无错误 3 星,≤2 次错误 2 星,否则 1 星
            int mistakes = _controller.MistakeCount;
            int stars = mistakes == 0 ? 3 : mistakes <= 2 ? 2 : 1;
            for (int i = 0; i < _starImages.Length; i++)
                _starImages[i].sprite = i < stars ? _starFilled : _starOutline;

            var diff = _controller.Difficulty;
            string perfect = mistakes == 0 ? " · " + Localization.T("result.perfect") : "";
            _resultSubtitle.text = DifficultyName(diff) + perfect;

            _resultTime.text = FormatTime(_controller.ElapsedSeconds);

            int best = _controller.Statistics.BestSecondsFor(diff);
            if (best == int.MaxValue)
            {
                _resultBest.text = "--:--";
                _resultBest.color = Theme.Text;
            }
            else
            {
                bool newRecord = _controller.ElapsedSeconds < best;
                _resultBest.text = (newRecord ? Localization.T("result.newRecord") + "  " : "") + FormatTime(best);
                _resultBest.color = newRecord ? Theme.Success : Theme.Text;
            }

            _resultHints.text = Mathf.Max(0, 3 - _controller.HintCount).ToString();

            _resultOverlay.transform.SetAsLastSibling(); // 保险:确保盖住键盘/工具栏等所有界面
            _resultOverlay.SetActive(true);
        }

        private void StartNew(Difficulty difficulty)
        {
            if (_resultOverlay != null) _resultOverlay.SetActive(false);
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
