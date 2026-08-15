using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Gameplay
{
    /// <summary>首次启动新手引导(模态):仅在未完成时显示,分步讲解、可跳过,引导期间暂停计时。</summary>
    public sealed class OnboardingView : MonoBehaviour
    {
        private string[] _steps;
        private GameObject _overlay;
        private Text _stepText;
        private Text _nextLabel;
        private int _stepIndex;

        private void Awake()
        {
            if (SettingsService.OnboardingCompleted) return; // 已看过,不显示

            _steps = new[]
            {
                Localization.T("onboarding.step1"),
                Localization.T("onboarding.step2"),
                Localization.T("onboarding.step3"),
                Localization.T("onboarding.step4"),
                Localization.T("onboarding.step5"),
            };

            UiFactory.Stretch((RectTransform)transform);
            BuildOverlay();
            ShowStep(0);
            Time.timeScale = 0f; // 引导期间暂停计时
        }

        private void OnDestroy()
        {
            if (Time.timeScale == 0f) Time.timeScale = 1f; // 兜底恢复,防止卡在暂停
        }

        private void BuildOverlay()
        {
            var overlayRt = UiFactory.CreateRect("Overlay", transform);
            _overlay = overlayRt.gameObject;
            UiFactory.Stretch(overlayRt);
            _overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

            var panel = UiFactory.CreateRect("Panel", _overlay.transform);
            var panelRt = panel;
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(720, 560);
            panelRt.anchoredPosition = Vector2.zero;
            panel.gameObject.AddComponent<Image>().color = Theme.Panel;

            var layout = UiFactory.Vertical(panel, 20f, TextAnchor.UpperCenter);
            layout.padding = new RectOffset(32, 32, 32, 32);

            var title = UiFactory.CreateText("Title", panel, 34, TextAnchor.MiddleCenter, Theme.Text);
            title.text = Localization.T("onboarding.title");

            _stepText = UiFactory.CreateText("StepText", panel, 26, TextAnchor.UpperLeft, Theme.Text);
            _stepText.horizontalOverflow = HorizontalWrapMode.Wrap; // 多行文本需要换行
            var stepLe = _stepText.gameObject.AddComponent<LayoutElement>();
            stepLe.preferredWidth = 640;
            stepLe.preferredHeight = 220;

            var row = UiFactory.CreateRect("Buttons", panel);
            UiFactory.Horizontal(row, 16f);
            UiFactory.CreateButton("Skip", row, Localization.T("onboarding.skip"), Theme.Secondary, Finish, 160, 64);
            var next = UiFactory.CreateButton("Next", row, Localization.T("onboarding.next"), Theme.Primary, NextStep, 200, 64);
            _nextLabel = next.GetComponentInChildren<Text>();
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
