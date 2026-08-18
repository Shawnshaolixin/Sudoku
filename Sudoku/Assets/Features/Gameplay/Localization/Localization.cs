using System.Collections.Generic;
using UnityEngine;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 轻量本地化:把所有 UI 文案集中到这里,用 T(key) / F(key, args) 取文案。
    /// 首版支持英文(默认,面向海外市场)和中文;以后加语言只需扩展表,不需要改 UI 代码。
    /// 说明:这是轻量方案,正式多语言(10+ 语言、复数/性别等规则)建议后续接入 I2 Localization。
    /// </summary>
    public static class Localization
    {
        public enum Language
        {
            English = 0,
            Chinese = 1
        }

        /// <summary>当前语言(默认英文,面向海外市场)。</summary>
        public static Language Current { get; set; } = Language.English;

        private struct Entry
        {
            public readonly string En;
            public readonly string Zh;
            public Entry(string en, string zh) { En = en; Zh = zh; }
        }

        private static readonly Dictionary<string, Entry> Table = new Dictionary<string, Entry>
        {
            // 通用
            { "common.on", new Entry("On", "开") },
            { "common.off", new Entry("Off", "关") },

            // 难度
            { "difficulty.beginner", new Entry("Beginner", "入门") },
            { "difficulty.easy", new Entry("Easy", "简单") },
            { "difficulty.medium", new Entry("Medium", "中等") },
            { "difficulty.hard", new Entry("Hard", "困难") },
            { "difficulty.expert", new Entry("Expert", "专家") },
            { "difficulty.master", new Entry("Master", "大师") },

            // 主菜单
            { "menu.title", new Entry("Sudoku", "数独") },
            { "menu.subtitle", new Entry("Train your brain, one puzzle a day", "锻炼脑力,每天一局") },
            { "menu.chooseDifficulty", new Entry("Choose difficulty", "选择难度") },
            { "menu.start", new Entry("Start", "开始游戏") },
            { "menu.settings", new Entry("Settings", "设置") },
            { "menu.stats", new Entry("Games {0} · Solved {1}", "总局 {0} · 完成 {1}") },

            // 对局
            { "game.status", new Entry("{0}   {1}   Hints {2}", "{0}   {1}   提示 {2}") },
            { "game.mode.number", new Entry("Number mode", "数字模式") },
            { "game.mode.note", new Entry("Note mode", "笔记模式") },
            { "game.undo", new Entry("Undo", "撤销") },
            { "game.hint", new Entry("Hint", "提示") },
            { "game.menu", new Entry("Menu", "菜单") },
            { "game.win", new Entry("Solved! Time {0}", "完成!用时 {0}") },
            { "game.stats", new Entry("Games {0} · Solved {1} · Best {2}", "总局 {0} · 完成 {1} · 最佳 {2}") },

            // 结算弹窗
            { "result.title", new Entry("Solved!", "完成!") },
            { "result.perfect", new Entry("Flawless", "无错误") },
            { "result.time", new Entry("Time", "用时") },
            { "result.best", new Entry("Personal Best", "个人最佳") },
            { "result.hints", new Entry("Hints Used", "提示使用") },
            { "result.newRecord", new Entry("New record!", "新纪录!") },
            { "result.next", new Entry("Next", "下一局") },
            { "result.home", new Entry("Home", "主页") },

            // 设置
            { "settings.title", new Entry("Settings", "设置") },
            { "settings.sound", new Entry("Sound", "音效") },
            { "settings.vibration", new Entry("Vibration", "震动") },
            { "settings.mistakes", new Entry("Show mistakes", "错误检测") },
            { "settings.music", new Entry("Music", "背景音乐") },
            { "settings.clearStats", new Entry("Clear progress", "清除游戏进度") },
            { "settings.deleteData", new Entry("Delete all data", "删除所有本地数据") },
            { "settings.buyRemoveAds", new Entry("Remove Ads", "购买去广告") },
            { "settings.restore", new Entry("Restore Purchase", "恢复购买") },
            { "settings.privacy", new Entry("Privacy Policy", "隐私政策") },
            { "settings.close", new Entry("Close", "关闭") },
            { "settings.feedback.cleared", new Entry("Progress cleared", "已清除游戏进度") },
            { "settings.feedback.deleted", new Entry("All data deleted", "已删除所有本地数据") },
            { "settings.feedback.bought", new Entry("Remove Ads purchased", "已购买去广告") },
            { "settings.feedback.restored", new Entry("Restore requested", "已请求恢复购买") },

            // 新手引导
            { "onboarding.title", new Entry("How to play", "怎么玩") },
            { "onboarding.step1", new Entry("Fill each row, column and 3×3 box with the digits 1-9, with no repeats.", "数独规则:在 9×9 棋盘里,让每一行、每一列、每个 3×3 宫都包含 1~9,且不重复。") },
            { "onboarding.step2", new Entry("Tap an empty cell to select it, then tap a number below to fill it in.", "点击任意空格选中它,再用下方数字键盘填入数字。") },
            { "onboarding.step3", new Entry("Tap the ＋ button to switch to note mode and jot down candidates.", "点数字键盘的「＋」可切换笔记模式,记录候选数。") },
            { "onboarding.step4", new Entry("Selecting a number highlights its row, column, box and identical numbers to help you eliminate.", "选中一个数字时,同行/列/宫和相同数字会高亮,帮你排除。") },
            { "onboarding.step5", new Entry("Stuck? Tap Hint to fill in a correct number. Tap Start when ready!", "卡住时点「提示」,会帮你填入一个正确的数字。准备好了就点「开始」!") },
            { "onboarding.skip", new Entry("Skip", "跳过") },
            { "onboarding.next", new Entry("Next", "下一步") },
            { "onboarding.start", new Entry("Start", "开始") },
        };

        /// <summary>
        /// 遍历全部文案(英中双语)。编辑器工具用它收集字符集,供 TMP 字体烘焙裁剪。
        /// </summary>
        public static IEnumerable<string> AllStrings()
        {
            foreach (var e in Table.Values)
            {
                yield return e.En;
                yield return e.Zh;
            }
        }

        /// <summary>取文案;找不到 key 时返回 key 本身(便于发现漏配)。</summary>
        public static string T(string key)
        {
            if (Table.TryGetValue(key, out var e))
                return Current == Language.English ? e.En : e.Zh;
            Debug.LogWarning($"[Localization] 缺少文案 key:{key}");
            return key;
        }

        /// <summary>取带参数的文案(如 "总局 {0} · 完成 {1}")。</summary>
        public static string F(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }
    }
}
