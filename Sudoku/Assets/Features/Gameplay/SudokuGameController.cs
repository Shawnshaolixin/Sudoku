using System;
using System.Collections.Generic;
using Sudoku.Core;
using UnityEngine;

namespace Sudoku.Gameplay
{
    /// <summary>输入模式:填数字 / 记笔记。</summary>
    public enum GameInputMode
    {
        Number = 0,
        Note = 1
    }

    /// <summary>
    /// 对局控制器:负责一局数独的规则、状态与输入(与 UI 解耦)。
    /// 通过 C# 事件向外广播状态变化,由 SudokuBoardView 订阅刷新。
    /// 阶段 A 采用「事件 + 直接引用」的轻量结构,后续再按 03 文档引入 VContainer/UniTask/R3。
    /// </summary>
    public sealed class SudokuGameController : MonoBehaviour
    {
        private Difficulty _difficulty = Difficulty.Easy;
        private bool _showMistakes = true; // 错误检测开关(运行时从 SettingsService 读取)

        private SudokuBoard _puzzle;    // 给定数(不可改)
        private SudokuBoard _solution;  // 完整解
        private SudokuBoard _board;     // 当前玩家盘面
        private int[] _notes = new int[SudokuBoard.CellCount]; // 笔记位掩码:bit d = 候选数 d

        private int _selectedIndex = -1;
        private GameInputMode _inputMode = GameInputMode.Number;

        private readonly Stack<Move> _undoStack = new Stack<Move>();

        private int _hintCount = 3;

        private float _startTime;
        private bool _finished;
        private float _elapsedOnFinish;
        private GameStatistics _statistics;

        // 事件(供视图订阅)
        public event Action BoardChanged;
        public event Action<int> CellSelected;
        public event Action<bool> GameFinished;
        public event Action HintExhausted; // 提示用尽,后续在此接激励视频

        // 只读状态
        public SudokuBoard Board => _board;
        public SudokuBoard Puzzle => _puzzle;
        public int SelectedIndex => _selectedIndex;
        public GameInputMode InputMode => _inputMode;
        public int HintCount => _hintCount;
        public Difficulty Difficulty => _difficulty;
        public GameStatistics Statistics => _statistics;
        public bool IsFinished => _finished;
        public bool IsReady => _board != null;
        public bool ShowMistakes { get => _showMistakes; set => _showMistakes = value; }

        /// <summary>本局已用时间(秒),完成后冻结。</summary>
        public float ElapsedSeconds => _finished ? _elapsedOnFinish : Time.time - _startTime;

        private void Awake()
        {
            _statistics = StatisticsStore.Load();
            _showMistakes = SettingsService.ShowMistakes;
            NewGame(SceneNavigator.SelectedDifficulty);
        }

        // ---------- 开局 ----------
        public void NewGame(Difficulty difficulty)
        {
            _difficulty = difficulty;
            var generated = new SudokuGenerator().Generate(difficulty);
            _puzzle = generated.Puzzle;
            _solution = generated.Solution;
            _board = _puzzle.Clone();

            Array.Clear(_notes, 0, _notes.Length);
            _undoStack.Clear();
            _selectedIndex = -1;
            _inputMode = GameInputMode.Number;
            _hintCount = 3;
            _finished = false;
            _startTime = Time.time;
            _elapsedOnFinish = 0f;

            _statistics.OnGameStarted();
            StatisticsStore.Save(_statistics);
            BoardChanged?.Invoke();
        }

        // ---------- 输入 ----------
        public void SelectCell(int index)
        {
            if (index < 0 || index >= SudokuBoard.CellCount) return;
            _selectedIndex = index;
            CellSelected?.Invoke(index);
            BoardChanged?.Invoke(); // 高亮随选中变化
        }

        public void InputNumber(int number)
        {
            if (_finished || number < 1 || number > SudokuBoard.Size) return;
            if (_selectedIndex < 0 || IsGiven(_selectedIndex)) return;

            int oldValue = _board[_selectedIndex];
            int oldNotes = _notes[_selectedIndex];

            if (_inputMode == GameInputMode.Note)
            {
                _notes[_selectedIndex] ^= (1 << number); // 切换该候选位
                _undoStack.Push(new Move(_selectedIndex, oldValue, oldValue, oldNotes, _notes[_selectedIndex]));
            }
            else
            {
                int newValue = oldValue == number ? 0 : number; // 再点同数字 = 清除
                _board[_selectedIndex] = newValue;
                _notes[_selectedIndex] = 0;
                if (newValue != 0) AutoClearPeerNotes(_selectedIndex, newValue);
                _undoStack.Push(new Move(_selectedIndex, oldValue, newValue, oldNotes, 0));
            }

            BoardChanged?.Invoke();
            CheckFinish();
        }

        public void ToggleInputMode()
        {
            _inputMode = _inputMode == GameInputMode.Number ? GameInputMode.Note : GameInputMode.Number;
            BoardChanged?.Invoke();
        }

        public void Erase()
        {
            if (_finished || _selectedIndex < 0 || IsGiven(_selectedIndex)) return;
            int oldValue = _board[_selectedIndex];
            int oldNotes = _notes[_selectedIndex];
            _board[_selectedIndex] = 0;
            _notes[_selectedIndex] = 0;
            _undoStack.Push(new Move(_selectedIndex, oldValue, 0, oldNotes, 0));
            BoardChanged?.Invoke();
        }

        public void Undo()
        {
            if (_finished || _undoStack.Count == 0) return;
            var m = _undoStack.Pop();
            _board[m.Index] = m.OldValue;
            _notes[m.Index] = m.OldNotes;
            _selectedIndex = m.Index;
            BoardChanged?.Invoke();
            CellSelected?.Invoke(m.Index);
        }

        /// <summary>使用一次提示;成功返回 true。提示用尽后触发 HintExhausted。</summary>
        public bool TryUseHint()
        {
            if (_finished) return false;
            if (!HintEngine.GetHint(_board, out var hint)) return false;

            int index = SudokuBoard.Index(hint.Row, hint.Col);
            int oldValue = _board[index];
            int oldNotes = _notes[index];
            _board[index] = hint.Value;
            _notes[index] = 0;
            AutoClearPeerNotes(index, hint.Value);
            _undoStack.Push(new Move(index, oldValue, hint.Value, oldNotes, 0));
            _selectedIndex = index;

            if (_hintCount > 0) _hintCount--;
            else HintExhausted?.Invoke();

            BoardChanged?.Invoke();
            CellSelected?.Invoke(index);
            CheckFinish();
            return true;
        }

        /// <summary>增加提示次数(供激励视频回调用)。</summary>
        public void AddHints(int count)
        {
            if (count <= 0) return;
            _hintCount += count;
            BoardChanged?.Invoke();
        }

        // ---------- 查询(供视图) ----------
        public bool IsGiven(int index) => _puzzle != null && _puzzle[index] != 0;
        public int GetValue(int index) => _board != null ? _board[index] : 0;
        public int GetNotes(int index) => _notes != null ? _notes[index] : 0;
        public bool HasNote(int index, int digit) => (_notes[index] & (1 << digit)) != 0;
        public bool IsSelected(int index) => _selectedIndex == index;

        /// <summary>该格是否为错误填入(与解不一致),受「错误检测开关」控制。</summary>
        public bool IsMistake(int index) =>
            _showMistakes && _board[index] != 0 && _board[index] != _solution[index];

        /// <summary>该格是否与选中格同数字(用于同数字高亮)。</summary>
        public bool IsSameNumber(int index) =>
            _selectedIndex >= 0 && _board[_selectedIndex] != 0 && _board[index] == _board[_selectedIndex];

        /// <summary>该格是否与选中格同行/列/宫(用于关联高亮)。</summary>
        public bool IsPeer(int index) =>
            _selectedIndex >= 0 && index != _selectedIndex && ArePeers(index, _selectedIndex);

        public static bool ArePeers(int a, int b)
        {
            int ra = SudokuBoard.RowOf(a), ca = SudokuBoard.ColOf(a);
            int rb = SudokuBoard.RowOf(b), cb = SudokuBoard.ColOf(b);
            if (ra == rb || ca == cb) return true;
            return SudokuBoard.BoxOf(ra, ca) == SudokuBoard.BoxOf(rb, cb);
        }

        // ---------- 内部 ----------
        /// <summary>填入确定数字后,清除同行/列/宫笔记中的该数字(FR-04 自动清除)。</summary>
        private void AutoClearPeerNotes(int index, int value)
        {
            int clearBit = ~(1 << value);
            int row = SudokuBoard.RowOf(index);
            int col = SudokuBoard.ColOf(index);

            for (int c = 0; c < SudokuBoard.Size; c++) _notes[SudokuBoard.Index(row, c)] &= clearBit;
            for (int r = 0; r < SudokuBoard.Size; r++) _notes[SudokuBoard.Index(r, col)] &= clearBit;

            int br = (row / SudokuBoard.BoxSize) * SudokuBoard.BoxSize;
            int bc = (col / SudokuBoard.BoxSize) * SudokuBoard.BoxSize;
            for (int r = br; r < br + SudokuBoard.BoxSize; r++)
                for (int c = bc; c < bc + SudokuBoard.BoxSize; c++)
                    _notes[SudokuBoard.Index(r, c)] &= clearBit;
        }

        private void CheckFinish()
        {
            if (_board.IsSolved())
            {
                _finished = true;
                _elapsedOnFinish = Time.time - _startTime;
                _statistics.OnGameCompleted(_difficulty, (int)_elapsedOnFinish);
                StatisticsStore.Save(_statistics);
                GameFinished?.Invoke(true);
            }
        }

        /// <summary>一步操作快照,用于撤销。</summary>
        private readonly struct Move
        {
            public readonly int Index;
            public readonly int OldValue;
            public readonly int NewValue;
            public readonly int OldNotes;
            public readonly int NewNotes;

            public Move(int index, int oldValue, int newValue, int oldNotes, int newNotes)
            {
                Index = index;
                OldValue = oldValue;
                NewValue = newValue;
                OldNotes = oldNotes;
                NewNotes = newNotes;
            }
        }
    }
}
