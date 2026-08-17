using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sudoku.Gameplay.Editor
{
    /// <summary>
    /// 字体文件保障(菜单:Sudoku → UI → 1. Check Fonts)。
    /// UI 使用 Unity 原生 UGUI Text + 直接引用 MiSans 字体文件,运行时动态渲染字形,
    /// 不需要 SDF 烘焙——此前 TMP 烘焙方案因批处理烘焙图集损坏已废弃,本类同时负责清理历史遗留的 SDF 资产。
    /// </summary>
    public static class FontSetup
    {
        public const string FontsDir = "Assets/App/Prefabs/UI/Assets/Fonts";
        public const string RegularFontPath = FontsDir + "/MiSans-Regular.otf";
        public const string SemiboldFontPath = FontsDir + "/MiSans-Semibold.otf";

        private const string RegularSdfAssetPath = FontsDir + "/MiSans-Regular SDF.asset";
        private const string SemiboldSdfAssetPath = FontsDir + "/MiSans-Semibold SDF.asset";

        [MenuItem("Sudoku/UI/1. Check Fonts")]
        public static void CheckFonts()
        {
            bool ok = EnsureFontFiles();
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Sudoku", ok
                    ? "MiSans 字体文件就绪。\n接着执行 Sudoku → UI → 2. Build UI Prefabs。"
                    : "缺少字体文件,请先放置 MiSans-Regular.otf / MiSans-Semibold.otf 到 " + FontsDir + "。", "好的");
        }

        /// <summary>确认两份字体文件存在;顺带清理历史 TMP 烘焙遗留的 SDF 资产(已废弃,留着会混淆)。</summary>
        public static bool EnsureFontFiles()
        {
            CleanUpSdfAssets();
            bool ok = File.Exists(RegularFontPath) && File.Exists(SemiboldFontPath);
            if (!ok)
                Debug.LogError($"[FontSetup] 缺少字体文件:{RegularFontPath} / {SemiboldFontPath}");
            return ok;
        }

        /// <summary>删除废弃的 SDF 字体资产(防止误引用/混淆)。</summary>
        public static void CleanUpSdfAssets()
        {
            if (File.Exists(RegularSdfAssetPath)) AssetDatabase.DeleteAsset(RegularSdfAssetPath);
            if (File.Exists(SemiboldSdfAssetPath)) AssetDatabase.DeleteAsset(SemiboldSdfAssetPath);
        }

        public static Font LoadRegular() => AssetDatabase.LoadAssetAtPath<Font>(RegularFontPath);
        public static Font LoadSemibold() => AssetDatabase.LoadAssetAtPath<Font>(SemiboldFontPath);
    }
}
