#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// CANDLE FURY — Asignador de Sprites oficiales del GDD (Entrega 3).
///
/// CÓMO USAR:
/// 1. Verifica que existan los 4 PNG en Assets/Sprites/Characters/:
///    principe_vela.png · wax_beast.png · candle_guard.png · royal_pyromancer.png
/// 2. Abre Level1 → menú Candle Fury → Asignar Sprites → revisa Console → Ctrl+S
/// 3. Repite en Level2
///
/// Qué hace (idempotente, con Undo):
///   • IMPORT: fuerza Sprite/Single, Point filter, sin compresión, y calcula
///     el Pixels Per Unit según el alto REAL de cada textura para que:
///     Player ≈ 1.2 u de alto · Guard ≈ 1.3 u · Pirómante ≈ 1.56 u (1.3×player)
///     · Beast ≈ 0.9 u (es más ancho que alto)
///   • Asigna cada sprite buscando POR COMPONENTE (PlayerController,
///     WaxBeast, CandleGuard, RoyalPyromancer), no por nombre de objeto
///   • Resetea el tint a blanco y el material a Sprite-Lit-Default
///   • Normaliza localScale a (±1, 1, 1) conservando el signo de X (flip)
///   • Reajusta Box/CapsuleCollider2D al sprite con margen (75% ancho, 95% alto)
///   • Player: GroundCheck a los pies, AttackPoint al frente,
///     ELIMINA la llama placeholder (hijo "Flame"/"FlameVisual") y pone
///     FlameLightFlicker en LuzLlama (el sprite oficial ya trae la llama dibujada)
///
/// DIRECCIÓN: el código asume sprites mirando A LA DERECHA. Si alguno mira
/// a la izquierda, agrega su nombre de archivo a MiraIzquierda (abajo).
/// </summary>
public static class SpriteAssigner
{
    const string Dir           = "Assets/Sprites/Characters/";
    const string LitShaderName = "Universal Render Pipeline/2D/Sprite-Lit-Default";
    const string LitMaterialPath =
        "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";

    // Archivo → alto deseado en unidades de mundo
    static readonly (string file, float worldHeight)[] Imports =
    {
        ("principe_vela.png",    1.20f),
        ("wax_beast.png",        0.90f),  // más ancho que alto
        ("candle_guard.png",     1.30f),
        ("royal_pyromancer.png", 1.56f),  // jefe: 1.3× el alto del player
    };

    // Sprites dibujados mirando a la IZQUIERDA (se compensan con flipX).
    // Revisar visualmente cada PNG y agregar aquí el nombre si aplica.
    static readonly HashSet<string> MiraIzquierda = new HashSet<string>
    {
        // "candle_guard.png",
    };

    [MenuItem("Candle Fury/Asignar Sprites")]
    public static void Assign()
    {
        Scene scene   = SceneManager.GetActiveScene();
        var creados   = new List<string>();
        var faltantes = new List<string>();

        // ── 1. Import settings + PPU calculado del alto real ───────────────
        var sprites = new Dictionary<string, Sprite>();
        foreach (var (file, worldHeight) in Imports)
        {
            string path = Dir + file;
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null)
            {
                faltantes.Add($"{path} (no existe o Unity aún no lo importó)");
                continue;
            }

            bool dirty = false;
            if (imp.textureType        != TextureImporterType.Sprite)          { imp.textureType        = TextureImporterType.Sprite;          dirty = true; }
            if (imp.spriteImportMode   != SpriteImportMode.Single)             { imp.spriteImportMode   = SpriteImportMode.Single;             dirty = true; }
            if (imp.filterMode         != FilterMode.Point)                    { imp.filterMode         = FilterMode.Point;                    dirty = true; }
            if (imp.textureCompression != TextureImporterCompression.Uncompressed)
                                                                               { imp.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
            if (dirty) imp.SaveAndReimport();

            // Leer el alto real de la textura ya importada y derivar el PPU
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) { faltantes.Add($"{path} (no cargó como Texture2D)"); continue; }

            float ppu = Mathf.Round(tex.height / worldHeight);
            if (Mathf.Abs(imp.spritePixelsPerUnit - ppu) > 0.5f)
            {
                imp.spritePixelsPerUnit = ppu;
                imp.SaveAndReimport();
            }

            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp == null) { faltantes.Add($"{path} (no generó Sprite — ¿es un PNG válido?)"); continue; }

            sprites[file] = sp;
            creados.Add($"{file}: {tex.width}x{tex.height}px, PPU {ppu} → {worldHeight:0.00} u de alto");
        }

        Material litMat = AssetDatabase.LoadAssetAtPath<Material>(LitMaterialPath);

        // ── 2. Asignar por componente ──────────────────────────────────────
        int count;

        // Player
        count = 0;
        foreach (var pc in Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (!sprites.TryGetValue("principe_vela.png", out var sp)) break;
            ApplyCharacterSprite(pc.gameObject, sp, "principe_vela.png", litMat);
            SetupPlayerExtras(pc.gameObject, sp, creados);
            count++;
        }
        if (count > 0) creados.Add($"Player: sprite principe_vela asignado ({count})");

        count = AssignAll<WaxBeast>(sprites, "wax_beast.png", litMat);
        if (count > 0) creados.Add($"WaxBeast: sprite asignado a {count}");

        count = AssignAll<CandleGuard>(sprites, "candle_guard.png", litMat);
        if (count > 0) creados.Add($"CandleGuard: sprite asignado a {count}");

        count = AssignAll<RoyalPyromancer>(sprites, "royal_pyromancer.png", litMat);
        if (count > 0) creados.Add($"RoyalPyromancer: sprite asignado a {count}");

        // ── Resumen ────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"══════ ASIGNAR SPRITES ({scene.name}) — RESUMEN ══════");
        foreach (var c in creados)   Debug.Log("  ✔ " + c);
        foreach (var f in faltantes) Debug.LogWarning("  ✘ FALTA: " + f);
        Debug.Log("Recuerda GUARDAR la escena (Ctrl+S) y repetir en la otra escena.");
    }

    // ── Asigna el sprite a todos los objetos con el componente T ───────────
    static int AssignAll<T>(Dictionary<string, Sprite> sprites, string file, Material litMat)
        where T : Component
    {
        if (!sprites.TryGetValue(file, out var sp)) return 0;
        int n = 0;
        foreach (var comp in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
        {
            ApplyCharacterSprite(comp.gameObject, sp, file, litMat);
            n++;
        }
        return n;
    }

    // ── Sprite + tint blanco + material lit + escala normalizada + colliders ──
    static void ApplyCharacterSprite(GameObject go, Sprite sprite, string file, Material litMat)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        Undo.RecordObject(sr, "asignar sprite");
        sr.sprite = sprite;
        sr.color  = Color.white;                       // limpiar tint del placeholder
        sr.flipX  = MiraIzquierda.Contains(file);      // compensar si mira a la izquierda
        if (litMat != null &&
            (sr.sharedMaterial == null || sr.sharedMaterial.shader == null ||
             sr.sharedMaterial.shader.name != LitShaderName))
            sr.sharedMaterial = litMat;

        // Escala (±1,1,1): el PPU ya deja el tamaño correcto; conservar flip
        Undo.RecordObject(go.transform, "normalizar escala");
        float sign = Mathf.Sign(go.transform.localScale.x);
        go.transform.localScale = new Vector3(sign * 1f, 1f, 1f);

        // Colliders del propio objeto, ajustados al sprite con margen
        Bounds b = sprite.bounds;   // en unidades de mundo (escala 1)
        foreach (var box in go.GetComponents<BoxCollider2D>())
        {
            Undo.RecordObject(box, "ajustar collider");
            box.size   = new Vector2(b.size.x * 0.75f, b.size.y * 0.95f);
            box.offset = new Vector2(b.center.x, b.center.y);
        }
        foreach (var cap in go.GetComponents<CapsuleCollider2D>())
        {
            Undo.RecordObject(cap, "ajustar collider");
            cap.size      = new Vector2(b.size.x * 0.65f, b.size.y * 0.98f);
            cap.offset    = new Vector2(b.center.x, b.center.y);
            cap.direction = b.size.y >= b.size.x ? CapsuleDirection2D.Vertical
                                                 : CapsuleDirection2D.Horizontal;
        }
    }

    // ── Extras del Player: GroundCheck, AttackPoint y la llama ─────────────
    static void SetupPlayerExtras(GameObject player, Sprite sprite, List<string> creados)
    {
        Bounds b = sprite.bounds;

        Transform groundCheck = player.transform.Find("GroundCheck");
        if (groundCheck != null)
        {
            Undo.RecordObject(groundCheck, "mover GroundCheck");
            groundCheck.localPosition = new Vector3(0f, b.min.y + 0.02f, 0f);
            creados.Add("GroundCheck reposicionado a los pies del sprite");
        }

        Transform attackPoint = player.transform.Find("AttackPoint");
        if (attackPoint != null)
        {
            Undo.RecordObject(attackPoint, "mover AttackPoint");
            attackPoint.localPosition = new Vector3(b.extents.x + 0.3f, b.center.y + 0.05f, 0f);
            creados.Add("AttackPoint reposicionado al frente del sprite");
        }

        // El sprite oficial YA trae la llama azul dibujada:
        // eliminar la llama placeholder (hijo con SpriteRenderer)
        foreach (string flameName in new[] { "Flame", "FlameVisual" })
        {
            Transform old = player.transform.Find(flameName);
            if (old != null && old.GetComponent<SpriteRenderer>() != null)
            {
                Undo.DestroyObjectImmediate(old.gameObject);
                creados.Add($"Llama placeholder '{flameName}' eliminada (el sprite ya la trae)");
            }
        }

        // FlameLightFlicker: el parpadeo ahora vive en la LUZ del player
        Transform luz = player.transform.Find("LuzLlama");
        if (luz == null)
        {
            GameObject go = new GameObject("LuzLlama");
            Undo.RegisterCreatedObjectUndo(go, "crear LuzLlama");
            go.transform.SetParent(player.transform, false);
            go.transform.localPosition = new Vector3(0f, b.max.y + 0.05f, 0f); // sobre la mecha
            var l = go.AddComponent<Light2D>();
            l.lightType             = Light2D.LightType.Point;
            l.color                 = new Color(0.290f, 0.831f, 1f); // #4AD4FF
            l.pointLightOuterRadius = 4f;
            l.intensity             = 1.1f;
            luz = go.transform;
            creados.Add("LuzLlama creada (ejecuta 'Aplicar Estética' para el ajuste fino)");
        }

        FlameLightFlicker flick = luz.GetComponent<FlameLightFlicker>();
        if (flick == null)
        {
            flick = Undo.AddComponent<FlameLightFlicker>(luz.gameObject);
            creados.Add("FlameLightFlicker agregado a LuzLlama");
        }
        var so = new SerializedObject(flick);
        so.FindProperty("syncWithWax").boolValue          = true;
        so.FindProperty("waxSystem").objectReferenceValue = player.GetComponent<WaxSystem>();
        so.ApplyModifiedProperties();
        creados.Add("FlameLightFlicker sincronizado con WaxSystem");
    }
}
#endif
