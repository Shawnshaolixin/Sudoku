using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 首次启动新手引导(模态):仅在未完成时显示,分步讲解、可跳过,引导期间暂停计时。
    /// UI 层级由 Prefab 承载(UiPrefabBuilder 生成),这里只绑定逻辑与文案。
    /// 已完成引导时直接自隐藏,由场景构建器统一放置。
    /// </summary>
    public sealed class OnboardingView : MonoBehaviour
    {
        [Header("Prefab 引用")]
        [SerializeField] private GameObject _overlay;
        [SerializeField] private Text _title;
        [SerializeField] private Text _stepText;
        [SerializeField] private Text _nextLabel;
        [SerializeField] private Button _skipButton;
        [SerializeField] private Button _nextButton;

        private string[] _steps;
        private int _stepIndex;

        private void Awake()
        {
            if (SettingsService.OnboardingCompleted)
            {
                gameObject.SetActive(false); // 已看过,不显示
                return;
            }

            _steps = new[]
            {
                Localization.T("onboarding.step1"),
                Localization.T("onboarding.step2"),
                Localization.T("onboarding.step3"),
                Localization.T("onboarding.step4"),
                Localization.T("onboarding.step5"),
            };

            _title.text = Localization.T("onboarding.title");
            WireUi();
            ShowStep(0);
            Time.timeScale = 0f; // 引导期间暂停计时
        }

        private void OnDestroy()
        {
            if (Time.timeScale == 0f) Time.timeScale = 1f; // 兜底恢复,防止卡在暂停
        }

        private void WireUi()
        {
            UiFactory.Wire(_skipButton, Finish);
            UiFactory.Wire(_nextButton, NextStep);
        }

        private void ShowStep(int index)
        {
            _stepIndex = index;
            if (_stepText != null) _stepText.text = _steps[index];
            if (_nextLabel != null)
                _nextLabel.text = index == _steps.Length - 1 ? Localization.T("onboarding.start") : Localization.T("onboarding.next");
        }

        private void NextStep()
        {
            if (_stepIndex < _steps.Length - 1) ShowStep(_stepIndex + 1);
            else Finish();
        }

        private void Finish()
        {
            SettingsService.OnboardingCompleted = true;
            Time.timeScale = 1f;
            if (_overlay != null) _overlay.SetActive(false);
            Destroy(gameObject);
        }
    }
}
