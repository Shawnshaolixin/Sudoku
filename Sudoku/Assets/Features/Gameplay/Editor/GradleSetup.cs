using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sudoku.EditorTools
{
    /// <summary>
    /// 一次性构建工具：把 Unity 构建用的 Gradle 从自带 7.5.1 切到本地 Gradle 8.6。
    /// 背景：AGP 8.4（修复 Firebase 13.15 的 D8 崩溃）要求 Gradle >= 8.6，
    /// 而 Unity 2022.3 构建时不认 Assets/Plugins/Android/gradle/wrapper 配置，
    /// 官方支持的切换方式是 EditorPrefs（GradleUseEmbedded + GradlePath）。
    /// 用完可保留：切换是幂等的，点几次都没副作用。
    /// </summary>
    public static class GradleSetup
    {
        private const string GradlePath = @"D:\Tools\gradle-8.6";

        [MenuItem("Tools/Sudoku/切换到 Gradle 8.6（AGP 8.4 必需）")]
        public static void SwitchToGradle86()
        {
            if (!Directory.Exists(GradlePath))
            {
                EditorUtility.DisplayDialog(
                    "Gradle 8.6 未找到",
                    $"目录不存在：{GradlePath}\n请先下载并解压 Gradle 8.6 到该路径。",
                    "知道了");
                return;
            }

            EditorPrefs.SetBool("GradleUseEmbedded", false);
            EditorPrefs.SetString("GradlePath", GradlePath);
            Debug.Log($"[GradleSetup] Gradle 已切换到 {GradlePath} (GradleUseEmbedded=false)");
            EditorUtility.DisplayDialog(
                "切换完成",
                "Gradle 已切换到本地 8.6，请重新构建。\n\nJDK 说明：Unity 的 JDK 保持 11 即可，\nJDK 17 已通过 gradleTemplate.properties 的\norg.gradle.java.home 指向 Gradle 守护进程，无需在 Preferences 切换。",
                "好");
        }

        /// <summary>查看当前 Gradle 设置（排查用）。</summary>
        [MenuItem("Tools/Sudoku/查看当前 Gradle 设置")]
        public static void ShowCurrentGradle()
        {
            bool embedded = EditorPrefs.GetBool("GradleUseEmbedded", true);
            string path = EditorPrefs.GetString("GradlePath", "(未设置)");
            Debug.Log($"[GradleSetup] GradleUseEmbedded={embedded}, GradlePath={path}");
        }
    }
}
