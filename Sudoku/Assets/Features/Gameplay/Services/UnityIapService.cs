// 本文件只有在定义了编译符号 SUDOKU_IAP 后才会参与编译。
// 前提:先通过 Package Manager 安装 Unity IAP(本文针对 v4.x,推荐 4.12.x)。
#if SUDOKU_IAP
using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 真实 Unity IAP 内购服务:去广告(非消耗型商品)。
    ///
    /// 完整流程:
    ///   1) 用 ConfigurationBuilder 声明商品;
    ///   2) UnityPurchasing.Initialize 初始化商店;
    ///   3) 购买 → ProcessPurchase 回调 → 标记已购并触发 PurchaseCompleted;
    ///   4) 非消耗型商品在重启后会自动恢复(有收据即视为已购)。
    /// </summary>
    public sealed class UnityIapService : IIapService, IDetailedStoreListener
    {
        // TODO:必须在 Google Play Console → 应用内商品 里创建同名商品 ID。
        public const string RemoveAdsProductId = "remove_ads";

        private IStoreController _storeController;

        public bool IsInitialized => _storeController != null;
        public bool IsRemoveAdsPurchased { get; private set; }
        public event Action PurchaseCompleted;

        public void Initialize()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            // NonConsumable = 非消耗型:买一次永久有效,可跨设备恢复
            builder.AddProduct(RemoveAdsProductId, ProductType.NonConsumable);
            UnityPurchasing.Initialize(this, builder);
        }

        public void BuyRemoveAds()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[IAP] 商店尚未初始化");
                return;
            }
            _storeController.InitiatePurchase(RemoveAdsProductId);
        }

        public void RestorePurchases()
        {
            // Google Play:非消耗型商品在初始化时已自动恢复,这里做兜底重查;
            // Apple(iOS):需要调用 AppleExtensions.RestoreTransactions,本文以 Android 为主。
            if (IsInitialized && _storeController.products.WithID(RemoveAdsProductId)?.hasReceipt == true)
                MarkPurchased();
        }

        // ---------- IDetailedStoreListener 回调 ----------
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;

            // 非消耗型商品有收据,说明之前买过(含换机后自动恢复)
            if (controller.products.WithID(RemoveAdsProductId)?.hasReceipt == true)
                MarkPurchased();
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogWarning($"[IAP] 初始化失败:{error}");
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogWarning($"[IAP] 初始化失败:{error} {message}");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            if (args.purchasedProduct.definition.id == RemoveAdsProductId)
                MarkPurchased();
            return PurchaseProcessingResult.Complete; // 告诉商店「处理完成,可以发货」
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.LogWarning($"[IAP] 购买失败:{product.definition.id} {failureReason}");
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            Debug.LogWarning($"[IAP] 购买失败:{product.definition.id} {failureDescription.message}");
        }

        private void MarkPurchased()
        {
            IsRemoveAdsPurchased = true;
            PurchaseCompleted?.Invoke();
        }
    }
}
#endif
