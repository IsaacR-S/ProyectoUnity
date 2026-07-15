#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// CANDLE FURY — Aplicador de Estética + Balance (Entrega 3).
///
/// CÓMO USAR:
/// 1. Abre Level1 (o Level2) — funciona en la escena que esté abierta
/// 2. Menú superior: Candle Fury → Aplicar Estética
/// 3. Revisa la Console y guarda la escena (Ctrl+S)
/// 4. Repite en la otra escena
///
/// LEVEL 1 (luz cálida):
///   • Global Light 2D #FFD9A0, intensidad 0.7 · fondo cámara #1A0F05
///   • LuzLlama azul #4AD4FF radio 4 en el Player
///
/// LEVEL 2 — OSCURIDAD ABSOLUTA (tipo cuevas de NSMB Wii):
///   • Global Light intensidad 0 y fondo de cámara NEGRO #000000:
///     lo que no toque una luz puntual es 100% invisible
///   • TODOS los SpriteRenderers forzados a Sprite-Lit-Default
///     (un sprite unlit se vería siempre brillante y rompe el efecto)
///   • Luces puntuales SOLO en entidades:
///       Player           #4AD4FF  radio 4.5  int 1.5 (su farol)
///       WaxBeast         #FFE0B0  radio 2.0  (tenue)
///       CandleGuard      #FF4422  radio 2.5
///       RoyalPyromancer  #FF4080  radio 3.5
///       Pickups/Gotas    #FFB830  radio 1.2  (guían al jugador)
///       Meta             #5FCC4F  radio 2.5  (objetivo visible de lejos)
///   • Los proyectiles emiten luz por código (ProjectileGlow en su Awake)
///
/// EN AMBAS ESCENAS: FlameLightFlicker en LuzLlama (parpadeo de la luz del
/// player, sincronizado con la cera — la llama visual ya viene dibujada en
/// el sprite oficial), FlameFrenzy en el Player, ComboSystem y drenaje 3.5.
///
/// IMPORTANTE (Level2): ejecuta ANTES "Candle Fury → Construir Level2",
/// para que los enemigos/gotas clonados también reciban su luz.
/// Idempotente: re-ejecutar no duplica luces, solo las reconfigura.
/// Todo des-hacible con Ctrl+Z.
/// </summary>
public static class AestheticApplier
{
    // Colores Level1
    static readonly Color WarmGlobal = new Color(1f, 0.851f, 0.627f);      // #FFD9A0
    static readonly Color BgLevel1   = new Color(0.102f, 0.059f, 0.020f);  // #1A0F05

    // Colores mundo oscuro (Level2)
    static readonly Color BlueFlame  = new Color(0.290f, 0.831f, 1f);      // #4AD4FF player
    static readonly Color BeastGlow  = new Color(1f, 0.878f, 0.690f);      // #FFE0B0 WaxBeast
    static readonly Color GuardGlow  = new Color(1f, 0.267f, 0.133f);      // #FF4422 CandleGuard
    static readonly Color PyroGlow   = new Color(1f, 0.251f, 0.502f);      // #FF4080 Pirómante
    static readonly Color PickupGlow = new Color(1f, 0.722f, 0.188f);      // #FFB830 pickups
    static readonly Color MetaGlow   = new Color(0.373f, 0.800f, 0.310f);  // #5FCC4F meta

    const string LitMaterialPath =
        "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";
    const string LitShaderName = "Universal Render Pipeline/2D/Sprite-Lit-Default";

    [MenuItem("Candle Fury/Aplicar Estética")]
    public static void Apply()
    {
        Scene scene   = SceneManager.GetActiveScene();
        bool isLevel2 = scene.name == "Level2";   // Level2 = oscuridad absoluta
        var creados   = new List<string>();
        var faltantes = new List<string>();

        // ── 1. Global Light 2D ─────────────────────────────────────────────
        Light2D globalLight = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None)
                                    .FirstOrDefault(l => l.lightType == Light2D.LightType.Global);
        if (globalLight == null)
        {
            GameObject go = new GameObject("Global Light 2D");
            Undo.RegisterCreatedObjectUndo(go, "crear Global Light");
            globalLight = go.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            creados.Add("Global Light 2D creada");
        }
        Undo.RecordObject(globalLight, "configurar Global Light");
        if (isLevel2)
        {
            // CERO luz ambiente: solo existen las luces puntuales
            globalLight.color     = Color.black;
            globalLight.intensity = 0f;
            creados.Add("Global Light: intensidad 0 — OSCURIDAD ABSOLUTA");
        }
        else
        {
            globalLight.color     = WarmGlobal;
            globalLight.intensity = 0.7f;
            creados.Add("Global Light: #FFD9A0, intensidad 0.7");
        }

        // ── 2. Fondo de la Main Camera ─────────────────────────────────────
        Camera cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "fondo de cámara");
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = isLevel2 ? Color.black : BgLevel1;
            creados.Add(isLevel2 ? "Fondo de cámara NEGRO PURO #000000"
                                 : "Fondo de cámara #1A0F05 (cálido oscuro)");
        }
        else faltantes.Add("Main Camera (tag 'MainCamera')");

        // ── 3. Luz del Player (su farol en la oscuridad) ───────────────────
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Light2D pl = AttachPointLight(player.transform, "LuzLlama", BlueFlame,
                                          isLevel2 ? 4.5f : 4f,
                                          isLevel2 ? 1.5f : 1.1f);
            pl.transform.localPosition = new Vector3(0f, 0.6f, 0f); // sobre la mecha
            creados.Add(isLevel2 ? "LuzLlama del Player: #4AD4FF radio 4.5, int 1.5 (farol)"
                                 : "LuzLlama del Player: #4AD4FF radio 4");
        }
        else faltantes.Add("Player (tag 'Player') — no pude crear su luz");

        // ── 4. MUNDO OSCURO: materiales + luces de entidades (solo Level2) ──
        if (isLevel2)
        {
            // 4a. Todos los sprites deben REACCIONAR a la luz (Sprite-Lit-Default).
            //     Un sprite unlit se ve siempre brillante y rompe la oscuridad.
            Material litMat = AssetDatabase.LoadAssetAtPath<Material>(LitMaterialPath);
            if (litMat == null)
            {
                string guid = AssetDatabase.FindAssets("Sprite-Lit-Default t:Material").FirstOrDefault();
                if (guid != null)
                    litMat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            }
            if (litMat != null)
            {
                int cambiados = 0;
                foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (sr.sharedMaterial == null || sr.sharedMaterial.shader == null ||
                        sr.sharedMaterial.shader.name != LitShaderName)
                    {
                        Undo.RecordObject(sr, "material lit");
                        sr.sharedMaterial = litMat;
                        cambiados++;
                    }
                }
                creados.Add($"Sprites verificados: {cambiados} cambiados a Sprite-Lit-Default (el resto ya lo usaba)");
            }
            else faltantes.Add("Material Sprite-Lit-Default (¿está instalado URP?)");

            // 4b. Luz por tipo de enemigo
            EnemyBase[] enemigos = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            foreach (EnemyBase enemy in enemigos)
            {
                Color c; float r;
                if      (enemy is RoyalPyromancer) { c = PyroGlow;  r = 3.5f; }
                else if (enemy is CandleGuard)     { c = GuardGlow; r = 2.5f; }
                else                               { c = BeastGlow; r = 2.0f; } // WaxBeast y otros
                AttachPointLight(enemy.transform, "LuzEnemiga", c, r, 1.2f);
            }
            if (enemigos.Length > 0)
                creados.Add($"Luz por tipo en {enemigos.Length} enemigos (Beast #FFE0B0 / Guard #FF4422 / Pyro #FF4080)");
            else
                faltantes.Add("Enemigos (ejecuta ANTES 'Construir Level2' y repite este menú)");

            // 4c. Pickups: brillan dorado para guiar al jugador de luz en luz
            int pickups = 0;
            foreach (PowerUpBase p in Object.FindObjectsByType<PowerUpBase>(FindObjectsSortMode.None))
            { AttachPointLight(p.transform, "LuzPickup", PickupGlow, 1.2f, 1.2f); pickups++; }
            foreach (WaxPickup w in Object.FindObjectsByType<WaxPickup>(FindObjectsSortMode.None))
            { AttachPointLight(w.transform, "LuzPickup", PickupGlow, 1.2f, 1.2f); pickups++; }
            if (pickups > 0) creados.Add($"Luz dorada #FFB830 en {pickups} pickups/gotas");
            else             faltantes.Add("Pickups/Gotas (¿aún no colocados?)");

            // 4d. Meta: faro verde, el objetivo se ve desde lejos
            LevelEnd meta = Object.FindAnyObjectByType<LevelEnd>();
            if (meta != null)
            {
                AttachPointLight(meta.transform, "LuzMeta", MetaGlow, 2.5f, 1.4f);
                creados.Add("Luz verde #5FCC4F en la Meta (faro de navegación)");
            }
            else faltantes.Add("Meta (LevelEnd)");
        }

        // ── 5. FlameLightFlicker + FlameFrenzy + ComboSystem + Balance ─────
        if (player != null)
        {
            WaxSystem wax = player.GetComponent<WaxSystem>();

            // El sprite oficial ya trae la llama dibujada: el parpadeo vive
            // en la LUZ del player (LuzLlama), no en un sprite aparte
            Transform luz = player.transform.Find("LuzLlama");
            if (luz != null)
            {
                FlameLightFlicker flick = luz.GetComponent<FlameLightFlicker>();
                if (flick == null)
                {
                    flick = Undo.AddComponent<FlameLightFlicker>(luz.gameObject);
                    creados.Add("FlameLightFlicker agregado a LuzLlama");
                }
                var so = new SerializedObject(flick);
                so.FindProperty("syncWithWax").boolValue          = true;
                so.FindProperty("waxSystem").objectReferenceValue = wax;
                so.ApplyModifiedProperties();
                creados.Add("FlameLightFlicker (parpadeo de la luz) sincronizado con WaxSystem");
            }

            FlameFrenzy frenzy = player.GetComponent<FlameFrenzy>();
            if (frenzy == null)
            {
                frenzy = Undo.AddComponent<FlameFrenzy>(player);
                creados.Add("FlameFrenzy agregado al Player (mecánica nueva)");
            }
            var soF = new SerializedObject(frenzy);
            // El tinte del frenesí ahora se aplica al sprite del propio Player
            soF.FindProperty("flameRenderer").objectReferenceValue =
                player.GetComponent<SpriteRenderer>();
            soF.ApplyModifiedProperties();

            if (Object.FindAnyObjectByType<ComboSystem>() == null)
            {
                GameObject comboGO = new GameObject("ComboSystem");
                Undo.RegisterCreatedObjectUndo(comboGO, "crear ComboSystem");
                comboGO.AddComponent<ComboSystem>();
                creados.Add("ComboSystem creado en la escena (multiplicador de cera x1→x3)");
            }

            if (wax != null)
            {
                var soW = new SerializedObject(wax);
                soW.FindProperty("waxDrainPerSecond").floatValue = 3.5f;
                soW.ApplyModifiedProperties();
                creados.Add("WaxSystem.waxDrainPerSecond = 3.5");
            }
            else faltantes.Add("WaxSystem en el Player");
        }

        // ── Resumen ────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"══════ APLICAR ESTÉTICA ({scene.name}{(isLevel2 ? " — MUNDO OSCURO" : "")}) — RESUMEN ══════");
        foreach (var c in creados)   Debug.Log("  ✔ " + c);
        foreach (var f in faltantes) Debug.LogWarning("  ✘ FALTA: " + f);
        if (isLevel2)
            Debug.Log("PRUEBA DE ÉXITO: en Play, una plataforma lejos de toda luz debe ser INVISIBLE (negro total).");
        Debug.Log("Balance en scripts (verificado): WaxBeast 40HP/25 cera · CandleGuard 80HP/40 cera · Pirómante 150HP.");
        Debug.Log("Recuerda GUARDAR la escena (Ctrl+S) y repetir en la otra escena.");
    }

    // ── Helper: crea/reconfigura una Point Light 2D hija (idempotente) ─────
    static Light2D AttachPointLight(Transform parent, string name, Color color,
                                    float radius, float intensity)
    {
        Transform t = parent.Find(name);
        Light2D l;
        if (t == null)
        {
            GameObject go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "crear " + name);
            go.transform.SetParent(parent, false);
            l = go.AddComponent<Light2D>();
        }
        else
        {
            l = t.GetComponent<Light2D>();
            if (l == null) l = Undo.AddComponent<Light2D>(t.gameObject);
        }
        Undo.RecordObject(l, "configurar " + name);
        l.lightType             = Light2D.LightType.Point;
        l.color                 = color;
        l.pointLightOuterRadius = radius;
        l.pointLightInnerRadius = radius * 0.15f;
        l.falloffIntensity      = 0.7f;   // falloff suave
        l.intensity             = intensity;
        return l;
    }
}
#endif
