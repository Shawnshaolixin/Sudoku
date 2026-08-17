using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sudoku.Gameplay.Editor
{
    /// <summary>
    /// 一键生成对局场景。菜单:Sudoku → Create Gameplay Scene。
    /// 生成 Assets/App/Scenes/Gameplay.unity,并注册到 Build Settings。
    /// 场景含:控制器、棋盘视图、新手引导(均为 UI Prefab 实例)。
    /// </summary>
    public static class GameplaySceneBuilder
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

        [MenuItem("Sudoku/Create Gameplay Scene")]
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 1) Canvas(UGUI 根;显式带上 RectTransform,避免 Canvas 缺失矩形变换)
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // 2) EventSystem(UGUI 输入事件必需)
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }

            // 3) 对局控制器(纯逻辑,不挂 UI)
            new GameObject("SudokuGameController").AddComponent<SudokuGameController>();

            // 4) 棋盘视图 = UI Prefab 实例(棋盘 81 格由运行时填充;先执行 Sudoku → UI → Build UI Prefabs)
            if (!InstantiatePrefab(canvasGo.transform, UiPrefabBuilder.GameplayPrefabPath, "SudokuBoardView")) return;

            // 5) 新手引导(放在视图之后,层级更高,弹窗能盖住棋盘)
            if (!InstantiatePrefab(canvasGo.transform, UiPrefabBuilder.OnboardingPrefabPath, "OnboardingView")) return;

            // 6) 保存 + 注册
            const string dir = "Assets/App/Scenes";
            ProjectSceneTools.EnsureFolder(dir);
            const string path = dir + "/Gameplay.unity";
            EditorSceneManager.SaveScene(scene, path);
            ProjectSceneTools.EnsureInBuildSettings(path);
            AssetDatabase.Refresh();

            Debug.Log($"对局场景已生成:{path}");
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Sudoku", "对局场景已保存到 Assets/App/Scenes/Gameplay.unity。", "好的");
        }
    }
}
