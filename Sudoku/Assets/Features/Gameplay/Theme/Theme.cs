using UnityEngine;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 统一配色主题(轻量美化):所有界面从这里取色,保证整体风格一致。
    /// 想换皮肤/深色模式时,只需改这里或扩展多套主题,不用逐个界面改颜色。
    /// </summary>
    public static class Theme
    {
        // 通用
        public static readonly Color Background = new Color(0.93f, 0.91f, 0.88f, 1f); // 暖米色,与白色格子区分
        public static readonly Color Panel = new Color(1f, 1f, 1f, 1f);
        public static readonly Color Primary = new Color(0.30f, 0.50f, 0.90f, 1f);   // 主色(选中/主要按钮)
        public static readonly Color Secondary = new Color(0.90f, 0.92f, 0.98f, 1f); // 次要按钮/面板
        public static readonly Color Text = new Color(0.12f, 0.12f, 0.18f, 1f);
        public static readonly Color TextMuted = new Color(0.45f, 0.45f, 0.52f, 1f);
        public static readonly Color ButtonLabel = new Color(0.10f, 0.10f, 0.15f, 1f);

        // 棋盘格
        public static readonly Color GridLine = new Color(0.24f, 0.28f, 0.40f, 1f); // 网格线/边框(深藏青)
        public static readonly Color CellBase = new Color(1f, 1f, 1f, 1f);
        public static readonly Color CellAltBox = new Color(0.91f, 0.94f, 1f, 1f);
        public static readonly Color CellSelected = new Color(0.65f, 0.80f, 1f, 1f);
        public static readonly Color CellPeer = new Color(0.86f, 0.91f, 1f, 1f);
        public static readonly Color CellSameNumber = new Color(0.55f, 0.72f, 1f, 1f);
        public static readonly Color CellMistake = new Color(1f, 0.72f, 0.72f, 1f);
        public static readonly Color GivenText = new Color(0.10f, 0.10f, 0.15f, 1f);
        public static readonly Color PlayerText = new Color(0.10f, 0.30f, 0.80f, 1f);
        public static readonly Color NoteText = new Color(0.40f, 0.40f, 0.45f, 1f);

        // 遮罩/反馈
        public static readonly Color OverlayDim = new Color(0f, 0f, 0f, 0.6f);
        public static readonly Color Success = new Color(0.20f, 0.60f, 0.30f, 1f);
        public static readonly Color Feedback = new Color(0.65f, 0.30f, 0.30f, 1f);
    }
}
