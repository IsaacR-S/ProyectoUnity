#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// CANDLE FURY — Constructor del Nivel 2 (Entrega 3).
///
/// CÓMO USAR:
/// 1. Abre la escena Level2 (Assets/Scenes/Level2.unity)
/// 2. Menú superior: Candle Fury → Construir Level2
/// 3. Revisa la Console: resumen de lo creado / faltante
/// 4. Guarda la escena (Ctrl+S)
///
/// Reorganiza el nivel para diferenciarlo del 1:
///   • 3 torres de plataformas (más verticalidad)
///   • 2 CandleGuards en posiciones altas
///   • 1 RoyalPyromancer justo antes de la meta
///   • 2 trampas de fuego en el suelo
///   • 1 muro de cera con una gota secreta detrás
///   • Meta con isFinalLevel = true
/// Todo se clona de objetos existentes de la escena (conserva sprites)
/// y es des-hacible con Ctrl+Z. Es seguro re-ejecutarlo: borra primero
/// lo que él mismo creó (prefijo "L2_").
/// </summary>
public static class Level2Builder
{
    [MenuItem("Candle Fury/Construir Level2")]
    public static void Build()
    {
        // ── 0. Verificar que estamos en Level2 ─────────────────────────────
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "Level2")
        {
            Debug.LogError($"[Level2Builder] La escena activa es '{scene.name}'. Abre Level2 y vuelve a ejecutar.");
            return;
        }

        var creados   = new List<string>();
        var faltantes = new List<string>();

        // ── 1. Buscar plantillas por nombre ────────────────────────────────
        GameObject suelo     = Find("Suelo");
        GameObject plataforma = Find("Plataforma") ?? suelo;
        GameObject gota      = Find("GotaDeCera");
        GameObject guardia   = Find("CandleGuard");
        GameObject piromante = Find("RoyalPyromancer");
        GameObject fireTrap  = Find("FireTrap (base)") ?? Find("FireTrap");
        GameObject waxWall   = Find("WaxWall");
        GameObject meta      = Find("Meta") ?? Find("LevelEnd");

        if (plataforma == null)
        {
            Debug.LogError("[Level2Builder] No encontré 'Plataforma' ni 'Suelo' para clonar. Aborto.");
            return;
        }
        float groundY = (suelo != null ? suelo.transform.position.y : plataforma.transform.position.y);

        // ── 2. Limpiar lo generado en ejecuciones anteriores (prefijo L2_) ──
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                 .Where(g => g.name.StartsWith("L2_")).ToList())
        {
            Undo.DestroyObjectImmediate(go);
        }

        // ── 3. VERTICALIDAD: tres torres de plataformas ────────────────────
        var plataformas = new (Vector3 pos, float w, string name)[]
        {
            // Torre A — subida tutorial de la verticalidad
            (new Vector3(12f, groundY + 2.0f, 0), 2.5f, "L2_TorreA_1"),
            (new Vector3(15f, groundY + 3.5f, 0), 2.5f, "L2_TorreA_2"),
            (new Vector3(18f, groundY + 5.0f, 0), 2.5f, "L2_TorreA_3"),
            // Torre B — más alta, custodiada por el primer guardia
            (new Vector3(33f, groundY + 2.2f, 0), 2.5f, "L2_TorreB_1"),
            (new Vector3(36f, groundY + 3.8f, 0), 2.5f, "L2_TorreB_2"),
            (new Vector3(39f, groundY + 5.4f, 0), 3.0f, "L2_TorreB_3"),
            // Camino alto hacia el segundo guardia
            (new Vector3(44f, groundY + 5.2f, 0), 2.5f, "L2_Alto_1"),
            (new Vector3(49f, groundY + 4.8f, 0), 3.0f, "L2_Alto_2"),
            // Bajada hacia la zona del jefe
            (new Vector3(56f, groundY + 3.0f, 0), 2.5f, "L2_Bajada_1"),
        };
        foreach (var p in plataformas)
        {
            GameObject plat = CloneAt(plataforma, p.pos, p.name);
            SetWidth(plat, p.w);
        }
        creados.Add("9 plataformas en 3 torres (verticalidad)");

        // ── 4. Gotas de cera premiando la escalada ─────────────────────────
        if (gota != null)
        {
            CloneAt(gota, new Vector3(18f, groundY + 5.9f, 0), "L2_Gota_TorreA");
            CloneAt(gota, new Vector3(44f, groundY + 6.1f, 0), "L2_Gota_Alto");
            creados.Add("2 gotas de cera en las alturas");
        }
        else faltantes.Add("GotaDeCera (colócalas a mano)");

        // ── 5. Dos CandleGuards en posiciones altas ────────────────────────
        if (guardia != null)
        {
            Undo.RecordObject(guardia.transform, "mover guardia 1");
            guardia.transform.position = new Vector3(39f, groundY + 6.2f, 0);

            CloneAt(guardia, new Vector3(49f, groundY + 5.6f, 0), "L2_CandleGuard_2");
            creados.Add("2 CandleGuards (Torre B y camino alto)");
        }
        else faltantes.Add("CandleGuard");

        // ── 6. Dos trampas de fuego en el suelo ────────────────────────────
        if (fireTrap != null)
        {
            float trapY = fireTrap.transform.position.y;
            CloneAt(fireTrap, new Vector3(25f, trapY, 0), "L2_FireTrap_1");
            CloneAt(fireTrap, new Vector3(53f, trapY, 0), "L2_FireTrap_2");
            creados.Add("2 FireTraps (x=25 y x=53)");
        }
        else faltantes.Add("FireTrap (base)");

        // ── 7. Muro de cera con secreto ────────────────────────────────────
        if (waxWall != null)
        {
            GameObject wall = CloneAt(waxWall, new Vector3(62f, groundY + 1.7f, 0), "L2_WaxWall_secreto");
            if (gota != null)
                CloneAt(gota, new Vector3(64f, groundY + 1.0f, 0), "L2_Gota_secreta");
            creados.Add("Muro de cera secreto en x=62 (indestructible — la gota de atrás se alcanza saltando por encima)");
        }
        else faltantes.Add("WaxWall");

        // ── 8. Pirómante Real antes de la meta ─────────────────────────────
        if (piromante != null)
        {
            Undo.RecordObject(piromante.transform, "mover pirómante");
            piromante.transform.position = new Vector3(72f, groundY + 1.5f, 0);
            creados.Add("RoyalPyromancer en x=72 (antes de la meta)");
        }
        else faltantes.Add("RoyalPyromancer");

        // ── 9. Meta final: posición + isFinalLevel = true ──────────────────
        if (meta != null)
        {
            Undo.RecordObject(meta.transform, "mover meta");
            meta.transform.position = new Vector3(79f, groundY + 1.5f, 0);

            LevelEnd le = meta.GetComponent<LevelEnd>();
            if (le != null)
            {
                var so = new SerializedObject(le);
                so.FindProperty("isFinalLevel").boolValue    = true;
                so.FindProperty("nextSceneName").stringValue = "";
                so.ApplyModifiedProperties();
                creados.Add("Meta en x=79 con isFinalLevel = TRUE");
            }
            else faltantes.Add("LevelEnd (la Meta no tiene el componente)");
        }
        else faltantes.Add("Meta / LevelEnd");

        // ── 10. Resumen ────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("══════ LEVEL 2 BUILDER — RESUMEN ══════");
        foreach (var c in creados)   Debug.Log("  ✔ " + c);
        foreach (var f in faltantes) Debug.LogWarning("  ✘ FALTA: " + f);
        Debug.Log("Recuerda GUARDAR la escena (Ctrl+S). Todo es des-hacible con Ctrl+Z.");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────
    static GameObject Find(string name) =>
        Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
              .FirstOrDefault(g => g.name == name);

    static GameObject CloneAt(GameObject template, Vector3 pos, string name)
    {
        GameObject c = Object.Instantiate(template, pos, Quaternion.identity);
        c.name = name;
        Undo.RegisterCreatedObjectUndo(c, "crear " + name);
        return c;
    }

    static void SetWidth(GameObject go, float width)
    {
        Vector3 s = go.transform.localScale;
        go.transform.localScale = new Vector3(width, s.y, s.z);
    }
}
#endif
