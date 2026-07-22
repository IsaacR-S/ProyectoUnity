#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// CANDLE FURY — Importador compartido de sprites pixelart:
/// Point filter, sin compresión, Sprite/Single, PPU derivado del alto
/// deseado en unidades de mundo, y pivote custom opcional.
/// (Misma convención que SpriteAssigner, reutilizable desde otros menús.)
/// </summary>
public static class PixelSpriteImport
{
    public static Sprite Import(string assetPath, float worldHeight,
                                Vector2? customPivot = null, Vector4? border = null)
    {
        var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (imp == null) return null;

        bool dirty = false;
        if (imp.textureType        != TextureImporterType.Sprite)  { imp.textureType      = TextureImporterType.Sprite;  dirty = true; }
        if (imp.spriteImportMode   != SpriteImportMode.Single)     { imp.spriteImportMode = SpriteImportMode.Single;     dirty = true; }
        if (imp.filterMode         != FilterMode.Point)            { imp.filterMode       = FilterMode.Point;            dirty = true; }
        if (imp.textureCompression != TextureImporterCompression.Uncompressed)
                                                                   { imp.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
        if (dirty) { imp.SaveAndReimport(); dirty = false; }

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex == null) return null;

        float ppu = Mathf.Round(tex.height / worldHeight);
        if (Mathf.Abs(imp.spritePixelsPerUnit - ppu) > 0.5f) { imp.spritePixelsPerUnit = ppu; dirty = true; }

        if (customPivot.HasValue || border.HasValue)
        {
            var ts = new TextureImporterSettings();
            imp.ReadTextureSettings(ts);
            bool tsDirty = false;
            if (customPivot.HasValue &&
                (ts.spriteAlignment != (int)SpriteAlignment.Custom || ts.spritePivot != customPivot.Value))
            {
                ts.spriteAlignment = (int)SpriteAlignment.Custom;
                ts.spritePivot     = customPivot.Value;
                tsDirty = true;
            }
            if (border.HasValue && ts.spriteBorder != border.Value)
            {
                ts.spriteBorder = border.Value;
                tsDirty = true;
            }
            if (tsDirty) { imp.SetTextureSettings(ts); dirty = true; }
        }
        if (dirty) imp.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }
}
#endif
