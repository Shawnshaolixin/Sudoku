using UnityEngine;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 全局设置(阶段 B):音效、震动、错误检测、新手引导完成标记。
    /// 使用 PlayerPrefs 持久化;正式版可替换为 JSON 存档或接入远程配置。
    /// </summary>
    public static class SettingsService
    {
        private const string KeySound = "sudoku.settings.sound";
        private const string KeyVibration = "sudoku.settings.vibration";
        private const string KeyMistakes = "sudoku.settings.showMistakes";
        private const string KeyOnboarding = "sudoku.settings.onboardingDone";

        public static bool Sound
        {
            get => PlayerPrefs.GetInt(KeySound, 1) == 1;
            set => SetInt(KeySound, value);
        }

        public static bool Vibration
        {
            get => PlayerPrefs.GetInt(KeyVibration, 1) == 1;
            set => SetInt(KeyVibration, value);
        }

        public static bool ShowMistakes
        {
            get => PlayerPrefs.GetInt(KeyMistakes, 1) == 1;
            set => SetInt(KeyMistakes, value);
        }

        public static bool OnboardingCompleted
        {
            get => PlayerPrefs.GetInt(KeyOnboarding, 0) == 1;
            set => SetInt(KeyOnboarding, value);
        }

        /// <summary>删除所有本地数据(设置 + 统计),对应「数据删除」入口。</summary>
        public static void DeleteAllLocalData()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        private static void SetInt(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
