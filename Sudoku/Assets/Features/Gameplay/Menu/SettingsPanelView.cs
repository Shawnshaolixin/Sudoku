using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 设置面板(模态弹窗):音效 / 震动 / 背景音乐 / 错误检测开关,购买去广告,恢复购买,清除数据,隐私政策。
    /// 由主菜单通过 FindFirstObjectByType 找到并调用 Show/Hide。
    /// </summary>
    public sealed class SettingsPanelView : MonoBehaviour
    {
        // TODO(阶段 C):替换为真实部署的隐私政策 URL。
        private const string PrivacyPolicyUrl = "https://example.com/sudoku/privacy.html";

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
            _overlay.AddComponent<Image>().color = Theme.OverlayDim;

            var panel = UiFactory.CreateRect("Panel", _overlay.transform);
            var panelRt = panel;
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(620, 800);
            panelRt.anchoredPosition = Vector2.zero;
            panel.gameObject.AddComponent<Image>().color = Theme.Panel;

            var layout = UiFactory.Vertical(panel, 16f, TextAnchor.UpperCenter);
            layout.padding = new RectOffset(28, 28, 28, 28);

            var title = UiFactory.CreateText("Title", panel, 36, TextAnchor.MiddleCenter, Theme.Text);
            title.text = Localization.T("settings.title");

            AddToggleButton(panel, Localization.T("settings.sound"), () => SettingsService.Sound, v => SettingsService.Sound = v);
            AddToggleButton(panel, Localization.T("settings.vibration"), () => SettingsService.Vibration, v => SettingsService.Vibration = v);
            AddToggleButton(panel, Localization.T("settings.music"), () => SettingsService.Music, v =>
            {
                SettingsService.Music = v;
                if (v) AudioService.PlayBgm("bgm");   // 重新开启时恢复播放
                else AudioService.StopBgm();
            });
            AddToggleButton(panel, Localization.T("settings.mistakes"), () => SettingsService.ShowMistakes, v => SettingsService.ShowMistakes = v);

            UiFactory.CreateButton("ClearStats", panel, Localization.T("settings.clearStats"), Theme.Secondary, () => ClearStats(), 400, 60);
            UiFactory.CreateButton("DeleteData", panel, Localization.T("settings.deleteData"), Theme.Secondary, () => DeleteData(), 400, 60);
            UiFactory.CreateButton("BuyRemoveAds", panel, Localization.T("settings.buyRemoveAds"), Theme.Secondary, () => BuyRemoveAds(), 400, 60);
            UiFactory.CreateButton("RestorePurchase", panel, Localization.T("settings.restore"), Theme.Secondary, () => RestorePurchase(), 400, 60);
            UiFactory.CreateButton("Privacy", panel, Localization.T("settings.privacy"), Theme.Secondary, () => Application.OpenURL(PrivacyPolicyUrl), 400, 60);

            _feedbackText = UiFactory.CreateText("Feedback", panel, 20, TextAnchor.MiddleCenter, Theme.Feedback);
            _feedbackText.text = "";

            UiFactory.CreateButton("Close", panel, Localization.T("settings.close"), Theme.Primary, () => Hide(), 400, 64);

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
            image.color = Theme.Secondary;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var text = UiFactory.CreateText("Label", rt, 24, TextAnchor.MiddleCenter, Theme.Text);
            UiFactory.Stretch(text.rectTransform);

            Action refresh = () => text.text = $"{label}: {(get() ? Localization.T("common.on") : Localization.T("common.off"))}";
            button.onClick.AddListener(() => { set(!get()); refresh(); });
            refresh();
        }

        private void ClearStats()
        {
            StatisticsStore.Reset();
            if (_feedbackText != null) _feedbackText.text = Localization.T("settings.feedback.cleared");
        }

        private void DeleteData()
        {
            SettingsService.DeleteAllLocalData();
            if (_feedbackText != null) _feedbackText.text = Localization.T("settings.feedback.deleted");
        }

        private void BuyRemoveAds()
        {
            Services.Iap.BuyRemoveAds();
            if (_feedbackText != null) _feedbackText.text = Localization.T("settings.feedback.bought");
        }

        private void RestorePurchase()
        {
            Services.Iap.RestorePurchases();
            if (_feedbackText != null) _feedbackText.text = Localization.T("settings.feedback.restored");
        }
    }
}
