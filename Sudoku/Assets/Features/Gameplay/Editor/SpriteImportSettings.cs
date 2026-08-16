using UnityEditor;
using UnityEngine;

namespace Sudoku.Gameplay.Editor
{
    /// <summary>
    /// 自动设置贴图导入参数,免去逐个手动改 Inspector:
    ///   - Assets/Resources/Art/UI/      下的 PNG → Sprite(2D and UI)+ 9-slice 圆角边框;
    ///   - Assets/Resources/Art/Effects/ 下的 PNG → Sprite(2D and UI),不做 9-slice。
    ///
    /// 用法:
    ///   1) 把下载解压后的 PNG 放进上述文件夹(拖进新文件时本脚本自动生效);
    ///   2) 已导入过的旧文件,点菜单 Sudoku → Reapply Art Import Settings 重刷一遍。
    ///
    /// 注意:9-slice 边框默认 20px,如果你的按钮圆角更小/更大,改下面的 UiBorder 常量后重刷即可。
    /// </summary>
    public class SpriteImportSettings : AssetPostprocessor
    {
        private const int UiBorder = 20; // UI 按钮/面板的 9-slice 边框(像素)

        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');

            if (path.StartsWith("Assets/Resources/Art/UI/"))
                ApplySpriteSettings(spriteBorder: UiBorder);
            else if (path.StartsWith("Assets/Resources/Art/Effects/"))
                ApplySpriteSettings(spriteBorder: 0);
        }

        private void ApplySpriteSettings(int spriteBorder)
        {
            var importer = assetImporter as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false;

            // 9-slice 边框:四个值分别代表 左/下/右/上(这里四边一致)
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteBorder = new Vector4(spriteBorder, spriteBorder, spriteBorder, spriteBorder);
            settings.spriteMeshType = SpriteMeshType.FullRect; // 9-slice 需要 FullRect
            settings.alphaIsTransparency = true;
            importer.SetTextureSettings(settings);
        }

        [MenuItem("Sudoku/Reapply Art Import Settings")]
        public static void ReapplyToAll()
        {
            int count = 0;
            string[] guids = AssetDatabase.FindAssets("t:Texture2D",
                new[] { "Assets/Resources/Art/UI", "Assets/Resources/Art/Effects" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool isUi = path.Replace('\\', '/').StartsWith("Assets/Resources/Art/UI/");
                int border = isUi ? UiBorder : 0;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.mipmapEnabled = false;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteBorder = new Vector4(border, border, border, border);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.alphaIsTransparency = true;
                importer.SetTextureSettings(settings);

                importer.SaveAndReimport();
                count++;
            }

            Debug.Log($"[SpriteImportSettings] 已重刷 {count} 张贴图设置");
        }
    }
}
