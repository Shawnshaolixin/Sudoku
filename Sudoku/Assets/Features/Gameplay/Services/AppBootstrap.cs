using UnityEngine;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 应用启动引导:在第一个场景加载前初始化广告 / 内购 / 分析服务。
    /// [RuntimeInitializeOnLoadMethod] 保证只执行一次,且主菜单与对局场景都能使用。
    /// </summary>
    public static class AppBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            Services.Initialize();
            AudioService.Initialize();

            Services.Iap.Initialize();   // 先内购:启动时自动恢复「去广告」购买状态
            Services.Ads.Initialize();   // 再广告:内部含 UMP 同意流程
            AudioService.PlayBgm("bgm"); // 背景音乐(缺失文件时静默跳过)
            Services.Analytics.LogEvent("session_start");
        }
    }
}
