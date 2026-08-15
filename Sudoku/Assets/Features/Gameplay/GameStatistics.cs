using Sudoku.Core;
using UnityEngine;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 本地统计(阶段 A):总局数、完成数、各难度最佳时间(秒)。
    /// 通过 <see cref="StatisticsStore"/> 用 PlayerPrefs + JSON 持久化。
    /// </summary>
    [System.Serializable]
    public class GameStatistics
    {
        public int TotalGames;
        public int CompletedGames;
        public int BestEasySeconds = int.MaxValue;
        public int BestMediumSeconds = int.MaxValue;
        public int BestHardSeconds = int.MaxValue;

        /// <summary>开局时调用(统计总局数)。</summary>
        public void OnGameStarted() => TotalGames++;

        /// <summary>完成一局时调用(统计完成数与最佳时间)。</summary>
        public void OnGameCompleted(Difficulty difficulty, int seconds)
        {
            CompletedGames++;
            switch (difficulty)
            {
                case Difficulty.Easy:
                    if (seconds < BestEasySeconds) BestEasySeconds = seconds;
                    break;
                case Difficulty.Medium:
                    if (seconds < BestMediumSeconds) BestMediumSeconds = seconds;
                    break;
                case Difficulty.Hard:
                    if (seconds < BestHardSeconds) BestHardSeconds = seconds;
                    break;
                // Beginner/Expert/Master 阶段 A 未开放,暂不单独统计
            }
        }

        /// <summary>取某难度的最佳时间(秒);未完成过返回 int.MaxValue。</summary>
        public int BestSecondsFor(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy: return BestEasySeconds;
                case Difficulty.Medium: return BestMediumSeconds;
                case Difficulty.Hard: return BestHardSeconds;
                default: return int.MaxValue;
            }
        }
    }

    /// <summary>统计数据的本地持久化(PlayerPrefs + JSON)。</summary>
    public static class StatisticsStore
    {
        private const string Key = "sudoku.statistics.v1";

        public static GameStatistics Load()
        {
            string json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return new GameStatistics();
            return JsonUtility.FromJson<GameStatistics>(json) ?? new GameStatistics();
        }

        public static void Save(GameStatistics stats)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(stats));
            PlayerPrefs.Save();
        }

        public static void Reset() => PlayerPrefs.DeleteKey(Key);
    }
}
