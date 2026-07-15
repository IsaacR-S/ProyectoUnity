#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// CANDLE FURY — Vestidor de Niveles con arte de entorno (Entrega 3, v3).
///
/// CÓMO USAR:
/// 1. Assets/Sprites/Environment/ debe tener:
///    cesped.png · bosque.png (Level1) · piedra.png · catacumbas.png (Level2)
/// 2. Abre Level1 → menú Candle Fury → Vestir Nivel → Console → Ctrl+S
/// 3. Repite en Level2. Verificar con Candle Fury → Diagnosticar Fondo.
///
/// v3 — dimensionado BRUTO (preferimos tiles invisibles a bordes visibles):
///   • Alto del fondo: 30 unidades (la cámara es orto 7 → 14 visibles),
///     centrado en la Y del PLAYER + 5 (no en la cámara del editor).
///   • Ancho: desde (playerX − 30) hasta (metaX + 30) MÁS el ancho visible
///     de cámara (leído del Camera.main real, con aspecto mínimo 2.4 por
///     si la ventana de Game es ultrawide). Sin descontar el ahorro del
///     parallax: sobredimensionar es barato.
///   • El Background se BORRA y reconstruye de cero. El resumen imprime el
///     tamaño REAL en mundo de cada tile (bounds del renderer instanciado).
///   • Fondo de cámara: L1 #0A140A, L2 #000000.
/// Requiere ParallaxLayer v2 (captura su referencia en el 2º frame).
/// </summary>
public static class LevelDresser
{
    const string Dir = "Assets/Sprites/Environment/";
    const string LitMaterialPath =
        "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";

    const float BgHeightUnits  = 30f;   // alto bruto del fondo
    const float SideMargin     = 30f;   // margen antes del player y después de la meta
    const float MinAspect      = 2.4f;  // por si la ventana de Game es ultrawide
    const float ParallaxFactor = 0.15f;

    static readonly Color CamBgLevel1 = new Color(0.039f, 0.078f, 0.039f); // #0A140A
    static readonly Color CamBgLevel2 = Color.black;                       // #000000

    [MenuItem("Candle Fury/Vestir Nivel")]
    public static void Dress()
    {
        Scene scene   = SceneManager.GetActiveScene();
        bool isLevel2 = scene.name == "Level2";
        var creados   = new List<string>();
        var faltantes = new List<string>();

        string platFile = isLevel2 ? "piedra.png"     : "cesped.png";
        string bgFile   = isLevel2 ? "catacumbas.png" : "bosque.png";

        Sprite platSprite = ImportSprite(platFile, true,  creados, faltantes);
        Sprite bgSprite   = ImportSprite(bgFile,   false, creados, faltantes);
        Material litMat   = AssetDatabase.LoadAssetAtPath<Material>(LitMaterialPath);

        // ── 1. Plataformas y suelos (igual que v2 — ya funcionaba) ─────────
        int vestidas = 0, saltadas = 0;
        if (platSprite != null)
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            foreach (var box in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
            {
                GameObject go = box.gameObject;
                if (go.layer != groundLayer || box.isTrigger) continue;
                if (go.GetComponent<WaxWall>()  != null ||
                    go.GetComponent<FireTrap>() != null ||
                    go.GetComponent<SpikePit>() != null ||
                    go.GetComponent<LevelEnd>() != null ||
                    go.GetComponent<BurningDripEmitter>() != null) { saltadas++; continue; }

                var sr = go.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                Vector3 ls = go.transform.localScale;
                Vector2 worldSize = new Vector2(
                    Mathf.Max(0.05f, box.size.x * Mathf.Abs(ls.x)),
                    Mathf.Max(0.05f, box.size.y * Mathf.Abs(ls.y)));
                Vector2 worldOffset = new Vector2(box.offset.x * ls.x, box.offset.y * ls.y);

                Undo.RecordObject(go.transform, "vestir plataforma");
                Undo.RecordObject(sr,  "vestir plataforma");
                Undo.RecordObject(box, "vestir plataforma");

                go.transform.localScale = Vector3.one;
                sr.sprite   = platSprite;
                sr.color    = Color.white;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size     = worldSize;
                if (litMat != null) sr.sharedMaterial = litMat;

                box.size   = worldSize;
                box.offset = worldOffset;
                vestidas++;
            }
            creados.Add($"{vestidas} plataformas/suelos vestidos con {platFile}" +
                        (saltadas > 0 ? $" ({saltadas} excluidos a propósito)" : ""));
        }

        // ── 2. Posiciones REALES de player, meta y cámara ──────────────────
        var playerCtrl = Object.FindAnyObjectByType<PlayerController>();
        var meta       = Object.FindAnyObjectByType<LevelEnd>();
        float playerX = playerCtrl != null ? playerCtrl.transform.position.x : -5f;
        float playerY = playerCtrl != null ? playerCtrl.transform.position.y : 0f;
        float metaX   = meta       != null ? meta.transform.position.x       : 85f;
        if (playerCtrl == null) faltantes.Add("Player (usé x=-5 por defecto)");
        if (meta       == null) faltantes.Add("Meta/LevelEnd (usé x=85 por defecto)");

        Camera cam  = Camera.main;
        float ortho = cam != null ? cam.orthographicSize : 7f;
        float visW  = 2f * ortho * Mathf.Max(cam != null ? cam.aspect : MinAspect, MinAspect);
        if (cam != null)
        {
            Undo.RecordObject(cam, "fondo de cámara");
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = isLevel2 ? CamBgLevel2 : CamBgLevel1;
            creados.Add($"Cámara: orto {ortho} (ve {2 * ortho:0.#}u de alto, hasta {visW:0.#}u de ancho) · " +
                        $"fondo {(isLevel2 ? "#000000" : "#0A140A")}");
        }
        else faltantes.Add("Main Camera");

        // ── 3. Background v3: bruto, imposible ver bordes ──────────────────
        if (bgSprite != null)
        {
            GameObject old = GameObject.Find("Background");
            if (old != null)
            {
                Undo.DestroyObjectImmediate(old);
                creados.Add("Background anterior eliminado");
            }

            float coverLeft  = playerX - SideMargin - visW / 2f;
            float coverRight = metaX   + SideMargin + visW / 2f;
            float totalWidth = coverRight - coverLeft;
            float centerX    = (coverLeft + coverRight) / 2f;
            float centerY    = playerY + 5f;   // cubre de sobra hacia arriba (torres) y abajo

            GameObject bgParent = new GameObject("Background");
            Undo.RegisterCreatedObjectUndo(bgParent, "crear Background");
            bgParent.transform.position = new Vector3(centerX, centerY, 0f);

            float tileW  = bgSprite.bounds.size.x;
            int   nTiles = Mathf.CeilToInt(totalWidth / tileW) + 1;   // +1 tile de propina
            float firstX = -(nTiles * tileW) / 2f + tileW / 2f;

            Vector2 realTile = Vector2.zero;
            for (int i = 0; i < nTiles; i++)
            {
                GameObject tile = new GameObject($"BG_Tile_{i + 1}");
                tile.transform.SetParent(bgParent.transform, false);
                tile.transform.localPosition = new Vector3(firstX + i * tileW, 0f, 0f);

                var tsr = tile.AddComponent<SpriteRenderer>();
                tsr.sprite       = bgSprite;
                tsr.color        = Color.white;
                tsr.sortingOrder = -50;
                if (litMat != null) tsr.sharedMaterial = litMat;

                realTile = tsr.bounds.size;   // tamaño REAL en mundo, del renderer vivo
            }

            var par = bgParent.AddComponent<ParallaxLayer>();
            var so = new SerializedObject(par);
            so.FindProperty("parallaxFactor").floatValue = ParallaxFactor;
            so.FindProperty("followY").boolValue         = false;
            so.ApplyModifiedProperties();

            creados.Add($"Background nuevo: {nTiles} tiles de {bgFile}");
            creados.Add($"  · Tile REAL en mundo: {realTile.x:0.##} x {realTile.y:0.##} unidades " +
                        (realTile.y < 2f * ortho ? "⚠ MÁS BAJO QUE LA CÁMARA — revisar PPU" : "(cubre el alto de cámara ✔)"));
            creados.Add($"  · Total: {nTiles * tileW:0.#} x {realTile.y:0.#} u, " +
                        $"x:[{centerX - nTiles * tileW / 2f:0.#} .. {centerX + nTiles * tileW / 2f:0.#}], " +
                        $"y:[{centerY - realTile.y / 2f:0.#} .. {centerY + realTile.y / 2f:0.#}]");
            creados.Add($"  · Player en ({playerX:0.#}, {playerY:0.#}) · Meta en x={metaX:0.#} · parallax {ParallaxFactor}");
        }
        else
        {
            Debug.LogError($"[Vestir Nivel] NO se colocó el fondo: '{Dir}{bgFile}' no cargó como Sprite.");
        }

        // ── Resumen ────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"══════ VESTIR NIVEL v3 ({scene.name}) — RESUMEN ══════");
        foreach (var c in creados)   Debug.Log("  ✔ " + c);
        foreach (var f in faltantes) Debug.LogWarning("  ✘ FALTA: " + f);
        Debug.Log("Verifica con 'Candle Fury → Diagnosticar Fondo' (funciona también en Play). Guarda con Ctrl+S.");
    }

    // ── Import settings + carga (reporta motivo exacto si falla) ───────────
    static Sprite ImportSprite(string file, bool tileable,
                               List<string> creados, List<string> faltantes)
    {
        string path = Dir + file;
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null)
        {
            string[] existentes = System.IO.Directory.Exists(Dir)
                ? System.IO.Directory.GetFiles(Dir, "*.png").Select(System.IO.Path.GetFileName).ToArray()
                : new string[0];
            Debug.LogError($"[Vestir Nivel] '{path}' NO EXISTE o no está importado. " +
                           $"En la carpeta hay: {string.Join(", ", existentes)}");
            faltantes.Add($"{path} (no existe / sin importar)");
            return null;
        }

        bool dirty = false;
        if (imp.textureType        != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; dirty = true; }
        if (imp.spriteImportMode   != SpriteImportMode.Single)    { imp.spriteImportMode = SpriteImportMode.Single; dirty = true; }
        if (imp.filterMode         != FilterMode.Point)           { imp.filterMode = FilterMode.Point; dirty = true; }
        if (imp.textureCompression != TextureImporterCompression.Uncompressed)
            { imp.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
        if (tileable && imp.wrapMode != TextureWrapMode.Repeat)   { imp.wrapMode = TextureWrapMode.Repeat; dirty = true; }

        var settings = new TextureImporterSettings();
        imp.ReadTextureSettings(settings);
        if (tileable && settings.spriteMeshType != SpriteMeshType.FullRect)
        {
            settings.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(settings);
            dirty = true;
        }
        if (dirty) imp.SaveAndReimport();

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null)
        {
            Debug.LogError($"[Vestir Nivel] '{path}' no cargó como Texture2D (¿corrupto?).");
            faltantes.Add($"{path} (no cargó)");
            return null;
        }

        float ppu = tileable ? tex.height : Mathf.Max(1f, Mathf.Round(tex.height / BgHeightUnits));
        if (Mathf.Abs(imp.spritePixelsPerUnit - ppu) > 0.5f)
        {
            imp.spritePixelsPerUnit = ppu;
            imp.SaveAndReimport();
        }

        Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp == null)
        {
            Debug.LogError($"[Vestir Nivel] '{path}' no generó Sprite tras el reimport.");
            faltantes.Add($"{path} (sin Sprite)");
            return null;
        }
        creados.Add($"{file} cargó OK: {tex.width}x{tex.height}px, PPU {imp.spritePixelsPerUnit} " +
                    $"→ {sp.bounds.size.x:0.##} x {sp.bounds.size.y:0.##} unidades por tile");
        return sp;
    }
}
#endif
