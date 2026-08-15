using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sudoku.Gameplay.Editor
{
    /// <summary>
    /// 一键生成可运行的对局场景,免去手摆 UI 与手动挂引用。
    /// 菜单:Sudoku → Create Gameplay Scene。
    /// 生成的场景保存到 Assets/App/Scenes/Gameplay.unity,进入 Play 模式即可游玩。
    /// </summary>
    public static class GameplaySceneBuilder
    {
        [MenuItem("Sudoku/Create Gameplay Scene")]
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 1) Canvas(UGUI 根)
            var canvasGo = new GameObject("Canvas");
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

            // 4) 棋盘视图(运行时自建 UI,并在 Awake 里自寻控制器)
            var viewGo = new GameObject("SudokuBoardView", typeof(RectTransform));
            viewGo.transform.SetParent(canvasGo.transform, false);
            viewGo.AddComponent<SudokuBoardView>();

            // 5) 保存场景
            const string dir = "Assets/App/Scenes";
            EnsureFolder(dir);
            const string path = dir + "/Gameplay.unity";
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.Refresh();

            Debug.Log($"对局场景已生成:{path}");
            EditorUtility.DisplayDialog("Sudoku", "场景已保存到 Assets/App/Scenes/Gameplay.unity。\n进入 Play 模式即可游玩。", "好的");
        }

        private static void EnsureFolder(string path)
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
