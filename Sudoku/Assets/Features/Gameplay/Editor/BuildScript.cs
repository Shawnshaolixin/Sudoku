using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Sudoku.Gameplay.Editor
{
    /// <summary>
    /// 命令行打包脚本:供 Unity 的 -executeMethod 调用。
    /// 用法: Unity.exe -quit -batchmode -projectPath <path> -buildTarget Android -executeMethod Sudoku.Gameplay.Editor.BuildScript.BuildAndroid
    /// </summary>
    public static class BuildScript
    {
        public static void BuildAndroid()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[]
                {
                    "Assets/App/Scenes/Menu.unity",
                    "Assets/App/Scenes/Gameplay.unity"
                },
                locationPathName = "Build/Sudoku.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
                EditorApplication.Exit(0);
            else
                EditorApplication.Exit(1);
        }
    }
}