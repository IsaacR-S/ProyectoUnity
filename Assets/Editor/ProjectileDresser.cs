#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// CANDLE FURY — Viste Proyectil.prefab con la bola de fuego pixelart:
/// sprite frame 0 + SpriteFlipbook (4 frames), material Sprite-Unlit-Default
/// (el fuego es auto-emisivo: brilla igual en la oscuridad de Level2)
/// y TrailRenderer con estela cian. Idempotente.
/// Entrada batch: -executeMethod ProjectileDresser.Dress
/// </summary>
public static class ProjectileDresser
{
    const string SpriteDir  = "Assets/Sprites/Effects/";
    const string PrefabPath = "Assets/Prefabs/Proyectil.prefab";
    const string UnlitMaterialPath =
        "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
    const float WorldHeight = 0.9f;   // 16px → PPU 18 ≈ diámetro de colisión (1u)

    [MenuItem("Candle Fury/Vestir Proyectil")]
    public static void Dress()
    {
        var sprites = new Sprite[4];
        for (int i = 0; i < 4; i++)
        {
            sprites[i] = PixelSpriteImport.Import($"{SpriteDir}fireball_{i}.png", WorldHeight);
            if (sprites[i] == null)
            {
                Debug.LogError($"[ProjectileDresser] FALTA {SpriteDir}fireball_{i}.png");
                return;
            }
        }
        var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var sr = root.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                Debug.LogError($"[ProjectileDresser] {PrefabPath} sin SpriteRenderer en la raíz");
                return;
            }
            sr.sprite = sprites[0];
            sr.color  = Color.white;              // el color ya va en el sprite
            if (unlit != null) sr.sharedMaterial = unlit;

            var flip = root.GetComponent<SpriteFlipbook>();
            if (flip == null) flip = root.AddComponent<SpriteFlipbook>();
            var so  = new SerializedObject(flip);
            var arr = so.FindProperty("frames");
            arr.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            var trail = root.GetComponent<TrailRenderer>();
            if (trail == null) trail = root.AddComponent<TrailRenderer>();
            trail.time              = 0.15f;
            trail.startWidth        = 0.25f;
            trail.endWidth          = 0f;
            trail.minVertexDistance = 0.05f;
            if (unlit != null) trail.sharedMaterial = unlit;
            trail.sortingLayerID = sr.sortingLayerID;
            trail.sortingOrder   = sr.sortingOrder - 1;   // detrás de la bola
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(0.290f, 0.831f, 1f),   0f),
                        new GradientColorKey(new Color(0.118f, 0.470f, 0.784f), 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = grad;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("✔ Proyectil vestido: fireball flipbook + unlit + estela");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
#endif
