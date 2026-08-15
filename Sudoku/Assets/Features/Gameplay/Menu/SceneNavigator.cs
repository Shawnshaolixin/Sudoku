using Sudoku.Core;
using UnityEngine.SceneManagement;

namespace Sudoku.Gameplay
{
    /// <summary>场景导航 + 跨场景传递所选难度。</summary>
    public static class SceneNavigator
    {
        public const string MenuScene = "Menu";
        public const string GameplayScene = "Gameplay";

        private static Difficulty _selectedDifficulty = Difficulty.Easy;

        /// <summary>当前所选难度(主菜单设置,对局场景在 Awake 时读取)。</summary>
        public static Difficulty SelectedDifficulty => _selectedDifficulty;

        public static void LoadMenu()
        {
            SceneManager.LoadScene(MenuScene);
        }

        public static void LoadGameplay(Difficulty difficulty)
        {
            _selectedDifficulty = difficulty;
            SceneManager.LoadScene(GameplayScene);
        }
    }
}
