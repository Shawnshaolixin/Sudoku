using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sudoku.Gameplay.Editor
{
    /// <summary>
    /// 一键生成主菜单场景。菜单:Sudoku → Create Main Menu Scene。
    /// 生成 Assets/App/Scenes/Menu.unity,并注册到 Build Settings。
    /// 场景内容为 UI Prefab 实例,需先执行 Sudoku → UI → Build UI Prefabs。
    /// </summary>
    public static class MenuSceneBuilder
    {
        /// <summary>从 UI Prefab 实例化界面;Prefab 缺失时提示先构建。</summary>
        private static bool InstantiatePrefab(Transform parent, string prefabPath, string goName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"缺少 UI Prefab:{prefabPath},请先执行 Sudoku → UI → 2. Build UI Prefabs。");
                if (!Application.isBatchMode)
                    EditorUtility.DisplayDialog("Sudoku", $"缺少 UI Prefab:{prefabPath}\n请先执行 Sudoku → UI → 2. Build UI Prefabs。", "好的");
                return false;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = goName;
            return true;
        }

        [MenuItem("Sudoku/Create Main Menu Scene")]
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Canvas
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }

            // 主菜单(实例化 UI Prefab;先执行 Sudoku → UI → Build UI Prefabs)
            if (!InstantiatePrefab(canvasGo.transform, UiPrefabBuilder.MainMenuPrefabPath, "MainMenuView")) return;

            // 设置面板(主菜单通过 FindFirstObjectByType 找到并 Show/Hide)
            if (!InstantiatePrefab(canvasGo.transform, UiPrefabBuilder.SettingsPrefabPath, "SettingsPanelView")) return;

            // 保存 + 注册
            const string dir = "Assets/App/Scenes";
            ProjectSceneTools.EnsureFolder(dir);
            const string path = dir + "/Menu.unity";
            EditorSceneManager.SaveScene(scene, path);
            ProjectSceneTools.EnsureInBuildSettings(path);
            AssetDatabase.Refresh();

            Debug.Log($"主菜单场景已生成:{path}");
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Sudoku", "主菜单场景已保存到 Assets/App/Scenes/Menu.unity。\n请再执行 Sudoku → Create Gameplay Scene 生成对局场景。", "好的");
        }
    }
}
