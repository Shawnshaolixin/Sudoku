using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 运行时 UI 构建工具。界面主体已迁移为 Prefab 承载,
    /// 此处仅保留棋盘 81 格等动态内容的生成工具与按钮点击包装。
    /// </summary>
    public static class UiFactory
    {
        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>把 RectTransform 拉伸填满父节点。</summary>
        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>统一按钮点击包装:先播放点击音效,再执行具体逻辑。</summary>
        public static void Wire(Button button, Action onClick)
        {
            button.onClick.AddListener(() =>
            {
                AudioService.PlaySfx("click");
                onClick();
            });
        }
    }
}
