// 本文件只有在定义了编译符号 SUDOKU_FIREBASE 后才会参与编译。
// 前提:先安装 Firebase Unity SDK(本文针对 v12.x),并把 google-services.json 放入 Assets 后由 EDM4U 处理。
#if SUDOKU_FIREBASE
using Firebase.Analytics;
using Firebase.Crashlytics;
using UnityEngine;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 真实 Firebase 分析服务:Firebase Analytics 埋点 + Crashlytics 崩溃/非致命上报。
    /// Firebase 会在首次调用其 API 时自动初始化(前提是 google-services.json 已正确配置)。
    /// </summary>
    public sealed class FirebaseAnalyticsService : IAnalyticsService
    {
        public void LogEvent(string eventName)
        {
            FirebaseAnalytics.LogEvent(eventName);
        }

        public void LogEvent(string eventName, string parameterName, object parameterValue)
        {
            // Firebase Parameter 需要明确类型;这里把 object 转成对应类型。
            if (parameterValue is long l)
                FirebaseAnalytics.LogEvent(eventName, new Parameter(parameterName, l));
            else if (parameterValue is double d)
                FirebaseAnalytics.LogEvent(eventName, new Parameter(parameterName, d));
            else
                FirebaseAnalytics.LogEvent(eventName, new Parameter(parameterName, parameterValue?.ToString() ?? ""));
        }

        public void LogNonFatal(string message)
        {
            // Crashlytics 记录「非致命错误」,不会导致崩溃,但可在后台看到异常信息
            Crashlytics.LogException(new System.Exception(message));
        }
    }
}
#endif
