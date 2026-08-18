using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 胜利撒花:全屏彩带雨,挂在 UI Canvas 最顶层(盖住结算弹窗与遮罩)。
    /// 全部在运行时用代码生成 Image,无需 Prefab;贴图放在 Assets/Resources/Art/Effects/
    /// (SpriteImportSettings 自动切图),缺失时静默跳过。
    /// 入口:VictoryCelebration.Play(),由 SudokuBoardView 在胜利事件里调用;
    /// 调试:对局页右上角 Test 按钮可直接触发。
    /// </summary>
    public static class VictoryCelebration
    {
        private static readonly string[] SpriteNames =
        {
            "star_01", "star_02", "star_03", "star_04", "star_05",
            "star_06", "star_07", "star_08", "star_09",
            "spark_01", "spark_02", "spark_03", "spark_04",
            "spark_05", "spark_06", "spark_07",
        };

        // 节日配色(每片随机取一色)
        private static readonly Color[] Palette =
        {
            new Color(1f, 0.85f, 0.30f), // 金
            new Color(1f, 0.45f, 0.55f), // 粉
            new Color(0.40f, 0.80f, 1f), // 天蓝
            new Color(0.60f, 0.45f, 1f), // 紫
            new Color(0.40f, 0.90f, 0.55f), // 绿
            new Color(1f, 0.60f, 0.30f), // 橙
        };

        private const int PieceCount = 60; // 一片彩带数

        /// <summary>在 UI 最顶层播一场撒花雨。找不到 Canvas 时静默跳过。</summary>
        public static void Play()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            var go = new GameObject("VictoryConfetti", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; // 铺满画布
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.AddComponent<ConfettiRain>();
        }

        /// <summary>单场撒花的动画载体:生成彩带片 → 逐帧下落/旋转/淡出 → 播完自毁。</summary>
        private sealed class ConfettiRain : MonoBehaviour
        {
            private struct Piece
            {
                public RectTransform Rt;
                public Image Image;
                public Vector2 Velocity;
                public float Spin;   // 旋转速度(度/秒)
                public float Life;   // 总寿命
                public float FadeAt; // 开始淡出的时间点
            }

            private Piece[] _pieces;
            private float _elapsed;

            private void Awake()
            {
                var container = (RectTransform)transform;
                float w = container.rect.width;
                float h = container.rect.height;

                _pieces = new Piece[PieceCount];
                for (int i = 0; i < _pieces.Length; i++)
                {
                    var piece = new GameObject($"C_{i}", typeof(RectTransform));
                    piece.transform.SetParent(transform, false);
                    var rt = (RectTransform)piece.transform;
                    rt.anchorMin = Vector2.zero; // 左下角定位,anchoredPosition 即画布像素坐标
                    rt.anchorMax = Vector2.zero;
                    rt.anchoredPosition = new Vector2(Random.Range(0f, w), Random.Range(h * 0.45f, h)); // 上半屏出生
                    float size = Random.Range(24f, 56f);
                    rt.sizeDelta = new Vector2(size, size);

                    var img = piece.AddComponent<Image>();
                    img.sprite = Resources.Load<Sprite>("Art/Effects/" + SpriteNames[Random.Range(0, SpriteNames.Length)]);
                    if (img.sprite == null)
                    {
                        Destroy(piece); // 贴图缺失:跳过这一片
                        continue;
                    }
                    img.color = Palette[Random.Range(0, Palette.Length)];
                    img.raycastTarget = false; // 不拦截结算按钮点击

                    float life = Random.Range(1.6f, 2.6f);
                    _pieces[i] = new Piece
                    {
                        Rt = rt,
                        Image = img,
                        Velocity = new Vector2(Random.Range(-40f, 40f), Random.Range(-60f, -20f)),
                        Spin = Random.Range(-360f, 360f),
                        Life = life,
                        FadeAt = life * 0.7f,
                    };
                }
            }

            private void Update()
            {
                _elapsed += Time.deltaTime;
                for (int i = 0; i < _pieces.Length; i++)
                {
                    var p = _pieces[i];
                    if (p.Rt == null) continue; // 贴图缺失被跳过的片

                    p.Velocity.y -= 260f * Time.deltaTime; // 重力(画布像素单位)
                    p.Rt.anchoredPosition += p.Velocity * Time.deltaTime;
                    p.Rt.localRotation *= Quaternion.Euler(0f, 0f, p.Spin * Time.deltaTime);

                    if (_elapsed > p.FadeAt)
                    {
                        float t = (_elapsed - p.FadeAt) / (p.Life - p.FadeAt);
                        var c = p.Image.color;
                        p.Image.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(1f - t));
                    }
                }

                if (_elapsed > 2.6f) Destroy(gameObject); // 最晚一片播完,整体自毁
            }
        }
    }
}
