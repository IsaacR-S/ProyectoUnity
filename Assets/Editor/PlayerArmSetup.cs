#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// CANDLE FURY — Crea/configura el hijo "Arm" del Player en la escena:
/// SpriteRenderer (lit, encima del cuerpo, apagado) + PlayerAttackArm con
/// los sprites arm_sword/arm_cast. Idempotente, con Undo.
/// No hay prefab de Player: correr en Level1 y Level2.
/// Entrada batch: -executeMethod PlayerArmSetup.SetupAllScenes
/// </summary>
public static class PlayerArmSetup
{
    const string SwordPath = "Assets/Sprites/Effects/arm_sword.png";
    const string CastPath  = "Assets/Sprites/Effects/arm_cast.png";
    const string LitMaterialPath =
        "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";
    // Alto 10 px al PPU del player (37) → 10/37 u de alto en mundo
    const float ArmWorldHeight = 10f / 37f;

    static readonly string[] ScenePaths =
        { "Assets/Scenes/Level1.unity", "Assets/Scenes/Level2.unity" };

    [MenuItem("Candle Fury/Configurar Brazo Jugador")]
    public static void SetupActiveScene()
    {
        Setup();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Recuerda GUARDAR la escena (Ctrl+S) y repetir en la otra escena.");
    }

    public static void SetupAllScenes()
    {
        foreach (string path in ScenePaths)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Setup();
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"✔ Brazo configurado en {path}");
        }
    }

    static void Setup()
    {
        // pivote en el hombro (extremo izquierdo del sprite)
        Sprite sword = PixelSpriteImport.Import(SwordPath, ArmWorldHeight, new Vector2(0.12f, 0.5f));
        Sprite cast  = PixelSpriteImport.Import(CastPath,  ArmWorldHeight, new Vector2(0.19f, 0.5f));
        if (sword == null || cast == null)
        {
            Debug.LogError("[PlayerArmSetup] Faltan arm_sword.png / arm_cast.png en Assets/Sprites/Effects/");
            return;
        }
        Material lit = AssetDatabase.LoadAssetAtPath<Material>(LitMaterialPath);

        foreach (var pc in Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            Transform arm = pc.transform.Find("Arm");
            if (arm == null)
            {
                var go = new GameObject("Arm");
                Undo.RegisterCreatedObjectUndo(go, "crear Arm");
                go.transform.SetParent(pc.transform, false);
                arm = go.transform;
            }
            Undo.RecordObject(arm, "configurar Arm");
            arm.localPosition = new Vector3(0.05f, 0.15f, 0f);   // hombro aprox.

            var sr = arm.GetComponent<SpriteRenderer>();
            if (sr == null) sr = Undo.AddComponent<SpriteRenderer>(arm.gameObject);
            Undo.RecordObject(sr, "configurar Arm");
            sr.sprite  = sword;
            sr.enabled = false;                    // PlayerAttackArm lo enciende al atacar
            if (lit != null) sr.sharedMaterial = lit;
            var bodySr = pc.GetComponent<SpriteRenderer>();
            if (bodySr != null)
            {
                sr.sortingLayerID = bodySr.sortingLayerID;
                sr.sortingOrder   = bodySr.sortingOrder + 1;   // encima del cuerpo
            }

            var attackArm = arm.GetComponent<PlayerAttackArm>();
            if (attackArm == null) attackArm = Undo.AddComponent<PlayerAttackArm>(arm.gameObject);
            var so = new SerializedObject(attackArm);
            so.FindProperty("swordSprite").objectReferenceValue = sword;
            so.FindProperty("castSprite").objectReferenceValue  = cast;
            so.ApplyModifiedProperties();
            Debug.Log($"✔ Arm listo en {pc.gameObject.name}");
        }
    }
}
#endif
