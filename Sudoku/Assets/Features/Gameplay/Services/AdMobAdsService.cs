// 本文件只有在定义了编译符号 SUDOKU_ADMOB 后才会参与编译。
// 针对 Google Mobile Ads Unity 插件 v8.6+ / v8.7 的新 API:
//   - RewardedAd 无参构造,广告位 ID 传给 LoadAd;
//   - 奖励回调直接传给 Show();
//   - UMP 使用静态方法(ConsentInformation.Update / CanRequestAds),不再用 Instance。
#if SUDOKU_ADMOB
using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 真实 AdMob 广告服务:激励视频 + UMP 同意管理(GDPR)。
    /// 流程:请求同意(UMP)→ 初始化 AdMob → 加载激励视频 → 展示并发放奖励。
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
            // v8.6+ 新 API:奖励回调直接传给 Show()
            _rewardedAd.Show(reward =>
            {
                var cb = _pendingRewardCallback;
                _pendingRewardCallback = null;
                cb?.Invoke(true);
            });
        }

        // ---------- UMP 同意(静态 API) ----------
        private void RequestConsentThenInit()
        {
            var request = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = false, // 应用不面向儿童
            };

            // v8.6+ 新 API:静态方法,不再用 ConsentInformation.Instance
            ConsentInformation.Update(request, updateError =>
            {
                if (updateError != null)
                    Debug.LogWarning($"[UMP] 同意信息更新失败:{updateError.Message}");

                if (ConsentInformation.CanRequestAds())
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
            // 清理旧广告实例
            if (_rewardedAd != null)
            {
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }

            // v8.7 API:RewardedAd 没有公开构造器,只能用静态 Load 加载,
            // 加载成功后通过回调返回广告实例(loadError 为 null 表示成功)。
            RewardedAd.Load(RewardedAdUnitId, new AdRequest.Builder().Build(), (ad, loadError) =>
            {
                if (loadError != null)
                {
                    Debug.LogWarning($"[AdMob] 激励视频加载失败:{loadError}");
                    return;
                }

                _rewardedAd = ad;

                // 广告关闭 → 预加载下一条,提升下次体验
                _rewardedAd.OnAdFullScreenContentClosed += () => LoadRewardedAd();

                // 展示失败 → 不发奖
                _rewardedAd.OnAdFullScreenContentFailed += error =>
                {
                    var cb = _pendingRewardCallback;
                    _pendingRewardCallback = null;
                    cb?.Invoke(false);
                };
            });
        }
    }
}
#endif
