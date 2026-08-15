namespace Sudoku.Gameplay
{
    /// <summary>
    /// 服务定位器:统一提供广告 / 内购 / 分析三个服务。
    ///
    /// 关键设计:用「编译开关」在真实 SDK 实现和桩实现之间切换。
    ///   - 未定义符号(默认):使用桩实现,不装任何 SDK 也能编译运行;
    ///   - 装了 AdMob 后:在 Player Settings → Scripting Define Symbols 加 SUDOKU_ADMOB;
    ///   - 装了 Unity IAP 后:加 SUDOKU_IAP;
    ///   - 装了 Firebase 后:加 SUDOKU_FIREBASE。
    /// </summary>
    public static class Services
    {
        public static IAdsService Ads { get; private set; }
        public static IIapService Iap { get; private set; }
        public static IAnalyticsService Analytics { get; private set; }

        public static void Initialize()
        {
            // ---------- 广告 ----------
#if SUDOKU_ADMOB
            Ads = new AdMobAdsService();
#else
            Ads = new AdsServiceStub();
#endif

            // ---------- 内购 ----------
#if SUDOKU_IAP
            Iap = new UnityIapService();
#else
            Iap = new IapServiceStub();
#endif

            // ---------- 分析 ----------
#if SUDOKU_FIREBASE
            Analytics = new FirebaseAnalyticsService();
#else
            Analytics = new AnalyticsServiceStub();
#endif

            // 内购「去广告」购买/恢复完成后,同步到广告服务(去广告用户零广告)
            Iap.PurchaseCompleted += () => Ads.SetRemoveAds(true);

            Analytics.LogEvent("services_initialized");
        }
    }
}
