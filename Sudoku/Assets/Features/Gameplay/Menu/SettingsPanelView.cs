using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 设置面板(模态弹窗):音效 / 震动 / 背景音乐 / 错误检测开关,购买去广告,恢复购买,清除数据,隐私政策。
    /// 由主菜单通过 FindFirstObjectByType 找到并调用 Show/Hide。
    /// UI 层级由 Prefab 承载(UiPrefabBuilder 生成),这里只绑定逻辑与文案。
    /// </summary>
    public sealed class SettingsPanelView : MonoBehaviour
    {
        // TODO(阶段 C):替换为真实部署的隐私政策 URL。
        private const string PrivacyPolicyUrl = "https://example.com/sudoku/privacy.html";

        [Header("弹窗")]
        [SerializeField] private GameObject _overlay;
        [SerializeField] private Text _title;
        [SerializeField] private Text _feedbackText;

        [Header("开关(顺序固定:Sound / Vibration / Music / Mistakes)")]
        [SerializeField] private ToggleEntry _soundToggle;
        [SerializeField] private ToggleEntry _vibrationToggle;
        [SerializeField] private ToggleEntry _musicToggle;
        [SerializeField] private ToggleEntry _mistakesToggle;

        [Header("动作按钮")]
        [SerializeField] private Button _clearStatsButton;
        [SerializeField] private Button _deleteDataButton;
        [SerializeField] private Button _buyRemoveAdsButton;
        [SerializeField] private Button _restoreButton;
        [SerializeField] private Button _privacyButton;
        [SerializeField] private Button _closeButton;

        [Serializable]
        private struct ToggleEntry
        {
            public Button Button;
            public Text Label;
        }

        private void Awake()
        {
            WireUi();
        }

        private void WireUi()
        {
            _title.text = Localization.T("settings.title");

            WireToggle(_soundToggle, Localization.T("settings.sound"),
                () => SettingsService.Sound, v => SettingsService.Sound = v);
            WireToggle(_vibrationToggle, Localization.T("settings.vibration"),
                () => SettingsService.Vibration, v => SettingsService.Vibration = v);
            WireToggle(_musicToggle, Localization.T("settings.music"),
                () => SettingsService.Music, v =>
                {
                    SettingsService.Music = v;
                    if (v) AudioService.PlayBgm("bgm");   // 重新开启时恢复播放
                    else AudioService.StopBgm();
                });
            WireToggle(_mistakesToggle, Localization.T("settings.mistakes"),
                () => SettingsService.ShowMistakes, v => SettingsService.ShowMistakes = v);

            UiFactory.Wire(_clearStatsButton, ClearStats);
            UiFactory.Wire(_deleteDataButton, DeleteData);
            UiFactory.Wire(_buyRemoveAdsButton, BuyRemoveAds);
            UiFactory.Wire(_restoreButton, RestorePurchase);
            UiFactory.Wire(_privacyButton, () => Application.OpenURL(PrivacyPolicyUrl));
            UiFactory.Wire(_closeButton, Hide);
        }

        public void Show()
        {
            if (_overlay != null) _overlay.SetActive(true);
        }

        public void Hide()
        {
            if (_overlay != null) _overlay.SetActive(false);
        }

        /// <summary>一个「开关」按钮:label 显示当前状态,点击切换。</summary>
        private void WireToggle(ToggleEntry entry, string label, Func<bool> get, Action<bool> set)
        {
            Action refresh = () =>
            {
                if (entry.Label != null)
                    entry.Label.text = $"{label}: {(get() ? Localization.T("common.on") : Localization.T("common.off"))}";
            };
            UiFactory.Wire(entry.Button, () =>
            {
                set(!get());
                refresh();
            });
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
