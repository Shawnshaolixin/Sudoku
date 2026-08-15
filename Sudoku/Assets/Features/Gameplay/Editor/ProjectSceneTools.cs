using System.Linq;
using UnityEditor;

namespace Sudoku.Gameplay.Editor
{
    /// <summary>项目级场景工具:注册到 Build Settings、逐级创建目录。</summary>
    internal static class ProjectSceneTools
    {
        /// <summary>把场景路径加入 Build Settings(用于 SceneManager.LoadScene)。</summary>
        public static void EnsureInBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>逐级创建形如 "Assets/App/Scenes" 的目录。</summary>
        public static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = "";
            for (int i = 0; i < parts.Length; i++)
            {
                string parent = current;
                current = i == 0 ? parts[0] : current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(current))
                    AssetDatabase.CreateFolder(string.IsNullOrEmpty(parent) ? "Assets" : parent, parts[i]);
            }
        }
    }
}
