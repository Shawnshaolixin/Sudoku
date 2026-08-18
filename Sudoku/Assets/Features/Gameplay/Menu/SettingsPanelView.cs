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
        [SerializeField] private Sprite _trackOnSprite;  // 开:彩色轨道
        [SerializeField] private Sprite _trackOffSprite; // 关:灰色轨道

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
            public Button Button;      // 开关整体(点击切换)
            public Text Label;         // 左侧名称
            public Image Track;        // 轨道(灰=关,彩色=开)
            public RectTransform Knob; // 把手(靠左=关,靠右=开)
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

        /// <summary>一个「真 Switch」:名称固定,状态由轨道换图 + 把手滑动呈现,点击切换。</summary>
        private void WireToggle(ToggleEntry entry, string label, Func<bool> get, Action<bool> set)
        {
            if (entry.Label != null) entry.Label.text = label;

            Action refresh = () =>
            {
                bool on = get();
                if (entry.Track != null && _trackOnSprite != null && _trackOffSprite != null)
                    entry.Track.sprite = on ? _trackOnSprite : _trackOffSprite;
                if (entry.Knob != null)
                {
                    entry.Knob.anchorMin = new Vector2(on ? 1f : 0f, 0.5f);
                    entry.Knob.anchorMax = new Vector2(on ? 1f : 0f, 0.5f);
                    entry.Knob.anchoredPosition = new Vector2(on ? -2f : 2f, 0f);
                }
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
