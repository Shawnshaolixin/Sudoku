// 本文件只有在定义了编译符号 SUDOKU_ADMOB 后才会参与编译。
// 前提:先安装 Google Mobile Ads Unity 插件(本文针对 v8.x,官方测试广告位可直接跑通)。
#if SUDOKU_ADMOB
using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 真实 AdMob 广告服务:激励视频 + UMP 同意管理(GDPR)。
    ///
    /// 完整流程:
    ///   1) UMP 请求同意(GDPR 地区会先弹同意表单);
    ///   2) 初始化 AdMob;
    ///   3) 加载激励视频;
    ///   4) 展示 → 玩家看完 → 回调发放奖励。
    /// </summary>
    public sealed class AdMobAdsService : IAdsService
    {
        // TODO:替换成你在 AdMob 后台创建的真实激励视频广告位 ID。
        // 下面是 Google 官方「测试广告位」,开发阶段可直接使用,不会产生真实收益。
        private const string RewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";

        private const string KeyAdsRemoved = "sudoku.ads.removed";

        private RewardedAd _rewardedAd;
        private Action<bool> _pendingRewardCallback; // 等待回奖的调用方
        private bool _adsRemoved;

        public bool IsInitialized { get; private set; }
        public bool IsRewardedReady => _rewardedAd != null && _rewardedAd.CanShowAd();
        public bool IsAdsRemoved => _adsRemoved;

        public AdMobAdsService()
        {
            _adsRemoved = PlayerPrefs.GetInt(KeyAdsRemoved, 0) == 1;
        }

        public void Initialize()
        {
            RequestConsentThenInit();
        }

        public void SetRemoveAds(bool removed)
        {
            _adsRemoved = removed;
            PlayerPrefs.SetInt(KeyAdsRemoved, removed ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ShowRewardedAd(Action<bool> onReward)
        {
            if (!IsRewardedReady)
            {
                Debug.LogWarning("[AdMob] 激励视频尚未准备好");
                onReward?.Invoke(false);
                return;
            }

            _pendingRewardCallback = onReward;
            _rewardedAd.Show();
        }

        // ---------- UMP 同意 ----------
        private void RequestConsentThenInit()
        {
            var consentInfo = ConsentInformation.Instance;
            var request = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = false, // 应用不面向儿童
            };

            // Update 会向 Google 请求用户是否需要同意(GDPR 地区)
            consentInfo.Update(request, updateError =>
            {
                if (updateError != null)
                    Debug.LogWarning($"[UMP] 同意信息更新失败:{updateError.Message}");

                if (consentInfo.CanRequestAds)
                {
                    // 可直接请求广告(非 GDPR 地区,或用户已同意)
                    MobileAds.Initialize(status => { IsInitialized = true; LoadRewardedAd(); });
                }
                else
                {
                    // GDPR 地区:先展示同意表单,再初始化广告
                    ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                    {
                        if (formError != null)
                            Debug.LogWarning($"[UMP] 同意表单错误:{formError.Message}");
                        MobileAds.Initialize(status => { IsInitialized = true; LoadRewardedAd(); });
                    });
                }
            });
        }

        // ---------- 激励视频 ----------
        private void LoadRewardedAd()
        {
            // 每次重建广告对象并订阅事件(旧对象由 Destroy 释放,无需手动退订)
            if (_rewardedAd != null) _rewardedAd.Destroy();

            _rewardedAd = new RewardedAd(RewardedAdUnitId);

            // 玩家看完广告 → 发放奖励
            _rewardedAd.OnUserEarnedReward += (sender, reward) =>
            {
                var cb = _pendingRewardCallback;
                _pendingRewardCallback = null;
                cb?.Invoke(true);
            };

            // 广告关闭 → 预加载下一条,提升下次体验
            _rewardedAd.OnAdClosed += (sender, e) => LoadRewardedAd();

            // 展示失败 → 不发奖
            _rewardedAd.OnAdFailedToShow += (sender, e) =>
            {
                var cb = _pendingRewardCallback;
                _pendingRewardCallback = null;
                cb?.Invoke(false);
            };

            _rewardedAd.LoadAd(new AdRequest.Builder().Build());
        }
    }
}
#endif
