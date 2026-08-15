using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Gameplay
{
    /// <summary>首次启动新手引导(模态):仅在未完成时显示,分步讲解、可跳过,引导期间暂停计时。</summary>
    public sealed class OnboardingView : MonoBehaviour
    {
        private static readonly string[] Steps =
        {
            "数独规则:在 9×9 棋盘里,让每一行、每一列、每个 3×3 宫都包含 1~9,且不重复。",
            "点击任意空格选中它,再用下方数字键盘填入数字。",
            "点数字键盘的「✎」可切换笔记模式,记录候选数。",
            "选中一个数字时,同行/列/宫和相同数字会高亮,帮你排除。",
            "卡住时点「提示」,会帮你填入一个正确的数字。准备好了就点「开始」!"
        };

        [Header("配色")]
        [SerializeField] private Color _panelColor = new Color(0.98f, 0.98f, 1f, 1f);
        [SerializeField] private Color _textColor = new Color(0.12f, 0.12f, 0.18f, 1f);
        [SerializeField] private Color _primaryColor = new Color(0.35f, 0.55f, 0.95f, 1f);
        [SerializeField] private Color _secondaryColor = new Color(0.90f, 0.92f, 1f, 1f);

        private GameObject _overlay;
        private Text _stepText;
        private Text _nextLabel;
        private int _stepIndex;

        private void Awake()
        {
            if (SettingsService.OnboardingCompleted) return; // 已看过,不显示

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
            var dim = _overlay.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.7f);

            var panel = UiFactory.CreateRect("Panel", _overlay.transform);
            var panelRt = panel;
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(720, 560);
            panelRt.anchoredPosition = Vector2.zero;
            panel.gameObject.AddComponent<Image>().color = _panelColor;

            var layout = UiFactory.Vertical(panel, 20f, TextAnchor.UpperCenter);
            layout.padding = new RectOffset(32, 32, 32, 32);

            var title = UiFactory.CreateText("Title", panel, 34, TextAnchor.MiddleCenter, _textColor);
            title.text = "怎么玩";

            _stepText = UiFactory.CreateText("StepText", panel, 26, TextAnchor.UpperLeft, _textColor);
            _stepText.horizontalOverflow = HorizontalWrapMode.Wrap; // 多行文本需要换行
            var stepLe = _stepText.gameObject.AddComponent<LayoutElement>();
            stepLe.preferredWidth = 640;
            stepLe.preferredHeight = 220;

            var row = UiFactory.CreateRect("Buttons", panel);
            UiFactory.Horizontal(row, 16f);
            UiFactory.CreateButton("Skip", row, "跳过", _secondaryColor, Finish, 160, 64);
            var next = UiFactory.CreateButton("Next", row, "下一步", _primaryColor, NextStep, 200, 64);
            _nextLabel = next.GetComponentInChildren<Text>();
        }

        private void ShowStep(int index)
        {
            _stepIndex = index;
            if (_stepText != null) _stepText.text = Steps[index];
            if (_nextLabel != null)
                _nextLabel.text = index == Steps.Length - 1 ? "开始" : "下一步";
        }

        private void NextStep()
        {
            if (_stepIndex < Steps.Length - 1) ShowStep(_stepIndex + 1);
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
