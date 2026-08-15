using System;
using System.Diagnostics;
using Sudoku.Core;

/// <summary>
/// 独立校验程序:在 Unity 之外直接编译 Sudoku.Core 并运行断言,
/// 用于在进入 Unity 前验证核心算法(求解/生成/唯一解/逻辑单步/提示/难度)。
/// </summary>
internal static class Program
{
    private static int _failed;

    private static void Check(bool condition, string name)
    {
        if (condition) Console.WriteLine($"  [PASS] {name}");
        else { Console.WriteLine($"  [FAIL] {name}"); _failed++; }
    }

    private static SudokuBoard FromString(params string[] rows)
    {
        var cells = new int[SudokuBoard.CellCount];
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
            {
                char ch = rows[r][c];
                cells[SudokuBoard.Index(r, c)] = (ch >= '1' && ch <= '9') ? (ch - '0') : 0;
            }
        return new SudokuBoard(cells);
    }

    private static readonly string[] PuzzleRows =
    {
        "53..7....", "6..195...", ".98....6.", "8...6...3", "4..8.3..1",
        "7...2...6", ".6....28.", "...419..5", "....8..79"
    };

    private static readonly string[] SolutionRows =
    {
        "534678912", "672195348", "198342567", "859761423", "426853791",
        "713924856", "961537284", "287419635", "345286179"
    };

    private static int Main()
    {
        Console.WriteLine("== SudokuCore 独立校验 ==");
        var puzzle = FromString(PuzzleRows);
        var solution = FromString(SolutionRows);

        // 1) 求解器
        Console.WriteLine("[求解器]");
        Check(SudokuSolver.Solve(puzzle, out var solved) && solved.Equals(solution), "求解经典谜题与已知解一致");
        Check(SudokuSolver.Solve(new SudokuBoard(), out var emptySolved) && emptySolved.IsSolved(), "空棋盘可解且为合法终盘");
        Check(SudokuSolver.CountSolutions(puzzle, 2) == 1, "经典谜题解数为 1");
        Check(SudokuSolver.HasUniqueSolution(puzzle), "经典谜题唯一解");
        Check(!SudokuSolver.HasUniqueSolution(new SudokuBoard()), "空棋盘非唯一解");
        Check(SudokuSolver.CountSolutions(new SudokuBoard(), 2) == 2, "空棋盘计数命中上限");
        var conflict = new SudokuBoard(); conflict[0, 0] = 5; conflict[0, 1] = 5;
        Check(!SudokuSolver.Solve(conflict, out _) && SudokuSolver.CountSolutions(conflict, 2) == 0, "冲突盘面无解");

        // 2) 逻辑求解器
        Console.WriteLine("[逻辑求解器]");
        var naked = FromString("123456780", "000000000", "000000000", "000000000", "000000000",
                               "000000000", "000000000", "000000000", "000000000");
        Check(LogicSolver.TryFindSingle(naked, out int ni, out int nv, out var nt)
              && nt == Technique.NakedSingle && ni == 8 && nv == 9, "显性唯一(0,8)=9");

        var hidden = FromString("123456000", "000000000", "000000000", "000000070", "000000000",
                                "000000000", "000000007", "000000000", "000000000");
        Check(LogicSolver.TryFindSingle(hidden, out int hi, out int hv, out var ht)
              && ht == Technique.HiddenSingle && hi == SudokuBoard.Index(0, 6) && hv == 7, "隐性唯一(0,6)=7");
        Check(!LogicSolver.TryFindSingle(solution, out _, out _, out _), "已解棋盘无逻辑单步");

        // 3) 难度评分
        Console.WriteLine("[难度评分]");
        Check(DifficultyRater.Rate(new SudokuBoard()) == Difficulty.Master, "空棋盘评分 Master");
        Check(DifficultyRater.Rate(solution) == Difficulty.Beginner, "终盘评分 Beginner");
        Check((int)DifficultyRater.Rate(puzzle) >= (int)Difficulty.Medium, "经典谜题评分 >= Medium");

        // 4) 提示引擎
        Console.WriteLine("[提示引擎]");
        Check(HintEngine.GetHint(puzzle, out var hint)
              && hint.Value == solution[hint.Row, hint.Col]
              && puzzle.IsValidPlacement(hint.Row, hint.Col, hint.Value), "提示为合法且正确的解格");
        Check(!HintEngine.GetHint(solution, out _), "已解棋盘无提示");

        // 5) 生成器
        Console.WriteLine("[生成器]");
        var sw = Stopwatch.StartNew();
        var g = new SudokuGenerator(20240512);
        bool allUnique = true, allSolvedMatch = true, allValidSubset = true, allSolvedBoardsValid = true;
        for (int i = 0; i < 30; i++)
            if (!g.GenerateSolvedBoard().IsSolved()) allSolvedBoardsValid = false;
        foreach (Difficulty d in new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard })
        {
            var ds = Stopwatch.StartNew();
            for (int i = 0; i < 3; i++)
            {
                var p = g.Generate(d);
                if (!SudokuSolver.HasUniqueSolution(p.Puzzle)) allUnique = false;
                if (!SudokuSolver.Solve(p.Puzzle, out var s) || !s.Equals(p.Solution)) allSolvedMatch = false;
                for (int idx = 0; idx < SudokuBoard.CellCount; idx++)
                    if (p.Puzzle[idx] != 0 && p.Puzzle[idx] != p.Solution[idx]) allValidSubset = false;
            }
            ds.Stop();
            Console.WriteLine($"  ({d} 3 个谜题耗时 {ds.ElapsedMilliseconds} ms)");
        }
        sw.Stop();
        Check(allSolvedBoardsValid, "30 个随机终盘均合法");
        Check(allUnique, "9 个谜题均唯一解");
        Check(allSolvedMatch, "谜题求解与生成解一致");
        Check(allValidSubset, "谜题给定数均为解的子集");
        Console.WriteLine($"  (总生成耗时 {sw.ElapsedMilliseconds} ms)");

        var g2 = new SudokuGenerator(7);
        Check(g2.Generate(Difficulty.Easy).ClueCount > g2.Generate(Difficulty.Hard).ClueCount, "Easy 提示数多于 Hard");
        Check(new SudokuGenerator(99).Generate(Difficulty.Medium).Puzzle.Equals(
              new SudokuGenerator(99).Generate(Difficulty.Medium).Puzzle), "同种子结果确定");

        Console.WriteLine(_failed == 0 ? "== ALL PASS ==" : $"== {_failed} FAILED ==");
        return _failed == 0 ? 0 : 1;
    }
}
