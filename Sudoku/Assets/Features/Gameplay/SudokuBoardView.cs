using System.Text;
using Sudoku.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 棋盘视图:运行时用 UGUI 动态构建 9x9 盘面、数字键盘与工具栏,
    /// 在 Awake 中自动寻找 <see cref="SudokuGameController"/> 并订阅事件刷新。
    ///
    /// 说明:阶段 A 为「零手工配置」直接用 UGUI Text(内置字体,开箱即用);
    /// 正式版按 03/07 文档将 Text 替换为 TextMeshProUGUI 并指定字体资产即可,接口不变。
    /// </summary>
    public sealed class SudokuBoardView : MonoBehaviour
    {
        [Header("棋盘格尺寸")]
        [SerializeField] private float _cellSize = 64f;

        [Header("配色")]
        [SerializeField] private Color _cellBase = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color _cellAltBox = new Color(0.93f, 0.95f, 1f, 1f);
        [SerializeField] private Color _cellSelected = new Color(0.75f, 0.85f, 1f, 1f);
        [SerializeField] private Color _cellPeer = new Color(0.90f, 0.93f, 1f, 1f);
        [SerializeField] private Color _cellSameNumber = new Color(0.70f, 0.80f, 1f, 1f);
        [SerializeField] private Color _cellMistake = new Color(1f, 0.75f, 0.75f, 1f);
        [SerializeField] private Color _givenText = new Color(0.10f, 0.10f, 0.15f, 1f);
        [SerializeField] private Color _playerText = new Color(0.10f, 0.30f, 0.80f, 1f);
        [SerializeField] private Color _noteText = new Color(0.40f, 0.40f, 0.45f, 1f);

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
                _statusText.text = $"难度 {DifficultyName(_controller.Difficulty)}   时间 {FormatTime(_controller.ElapsedSeconds)}   提示 {_controller.HintCount}";
            if (_modeText != null)
                _modeText.text = _controller.InputMode == GameInputMode.Number ? "数字模式" : "笔记模式";
        }

        // ---------- UI 构建 ----------
        private void BuildUi()
        {
            var root = (RectTransform)transform;
            Stretch(root);

            var rootLayout = gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.spacing = 12;
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            // childControl* = true:布局组接管子元素尺寸(尊重 LayoutElement/Text 的 preferred 尺寸);
            // childForceExpandWidth = true:子元素水平方向铺满,便于居中。
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;
            rootLayout.padding = new RectOffset(16, 16, 24, 16);

            var title = CreateText("Title", transform, 40, TextAnchor.MiddleCenter, new Color(0.2f, 0.2f, 0.3f, 1f));
            title.text = "Sudoku";

            _statusText = CreateText("Status", transform, 22, TextAnchor.MiddleCenter, new Color(0.3f, 0.3f, 0.4f, 1f));
            _modeText = CreateText("Mode", transform, 20, TextAnchor.MiddleCenter, new Color(0.4f, 0.4f, 0.5f, 1f));

            BuildBoard(transform);

            _resultText = CreateText("Result", transform, 26, TextAnchor.MiddleCenter, new Color(0.2f, 0.6f, 0.3f, 1f));
            _resultText.text = "";

            BuildNumberPad(transform);
            BuildToolbar(transform);
            BuildDifficultyBar(transform);

            _statsText = CreateText("Stats", transform, 18, TextAnchor.MiddleCenter, new Color(0.45f, 0.45f, 0.5f, 1f));
        }

        private void BuildBoard(Transform parent)
        {
            var grid = CreateRect("Board", parent);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = SudokuBoard.Size;
            layout.cellSize = new Vector2(_cellSize, _cellSize);
            layout.spacing = new Vector2(2, 2);
            layout.childAlignment = TextAnchor.MiddleCenter;

            _cells = new Cell[SudokuBoard.CellCount];
            for (int i = 0; i < SudokuBoard.CellCount; i++)
            {
                var cellRect = CreateRect($"Cell_{i}", grid);
                var image = cellRect.gameObject.AddComponent<Image>();

                var button = cellRect.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None; // 手动配色,避免按钮过渡覆盖高亮
                int idx = i;
                button.onClick.AddListener(() => _controller.SelectCell(idx));

                var valueText = CreateText("Value", cellRect, 30, TextAnchor.MiddleCenter, _playerText);
                Stretch(valueText.rectTransform);
                var noteText = CreateText("Note", cellRect, 12, TextAnchor.MiddleCenter, _noteText);
                Stretch(noteText.rectTransform);

                _cells[i] = new Cell { Background = image, ValueText = valueText, NoteText = noteText };
            }
        }

        private void BuildNumberPad(Transform parent)
        {
            var pad = CreateRect("NumberPad", parent);
            AddHorizontalLayout(pad, 6f);

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
            var bar = CreateRect("Toolbar", parent);
            AddHorizontalLayout(bar, 8f);
            AddButton(bar, "撤销", () => _controller.Undo());
            AddButton(bar, "提示", () => _controller.TryUseHint());
        }

        private void BuildDifficultyBar(Transform parent)
        {
            var bar = CreateRect("DifficultyBar", parent);
            AddHorizontalLayout(bar, 8f);
            AddButton(bar, "简单", () => StartNew(Difficulty.Easy));
            AddButton(bar, "中等", () => StartNew(Difficulty.Medium));
            AddButton(bar, "困难", () => StartNew(Difficulty.Hard));
        }

        private static HorizontalLayoutGroup AddHorizontalLayout(RectTransform rt, float spacing)
        {
            var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            // childControl* = true 才会应用按钮上 LayoutElement 的 preferred 尺寸(64x64),
            // 否则按钮会退回默认 100x100 导致横向溢出/错位。
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        private static Button AddButton(Transform parent, string label, System.Action onClick)
        {
            var rt = CreateRect($"Btn_{label}", parent);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 64;
            le.preferredHeight = 64;

            var image = rt.gameObject.AddComponent<Image>();
            image.color = new Color(0.95f, 0.95f, 0.98f, 1f);

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());

            var text = CreateText("Label", rt, 26, TextAnchor.MiddleCenter, new Color(0.1f, 0.1f, 0.15f, 1f));
            Stretch(text.rectTransform);
            text.text = label;
            return button;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Text CreateText(string name, Transform parent, int fontSize, TextAnchor anchor, Color color)
        {
            var rt = CreateRect(name, parent);
            var text = rt.gameObject.AddComponent<Text>();
            // 关键:AddComponent<Text>() 不会自动绑定字体,必须显式赋值,否则文字不可见。
            text.font = GetDefaultFont();
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false; // 文本不拦截点击,让按钮/格子的 Image 接收事件
            return text;
        }

        private static Font _defaultFont;

        /// <summary>获取内置字体(Unity 2022.3 为 LegacyRuntime.ttf),并缓存复用。</summary>
        private static Font GetDefaultFont()
        {
            if (_defaultFont == null)
            {
                _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_defaultFont == null)
                    _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); // 兼容旧版本
            }
            return _defaultFont;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
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
                    cell.ValueText.color = _controller.IsGiven(i) ? _givenText : _playerText;
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
            if (_controller.IsMistake(index)) return _cellMistake;
            if (_controller.IsSelected(index)) return _cellSelected;
            if (_controller.IsSameNumber(index)) return _cellSameNumber;
            if (_controller.IsPeer(index)) return _cellPeer;

            int box = SudokuBoard.BoxOf(SudokuBoard.RowOf(index), SudokuBoard.ColOf(index));
            return box % 2 == 0 ? _cellBase : _cellAltBox;
        }

        private void OnGameFinished(bool won)
        {
            if (_resultText == null) return;
            _resultText.text = won ? $"完成!用时 {FormatTime(_controller.ElapsedSeconds)}" : "失败";
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

        private static string DifficultyName(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Beginner: return "入门";
                case Difficulty.Easy: return "简单";
                case Difficulty.Medium: return "中等";
                case Difficulty.Hard: return "困难";
                case Difficulty.Expert: return "专家";
                case Difficulty.Master: return "大师";
                default: return d.ToString();
            }
        }

        private static string StatsLine(GameStatistics s, Difficulty d)
        {
            int best = s.BestSecondsFor(d);
            string bestStr = best == int.MaxValue ? "--:--" : FormatTime(best);
            return $"总局 {s.TotalGames} · 完成 {s.CompletedGames} · 最佳 {bestStr}";
        }
    }
}
