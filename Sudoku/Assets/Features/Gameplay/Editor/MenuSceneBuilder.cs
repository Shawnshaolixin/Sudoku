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
    /// </summary>
    public static class MenuSceneBuilder
    {
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

            // 主菜单
            var menuGo = new GameObject("MainMenuView", typeof(RectTransform));
            menuGo.transform.SetParent(canvasGo.transform, false);
            menuGo.AddComponent<MainMenuView>();

            // 设置面板(主菜单通过 FindFirstObjectByType 找到并 Show/Hide)
            var settingsGo = new GameObject("SettingsPanelView", typeof(RectTransform));
            settingsGo.transform.SetParent(canvasGo.transform, false);
            settingsGo.AddComponent<SettingsPanelView>();

            // 保存 + 注册
            const string dir = "Assets/App/Scenes";
            ProjectSceneTools.EnsureFolder(dir);
            const string path = dir + "/Menu.unity";
            EditorSceneManager.SaveScene(scene, path);
            ProjectSceneTools.EnsureInBuildSettings(path);
            AssetDatabase.Refresh();

            Debug.Log($"主菜单场景已生成:{path}");
            EditorUtility.DisplayDialog("Sudoku", "主菜单场景已保存到 Assets/App/Scenes/Menu.unity。\n请再执行 Sudoku → Create Gameplay Scene 生成对局场景。", "好的");
        }
    }
}
