#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// CANDLE FURY — Vestidor de Niveles (Entrega 3, v4).
///
/// CÓMO USAR: abre Level1 → Candle Fury → Vestir Nivel → Ctrl+S. Repite en
/// Level2. Verificar fondo con Candle Fury → Diagnosticar Fondo.
///
/// v4:
///   • SUELOS/PLATAFORMAS en DOS capas hijas: "Visual_Top" (franja de
///     césped/borde de piedra, tileada solo horizontal, anclada al borde
///     superior del collider) y "Visual_Fill" (tierra/piedra pura tileada
///     en ambas direcciones hasta el fondo). El collider NO cambia.
///     Usa cesped_top/cesped_fill (L1) y piedra_top/piedra_fill (L2).
///     Plataformas delgadas: solo Top. Idempotente (borra hijos previos).
///   • WaxWall → pilar.png tileado vertical (sin estirar).
///   • SpikePit → hijo "Visual_Pinchos" con pinchos.png tileado a lo ancho,
///     alineado al fondo del foso.
///   • Pickups (PowerUpBase + WaxPickup, escena Y prefabs) → powerup.png
///     con tint por tipo: gota #4AD4FF · mecha #FF6B00 · filo #1A6FFF ·
///     sellada #FFB830 · núcleo #5FCC4F.
///   • JEFES: reemplaza el componente RoyalPyromancer (tipo exacto) por
///     RoyalPyromancerL1 o L2 según la escena, copiando sus campos.
///   • FONDO v3 (sin cambios): 30u de alto, ancho bruto player→meta.
/// </summary>
public static class LevelDresser
{
    const string Dir = "Assets/Sprites/Environment/";
    const string LitMaterialPath =
        "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";

    const float BgHeightUnits  = 30f;
    const float SideMargin     = 30f;
    const float MinAspect      = 2.4f;
    const float ParallaxFactor = 0.15f;
    const float TopBandUnits   = 0.5f;   // alto en mundo de la franja superior
    const float SpikeUnits     = 0.42f;  // alto en mundo de los pinchos
    const float PickupUnits    = 0.5f;   // alto en mundo de los power-ups

    static readonly Color CamBgLevel1 = new Color(0.039f, 0.078f, 0.039f); // #0A140A
    static readonly Color CamBgLevel2 = Color.black;

    [MenuItem("Candle Fury/Vestir Nivel")]
    public static void Dress()
    {
        Scene scene   = SceneManager.GetActiveScene();
        bool isLevel2 = scene.name == "Level2";
        var creados   = new List<string>();
        var faltantes = new List<string>();

        // ── 1. Sprites ─────────────────────────────────────────────────────
        Sprite topSprite  = ImportByHeight(isLevel2 ? "piedra_top.png"  : "cesped_top.png",  TopBandUnits, true,  creados, faltantes);
        Sprite fillSprite = ImportByHeight(isLevel2 ? "piedra_fill.png" : "cesped_fill.png", 1f,           true,  creados, faltantes);
        Sprite bgSprite   = ImportByHeight(isLevel2 ? "catacumbas.png"  : "bosque.png",      BgHeightUnits, false, creados, faltantes);
        // pilar: Sliced con borders (remates arriba/abajo fijos, fuste estirable)
        Sprite pilar      = ImportByWidth ("pilar.png",   1f,          creados, faltantes,
                                           new Vector4(0f, 14f, 0f, 14f));
        Sprite pinchos    = ImportByHeight("pinchos.png", SpikeUnits,  true,  creados, faltantes);
        Sprite powerup    = ImportByHeight("powerup.png", PickupUnits, false, creados, faltantes);
        Sprite puerta     = ImportByHeight("puerta.png",  2f,          false, creados, faltantes);
        Material litMat   = AssetDatabase.LoadAssetAtPath<Material>(LitMaterialPath);

        // ── 2. Suelos y plataformas: Top + Fill ────────────────────────────
        int vestidas = 0, soloTop = 0;
        if (topSprite != null && fillSprite != null)
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            float topH = topSprite.bounds.size.y;

            foreach (var box in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
            {
                GameObject go = box.gameObject;
                if (go.layer != groundLayer || box.isTrigger) continue;
                if (go.GetComponent<WaxWall>()  != null ||
                    go.GetComponent<FireTrap>() != null ||
                    go.GetComponent<SpikePit>() != null ||
                    go.GetComponent<LevelEnd>() != null ||
                    go.GetComponent<BurningDripEmitter>() != null) continue;

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
                box.size   = worldSize;
                box.offset = worldOffset;

                // El renderer raíz ya no dibuja (las capas hijas lo hacen)
                sr.sprite   = null;
                sr.drawMode = SpriteDrawMode.Simple;

                ClearVisualChildren(go.transform);

                // Capa TOP: anclada al borde superior del collider
                float topY = worldOffset.y + worldSize.y / 2f - topH / 2f;
                CreateVisualChild(go.transform, "Visual_Top", topSprite,
                    new Vector2(worldSize.x, topH),
                    new Vector3(worldOffset.x, topY, 0f), -1, litMat);

                // Capa FILL: desde debajo del top hasta el fondo (si cabe)
                if (worldSize.y > topH + 0.1f)
                {
                    float fillH = worldSize.y - topH + 0.1f;   // 0.1 de traslape
                    float fillY = worldOffset.y - topH / 2f + 0.05f;
                    CreateVisualChild(go.transform, "Visual_Fill", fillSprite,
                        new Vector2(worldSize.x, fillH),
                        new Vector3(worldOffset.x, fillY, 0f), -2, litMat);
                }
                else soloTop++;

                vestidas++;
            }
            creados.Add($"{vestidas} suelos/plataformas en 2 capas (top+fill), {soloTop} delgadas solo con top");
        }

        // ── 3. WaxWall → pilar tileado vertical ────────────────────────────
        int pilares = 0;
        if (pilar != null)
        {
            foreach (var wall in Object.FindObjectsByType<WaxWall>(FindObjectsSortMode.None))
            {
                var box = wall.GetComponent<BoxCollider2D>();
                var sr  = wall.GetComponent<SpriteRenderer>();
                if (box == null || sr == null) continue;

                Vector3 ls = wall.transform.localScale;
                Vector2 worldSize = new Vector2(
                    Mathf.Max(0.05f, box.size.x * Mathf.Abs(ls.x)),
                    Mathf.Max(0.05f, box.size.y * Mathf.Abs(ls.y)));
                Vector2 worldOffset = new Vector2(box.offset.x * ls.x, box.offset.y * ls.y);

                Undo.RecordObject(wall.transform, "vestir pilar");
                Undo.RecordObject(sr,  "vestir pilar");
                Undo.RecordObject(box, "vestir pilar");

                wall.transform.localScale = Vector3.one;
                sr.sprite   = pilar;
                sr.color    = Color.white;
                // SLICED: un solo pilar por columna — los remates (borders del
                // import) se conservan y solo el fuste central se estira.
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size     = worldSize;
                if (litMat != null) sr.sharedMaterial = litMat;

                box.size   = worldSize;
                box.offset = worldOffset;
                pilares++;
            }
            if (pilares > 0) creados.Add($"{pilares} pilares corregidos: UN pilar.png por columna (Sliced, sin repetición)");
        }

        // ── 4. SpikePit → UNA sola fila de pinchos al fondo del foso ───────
        int fosos = 0;
        if (pinchos != null)
        {
            float spikeH = pinchos.bounds.size.y;   // alto NATIVO de una fila (mundo)
            foreach (var pit in Object.FindObjectsByType<SpikePit>(FindObjectsSortMode.None))
            {
                var box = pit.GetComponent<BoxCollider2D>();
                if (box == null) continue;

                ClearVisualChildren(pit.transform);

                // El hijo lleva ESCALA INVERSA al padre: así su size queda en
                // unidades de mundo reales y el tiled no apila filas cuando el
                // SpikePit tiene escala en el transform.
                Vector3 ls = pit.transform.localScale;
                float sx = Mathf.Abs(ls.x) < 0.01f ? 1f : ls.x;
                float sy = Mathf.Abs(ls.y) < 0.01f ? 1f : ls.y;
                float worldW = Mathf.Max(0.1f, box.size.x * Mathf.Abs(sx));

                // fondo del foso en local del padre + media fila hacia arriba
                float yLocal = box.offset.y - box.size.y / 2f + (spikeH / 2f) / sy;

                CreateVisualChild(pit.transform, "Visual_Pinchos", pinchos,
                    new Vector2(worldW, spikeH),                       // ALTO NATIVO: una fila
                    new Vector3(box.offset.x, yLocal, 0f), 1, litMat,
                    new Vector3(1f / sx, 1f / sy, 1f));                // anula la escala del padre
                fosos++;
            }
            if (fosos > 0) creados.Add($"{fosos} SpikePits corregidos: UNA fila de pinchos ({spikeH:0.##}u) al fondo del foso");
        }

        // ── 5. Pickups: escena + prefabs ───────────────────────────────────
        if (powerup != null)
        {
            int enEscena = 0;
            foreach (var p in Object.FindObjectsByType<PowerUpBase>(FindObjectsSortMode.None))
            { ApplyPickupLook(p.gameObject, p.GetType().Name, powerup, litMat, true); enEscena++; }
            foreach (var wpk in Object.FindObjectsByType<WaxPickup>(FindObjectsSortMode.None))
            { ApplyPickupLook(wpk.gameObject, "WaxPickup", powerup, litMat, true); enEscena++; }
            creados.Add($"{enEscena} pickups de la escena con powerup.png + tint por tipo");

            int enPrefabs = 0;
            foreach (string path in System.IO.Directory.GetFiles("Assets/Prefabs", "*.prefab"))
            {
                string assetPath = path.Replace('\\', '/');
                GameObject root = PrefabUtility.LoadPrefabContents(assetPath);
                string tipo = null;
                var pu = root.GetComponent<PowerUpBase>();
                if (pu != null) tipo = pu.GetType().Name;
                else if (root.GetComponent<WaxPickup>() != null) tipo = "WaxPickup";

                if (tipo != null && root.GetComponent<SpriteRenderer>() != null)
                {
                    ApplyPickupLook(root, tipo, powerup, litMat, false);
                    PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                    enPrefabs++;
                }
                PrefabUtility.UnloadPrefabContents(root);
            }
            creados.Add($"{enPrefabs} prefabs de pickup actualizados en Assets/Prefabs");
        }

        // ── 5b. Puertas en las metas (solo visual, trigger intacto) ────────
        int puertas = 0;
        if (puerta != null)
        {
            float doorH = puerta.bounds.size.y;
            foreach (var le in Object.FindObjectsByType<LevelEnd>(FindObjectsSortMode.None))
            {
                GameObject go = le.gameObject;

                // apagar el placeholder del propio objeto Meta si tenía sprite
                var srMeta = go.GetComponent<SpriteRenderer>();
                if (srMeta != null && srMeta.sprite != null)
                {
                    Undo.RecordObject(srMeta, "meta sin placeholder");
                    srMeta.sprite = null;
                }

                // hijo Visual_Puerta apoyado en el fondo del trigger
                Transform prev = go.transform.Find("Visual_Puerta");
                if (prev != null) Undo.DestroyObjectImmediate(prev.gameObject);

                Vector3 ls = go.transform.localScale;
                float sx = Mathf.Abs(ls.x) < 0.01f ? 1f : ls.x;
                float sy = Mathf.Abs(ls.y) < 0.01f ? 1f : ls.y;
                var boxM = go.GetComponent<BoxCollider2D>();
                float yLocal = boxM != null
                    ? boxM.offset.y - boxM.size.y / 2f + (doorH / 2f) / sy
                    : 0f;
                float xLocal = boxM != null ? boxM.offset.x : 0f;

                GameObject door = new GameObject("Visual_Puerta");
                Undo.RegisterCreatedObjectUndo(door, "crear puerta");
                door.transform.SetParent(go.transform, false);
                door.transform.localPosition = new Vector3(xLocal, yLocal, 0f);
                door.transform.localScale    = new Vector3(1f / sx, 1f / sy, 1f);

                var srD = door.AddComponent<SpriteRenderer>();
                srD.sprite       = puerta;
                srD.color        = Color.white;
                srD.drawMode     = SpriteDrawMode.Simple;
                srD.sortingOrder = -5;   // delante del fondo, detrás de plataformas
                if (litMat != null) srD.sharedMaterial = litMat;
                puertas++;
            }
            if (puertas > 0)
                creados.Add($"{puertas} metas con puerta.png ({doorH:0.#}u de alto, trigger intacto" +
                            (isLevel2 ? ", iluminada por su LuzMeta verde)" : ")"));
        }

        // ── 6. Separar jefes: RoyalPyromancer → L1 / L2 ────────────────────
        var genericos = Object.FindObjectsByType<RoyalPyromancer>(FindObjectsSortMode.None)
                              .Where(p => p.GetType() == typeof(RoyalPyromancer)).ToList();
        foreach (var old in genericos)
        {
            GameObject go = old.gameObject;
            var nu = isLevel2
                ? (RoyalPyromancer)Undo.AddComponent(go, typeof(RoyalPyromancerL2))
                : (RoyalPyromancer)Undo.AddComponent(go, typeof(RoyalPyromancerL1));

            // Copiar todos los campos serializados (prefabs de orbe, castPoint, etc.)
            var soOld = new SerializedObject(old);
            var soNew = new SerializedObject(nu);
            SerializedProperty it = soOld.GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                enter = false;
                if (it.name == "m_Script") continue;
                soNew.CopyFromSerializedProperty(it);
            }
            soNew.ApplyModifiedProperties();
            Undo.DestroyObjectImmediate(old);
            creados.Add($"Jefe '{go.name}' convertido a {(isLevel2 ? "RoyalPyromancerL2" : "RoyalPyromancerL1")}");
        }
        if (genericos.Count == 0)
        {
            var yaSeparado = Object.FindAnyObjectByType<RoyalPyromancer>();
            if (yaSeparado != null)
                creados.Add($"Jefe ya separado: {yaSeparado.GetType().Name} (sin cambios)");
        }

        // ── 6b. Orbe propio del jefe final (solo Level2) ───────────────────
        // MagicOrb_L2.prefab: copia violeta #7A60FF con luz propia (el
        // ProjectileGlow de runtime respeta la luz que ya trae el prefab).
        // El MagicOrb original de L1 NO se toca.
        if (isLevel2)
        {
            const string orbSrc = "Assets/Prefabs/MagicOrb.prefab";
            const string orbDst = "Assets/Prefabs/MagicOrb_L2.prefab";
            Color violeta = new Color(0.478f, 0.376f, 1f);   // #7A60FF

            if (AssetDatabase.LoadAssetAtPath<GameObject>(orbDst) == null)
            {
                if (AssetDatabase.CopyAsset(orbSrc, orbDst))
                    creados.Add("Prefab MagicOrb_L2 creado (copia de MagicOrb — el original queda intacto)");
                else
                    faltantes.Add($"{orbSrc} (no pude copiarlo a MagicOrb_L2)");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(orbDst) != null)
            {
                GameObject orbRoot = PrefabUtility.LoadPrefabContents(orbDst);
                var srO = orbRoot.GetComponent<SpriteRenderer>();
                if (srO != null) srO.color = violeta;

                Transform glowT = orbRoot.transform.Find("Glow");
                UnityEngine.Rendering.Universal.Light2D lg;
                if (glowT == null)
                {
                    var g = new GameObject("Glow");
                    g.transform.SetParent(orbRoot.transform, false);
                    lg = g.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
                }
                else
                {
                    lg = glowT.GetComponent<UnityEngine.Rendering.Universal.Light2D>();
                    if (lg == null) lg = glowT.gameObject.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
                }
                lg.lightType             = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
                lg.color                 = violeta;
                lg.pointLightOuterRadius = 1f;
                lg.pointLightInnerRadius = 0.1f;
                lg.falloffIntensity      = 0.7f;
                lg.intensity             = 1.2f;
                PrefabUtility.SaveAsPrefabAsset(orbRoot, orbDst);
                PrefabUtility.UnloadPrefabContents(orbRoot);

                var bossL2 = Object.FindAnyObjectByType<RoyalPyromancerL2>();
                if (bossL2 != null)
                {
                    var soB = new SerializedObject(bossL2);
                    soB.FindProperty("magicOrbL2Prefab").objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<GameObject>(orbDst);
                    soB.ApplyModifiedProperties();
                    creados.Add("MagicOrb_L2 (#7A60FF con luz a juego) asignado a RoyalPyromancerL2");
                }
                else faltantes.Add("RoyalPyromancerL2 en escena (¿el swap de jefe corrió bien?)");
            }
        }

        // ── 7. Cámara + fondo (v3, sin cambios de lógica) ──────────────────
        var playerCtrl = Object.FindAnyObjectByType<PlayerController>();
        var meta       = Object.FindAnyObjectByType<LevelEnd>();
        float playerX = playerCtrl != null ? playerCtrl.transform.position.x : -5f;
        float playerY = playerCtrl != null ? playerCtrl.transform.position.y : 0f;
        float metaX   = meta       != null ? meta.transform.position.x       : 85f;

        Camera cam  = Camera.main;
        float ortho = cam != null ? cam.orthographicSize : 7f;
        float visW  = 2f * ortho * Mathf.Max(cam != null ? cam.aspect : MinAspect, MinAspect);
        if (cam != null)
        {
            Undo.RecordObject(cam, "fondo de cámara");
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = isLevel2 ? CamBgLevel2 : CamBgLevel1;
        }

        if (bgSprite != null)
        {
            GameObject old = GameObject.Find("Background");
            if (old != null) Undo.DestroyObjectImmediate(old);

            float coverLeft  = playerX - SideMargin - visW / 2f;
            float coverRight = metaX   + SideMargin + visW / 2f;
            float totalWidth = coverRight - coverLeft;
            float centerX    = (coverLeft + coverRight) / 2f;
            float centerY    = playerY + 5f;

            GameObject bgParent = new GameObject("Background");
            Undo.RegisterCreatedObjectUndo(bgParent, "crear Background");
            bgParent.transform.position = new Vector3(centerX, centerY, 0f);

            float tileW  = bgSprite.bounds.size.x;
            int   nTiles = Mathf.CeilToInt(totalWidth / tileW) + 1;
            float firstX = -(nTiles * tileW) / 2f + tileW / 2f;
            Vector2 realTile = Vector2.zero;

            for (int i = 0; i < nTiles; i++)
            {
                GameObject tile = new GameObject($"BG_Tile_{i + 1}");
                tile.transform.SetParent(bgParent.transform, false);
                tile.transform.localPosition = new Vector3(firstX + i * tileW, 0f, 0f);
                var tsr = tile.AddComponent<SpriteRenderer>();
                tsr.sprite       = bgSprite;
                tsr.sortingOrder = -50;
                if (litMat != null) tsr.sharedMaterial = litMat;
                realTile = tsr.bounds.size;
            }

            var par = bgParent.AddComponent<ParallaxLayer>();
            var soP = new SerializedObject(par);
            soP.FindProperty("parallaxFactor").floatValue = ParallaxFactor;
            soP.FindProperty("followY").boolValue         = false;
            soP.ApplyModifiedProperties();

            creados.Add($"Fondo: {nTiles} tiles, total {nTiles * tileW:0.#} x {realTile.y:0.#} u " +
                        (realTile.y < 2f * ortho ? "⚠ MÁS BAJO QUE LA CÁMARA" : "(alto de sobra ✔)"));
        }

        // ── Resumen ────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"══════ VESTIR NIVEL v4 ({scene.name}) — RESUMEN ══════");
        foreach (var c in creados)   Debug.Log("  ✔ " + c);
        foreach (var f in faltantes) Debug.LogWarning("  ✘ FALTA: " + f);
        Debug.Log("Verifica con 'Candle Fury → Diagnosticar Fondo'. Guarda con Ctrl+S y repite en la otra escena.");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    static void ClearVisualChildren(Transform parent)
    {
        foreach (Transform child in parent.Cast<Transform>().ToList())
            if (child.name.StartsWith("Visual_"))
                Undo.DestroyObjectImmediate(child.gameObject);
    }

    static void CreateVisualChild(Transform parent, string name, Sprite sprite,
                                  Vector2 size, Vector3 localPos, int order, Material litMat,
                                  Vector3? localScale = null)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "crear " + name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        if (localScale.HasValue) go.transform.localScale = localScale.Value;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = sprite;
        sr.color        = Color.white;
        sr.drawMode     = SpriteDrawMode.Tiled;
        sr.size         = size;
        sr.sortingOrder = order;
        if (litMat != null) sr.sharedMaterial = litMat;
    }

    static void ApplyPickupLook(GameObject go, string typeName, Sprite sprite,
                                Material litMat, bool withUndo)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        if (withUndo) { Undo.RecordObject(sr, "pickup"); Undo.RecordObject(go.transform, "pickup"); }

        // Sprite en BLANCO puro: el tint multiplicativo oscurecía el arte.
        // La identidad por tipo va en la LUZ de color (halo), no en el sprite.
        sr.sprite   = sprite;
        sr.drawMode = SpriteDrawMode.Simple;
        sr.color    = Color.white;
        if (litMat != null) sr.sharedMaterial = litMat;

        float sign = Mathf.Sign(go.transform.localScale.x);
        go.transform.localScale = new Vector3(sign, 1f, 1f);

        // Trigger generoso alrededor del sprite nuevo
        foreach (var cc in go.GetComponents<CircleCollider2D>())
        { if (withUndo) Undo.RecordObject(cc, "pickup"); cc.radius = 0.35f; cc.offset = Vector2.zero; }
        foreach (var bb in go.GetComponents<BoxCollider2D>())
        { if (withUndo) Undo.RecordObject(bb, "pickup"); bb.size = new Vector2(0.55f, 0.75f); bb.offset = Vector2.zero; }

        // Luz de color por tipo (visible también con la luz global de Level1)
        Transform luz = go.transform.Find("LuzPickup");
        UnityEngine.Rendering.Universal.Light2D l;
        if (luz == null)
        {
            GameObject lgo = new GameObject("LuzPickup");
            if (withUndo) Undo.RegisterCreatedObjectUndo(lgo, "LuzPickup");
            lgo.transform.SetParent(go.transform, false);
            l = lgo.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        }
        else
        {
            l = luz.GetComponent<UnityEngine.Rendering.Universal.Light2D>();
            if (l == null) l = luz.gameObject.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            if (withUndo) Undo.RecordObject(l, "LuzPickup");
        }
        l.lightType             = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        l.color                 = TintFor(typeName);
        l.pointLightOuterRadius = 1.2f;
        l.pointLightInnerRadius = 0.2f;
        l.falloffIntensity      = 0.7f;
        l.intensity             = 1.5f;
    }

    static Color TintFor(string typeName)
    {
        switch (typeName)
        {
            case "WaxDropPickup":
            case "WaxPickup":           return new Color(0.290f, 0.831f, 1f);     // #4AD4FF gota
            case "ReinforcedWickPickup":return new Color(1f, 0.420f, 0f);          // #FF6B00 mecha
            case "BurningEdgePickup":   return new Color(0.102f, 0.435f, 1f);      // #1A6FFF filo
            case "SealedWaxPickup":     return new Color(1f, 0.722f, 0.188f);      // #FFB830 sellada
            case "FlameCorePickup":     return new Color(0.373f, 0.800f, 0.310f);  // #5FCC4F núcleo
            default:                    return Color.white;
        }
    }

    // ── Import: PPU por alto o por ancho objetivo en unidades ──────────────
    static Sprite ImportByHeight(string file, float worldHeight, bool tileable,
                                 List<string> creados, List<string> faltantes)
        => Import(file, tex => tex.height / worldHeight, tileable, creados, faltantes, null);

    static Sprite ImportByWidth(string file, float worldWidth,
                                List<string> creados, List<string> faltantes,
                                Vector4? border = null)
        => Import(file, tex => tex.width / worldWidth, true, creados, faltantes, border);

    static Sprite Import(string file, System.Func<Texture2D, float> ppuCalc, bool tileable,
                         List<string> creados, List<string> faltantes, Vector4? border)
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
        // Borders para Draw Mode Sliced (remates del pilar)
        if (border.HasValue && imp.spriteBorder != border.Value)
        {
            imp.spriteBorder = border.Value;
            dirty = true;
        }
        if (dirty) imp.SaveAndReimport();

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null)
        {
            Debug.LogError($"[Vestir Nivel] '{path}' no cargó como Texture2D.");
            faltantes.Add($"{path} (no cargó)");
            return null;
        }

        float ppu = Mathf.Max(1f, Mathf.Round(ppuCalc(tex)));
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
        creados.Add($"{file}: {tex.width}x{tex.height}px, PPU {imp.spritePixelsPerUnit} " +
                    $"→ {sp.bounds.size.x:0.##} x {sp.bounds.size.y:0.##} u");
        return sp;
    }
}
#endif
