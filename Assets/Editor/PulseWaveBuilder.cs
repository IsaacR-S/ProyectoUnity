#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// CANDLE FURY — Construye Assets/Prefabs/PulseWave.prefab (anillo de onda del
/// Pulso: SpriteRenderer unlit + SpriteFlipbook + PulseWaveEffect) y cablea
/// PlayerCombat.pulseWavePrefab en la escena. Idempotente.
/// Entrada batch: -executeMethod PulseWaveBuilder.BuildAndWireAllScenes
/// </summary>
public static class PulseWaveBuilder
{
    const string SpriteDir  = "Assets/Sprites/Effects/";
    const string PrefabPath = "Assets/Prefabs/PulseWave.prefab";
    const string UnlitMaterialPath =
        "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
    const float WorldHeight = 1f;   // 48 px → PPU 48 → anillo de 1u de diámetro base

    static readonly string[] ScenePaths =
        { "Assets/Scenes/Level1.unity", "Assets/Scenes/Level2.unity" };

    [MenuItem("Candle Fury/Construir Onda de Pulso")]
    public static void BuildAndWireActiveScene()
    {
        if (!BuildPrefab()) return;
        WireScene();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Recuerda GUARDAR la escena (Ctrl+S) y repetir en la otra escena.");
    }

    public static void BuildAndWireAllScenes()
    {
        if (!BuildPrefab()) return;
        foreach (string path in ScenePaths)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            WireScene();
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"✔ Onda de pulso cableada en {path}");
        }
    }

    static bool BuildPrefab()
    {
        var sprites = new Sprite[3];
        for (int i = 0; i < 3; i++)
        {
            sprites[i] = PixelSpriteImport.Import($"{SpriteDir}pulse_wave_{i}.png", WorldHeight);
            if (sprites[i] == null)
            {
                Debug.LogError($"[PulseWaveBuilder] FALTA {SpriteDir}pulse_wave_{i}.png");
                return false;
            }
        }
        var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);

        GameObject root;
        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
        root = existed ? PrefabUtility.LoadPrefabContents(PrefabPath)
                       : new GameObject("PulseWave");
        try
        {
            var sr = root.GetComponent<SpriteRenderer>();
            if (sr == null) sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[0];
            sr.color  = Color.white;
            if (unlit != null) sr.sharedMaterial = unlit;
            sr.sortingOrder = 5;   // por encima de personajes

            var flip = root.GetComponent<SpriteFlipbook>();
            if (flip == null) flip = root.AddComponent<SpriteFlipbook>();
            var so  = new SerializedObject(flip);
            var arr = so.FindProperty("frames");
            arr.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            if (root.GetComponent<PulseWaveEffect>() == null)
                root.AddComponent<PulseWaveEffect>();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("✔ PulseWave.prefab construido");
            return true;
        }
        finally
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }
    }

    static void WireScene()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        foreach (var pc in Object.FindObjectsByType<PlayerCombat>(FindObjectsSortMode.None))
        {
            var so = new SerializedObject(pc);
            so.FindProperty("pulseWavePrefab").objectReferenceValue = prefab;
            so.ApplyModifiedProperties();
            Debug.Log($"✔ pulseWavePrefab cableado en {pc.gameObject.name}");
        }
    }
}
#endif
