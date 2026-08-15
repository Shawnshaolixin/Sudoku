using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 运行时 UGUI 构建工具:统一字体绑定、尺寸与布局配置,避免各视图重复实现。
    /// 阶段 A/B 使用 UGUI Text + 内置字体(开箱即用),正式版可整体替换为 TMP。
    /// </summary>
    public static class UiFactory
    {
        private static Font _defaultFont;

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

        public static Text CreateText(string name, Transform parent, int fontSize, TextAnchor anchor, Color color)
        {
            var rt = CreateRect(name, parent);
            var text = rt.gameObject.AddComponent<Text>();
            // 关键:AddComponent<Text>() 不会自动绑定字体,必须显式赋值。
            text.font = GetDefaultFont();
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false; // 文本不拦截点击
            return text;
        }

        public static Image CreateImage(string name, Transform parent, Color color)
        {
            var rt = CreateRect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        /// <summary>创建带背景图 + 文本标签的按钮(尺寸由 LayoutElement 指定)。</summary>
        public static Button CreateButton(string name, Transform parent, string label, Color color, Action onClick, float width, float height)
        {
            var rt = CreateRect(name, parent);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;

            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());

            var text = CreateText("Label", rt, 26, TextAnchor.MiddleCenter, new Color(0.1f, 0.1f, 0.15f, 1f));
            Stretch(text.rectTransform);
            text.text = label;
            return button;
        }

        /// <summary>水平布局(子元素保持 preferred 尺寸,居中)。</summary>
        public static HorizontalLayoutGroup Horizontal(RectTransform rt, float spacing, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = align;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        /// <summary>垂直布局(子元素保持 preferred 尺寸,水平居中)。</summary>
        public static VerticalLayoutGroup Vertical(RectTransform rt, float spacing, TextAnchor align = TextAnchor.UpperCenter)
        {
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = align;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        private static Font GetDefaultFont()
        {
            if (_defaultFont == null)
            {
                _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_defaultFont == null)
                    _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); // 兼容旧版本
            }
            return _defaultFont;
        }
    }
}
