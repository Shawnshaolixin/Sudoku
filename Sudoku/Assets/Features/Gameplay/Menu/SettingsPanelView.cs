using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 设置面板(模态弹窗):音效 / 震动 / 错误检测开关,清除数据,隐私政策入口。
    /// 由主菜单通过 FindFirstObjectByType 找到并调用 Show/Hide。
    /// </summary>
    public sealed class SettingsPanelView : MonoBehaviour
    {
        // TODO(阶段 C):替换为真实部署的隐私政策 URL。
        private const string PrivacyPolicyUrl = "https://example.com/sudoku/privacy.html";

        [Header("配色")]
        [SerializeField] private Color _primaryColor = new Color(0.35f, 0.55f, 0.95f, 1f);
        [SerializeField] private Color _secondaryColor = new Color(0.90f, 0.92f, 1f, 1f);
        [SerializeField] private Color _textColor = new Color(0.12f, 0.12f, 0.18f, 1f);

        private GameObject _overlay;
        private Text _feedbackText;

        private void Awake()
        {
            UiFactory.Stretch((RectTransform)transform);
        }

        public void Show()
        {
            if (_overlay == null) BuildOverlay();
            _overlay.SetActive(true);
        }

        public void Hide()
        {
            if (_overlay != null) _overlay.SetActive(false);
        }

        private void BuildOverlay()
        {
            var overlayRt = UiFactory.CreateRect("Overlay", transform);
            _overlay = overlayRt.gameObject;
            UiFactory.Stretch(overlayRt);
            var dim = _overlay.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);

            var panel = UiFactory.CreateRect("Panel", _overlay.transform);
            var panelRt = panel;
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(620, 720);
            panelRt.anchoredPosition = Vector2.zero;
            panel.gameObject.AddComponent<Image>().color = new Color(0.98f, 0.98f, 1f, 1f);

            var layout = UiFactory.Vertical(panel, 16f, TextAnchor.UpperCenter);
            layout.padding = new RectOffset(28, 28, 28, 28);

            var title = UiFactory.CreateText("Title", panel, 36, TextAnchor.MiddleCenter, _textColor);
            title.text = "设置";

            AddToggleButton(panel, "音效", () => SettingsService.Sound, v => SettingsService.Sound = v);
            AddToggleButton(panel, "震动", () => SettingsService.Vibration, v => SettingsService.Vibration = v);
            AddToggleButton(panel, "错误检测", () => SettingsService.ShowMistakes, v => SettingsService.ShowMistakes = v);

            UiFactory.CreateButton("ClearStats", panel, "清除游戏进度", _secondaryColor, () => ClearStats(), 400, 60);
            UiFactory.CreateButton("DeleteData", panel, "删除所有本地数据", _secondaryColor, () => DeleteData(), 400, 60);
            UiFactory.CreateButton("Privacy", panel, "隐私政策", _secondaryColor, () => Application.OpenURL(PrivacyPolicyUrl), 400, 60);

            _feedbackText = UiFactory.CreateText("Feedback", panel, 20, TextAnchor.MiddleCenter, new Color(0.65f, 0.30f, 0.30f, 1f));
            _feedbackText.text = "";

            UiFactory.CreateButton("Close", panel, "关闭", _primaryColor, () => Hide(), 400, 64);

            _overlay.SetActive(false);
        }

        /// <summary>一个「开关」按钮:label 显示当前状态,点击切换。</summary>
        private void AddToggleButton(Transform parent, string label, Func<bool> get, Action<bool> set)
        {
            var rt = UiFactory.CreateRect($"Toggle_{label}", parent);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 400;
            le.preferredHeight = 60;

            var image = rt.gameObject.AddComponent<Image>();
            image.color = _secondaryColor;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var text = UiFactory.CreateText("Label", rt, 24, TextAnchor.MiddleCenter, _textColor);
            UiFactory.Stretch(text.rectTransform);

            Action refresh = () => text.text = $"{label}: {(get() ? "开" : "关")}";
            button.onClick.AddListener(() => { set(!get()); refresh(); });
            refresh();
        }

        private void ClearStats()
        {
            StatisticsStore.Reset();
            if (_feedbackText != null) _feedbackText.text = "已清除游戏进度";
        }

        private void DeleteData()
        {
            SettingsService.DeleteAllLocalData();
            if (_feedbackText != null) _feedbackText.text = "已删除所有本地数据";
        }
    }
}
