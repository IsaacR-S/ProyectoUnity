#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// CANDLE FURY — Expansor de Nivel (Entrega 3).
///
/// CÓMO USAR:
/// 1. Coloca este archivo en Assets/Editor/  (crea la carpeta "Editor" si no existe)
/// 2. Abre la escena SampleScene
/// 3. Menú superior: Candle Fury → Expandir Nivel
/// 4. Revisa la Console: te dice qué creó y qué falta
///
/// Construye el nivel de ~80 unidades en 3 zonas (Tutorial / Combate / Desafío)
/// clonando tus objetos existentes para conservar sprites y configuración.
/// Todo es des-hacible con Ctrl+Z.
/// </summary>
public static class LevelExpander
{
    [MenuItem("Candle Fury/Expandir Nivel")]
    public static void Expand()
    {
        // ── 1. Buscar plantillas en la escena ──────────────────────────────
        GameObject sueloTemplate  = Find("Suelo");
        GameObject gotaTemplate   = Find("GotaDeCera");
        GameObject beastTemplate  = Find("WaxBeast");
        GameObject guardTemplate  = Find("CandleGuard");
        GameObject meta           = Find("Meta") ?? Find("LevelEnd");

        if (sueloTemplate == null)
        {
            Debug.LogError("[LevelExpander] No encontré un objeto llamado 'Suelo'. Renombra tu suelo principal a 'Suelo' y vuelve a ejecutar.");
            return;
        }

        var creados = new List<string>();
        var faltantes = new List<string>();

        // ── 2. Limpiar suelos duplicados viejos (Suelo (1), Suelo (2)...) ──
        foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                 .Where(g => g.name.StartsWith("Suelo (") ).ToList())
        {
            Undo.DestroyObjectImmediate(go);
        }

        // ── 3. Suelo continuo con FOSO entre x=26 y x=31 ───────────────────
        float groundY = sueloTemplate.transform.position.y;
        // Segmento A: x -5 → 26   (centro 10.5, ancho 31)
        ResizeGround(sueloTemplate, new Vector3(10.5f, groundY, 0), 31f, "Suelo_A");
        // Segmento B: x 31 → 80   (centro 55.5, ancho 49)
        GameObject segB = CloneAt(sueloTemplate, new Vector3(55.5f, groundY, 0), "Suelo_B");
        SetWidth(segB, 49f);
        creados.Add("Suelo extendido a x=80 con foso en x:26-31");

        // ── 4. Plataformas (clones del suelo, más pequeñas) ────────────────
        var plataformas = new (Vector3 pos, float w, string name)[]
        {
            (new Vector3( 6f,  groundY + 1.5f, 0), 3.0f, "Plat_A1_saltoBasico"),
            (new Vector3(11f,  groundY + 3.2f, 0), 3.0f, "Plat_A2_dobleSalto"),
            (new Vector3(27.5f,groundY + 1.8f, 0), 2.0f, "Plat_B1_foso"),
            (new Vector3(30f,  groundY + 2.6f, 0), 2.0f, "Plat_B2_foso"),
            (new Vector3(43f,  groundY + 3.8f, 0), 3.0f, "Plat_B3_guardia"),
            (new Vector3(61f,  groundY + 2.2f, 0), 2.5f, "Plat_C1"),
            (new Vector3(66f,  groundY + 3.5f, 0), 2.5f, "Plat_C2"),
        };
        foreach (var p in plataformas)
        {
            GameObject plat = CloneAt(sueloTemplate, p.pos, p.name);
            SetWidth(plat, p.w);
            plat.transform.localScale = new Vector3(plat.transform.localScale.x, 0.4f, 1f);
        }
        creados.Add("7 plataformas en las 3 zonas");

        // ── 5. Gotas de cera ───────────────────────────────────────────────
        if (gotaTemplate != null)
        {
            var gotas = new Vector3[]
            {
                new Vector3( 6f,  groundY + 2.4f, 0),
                new Vector3(20f,  groundY + 0.9f, 0),
                new Vector3(28.8f,groundY + 3.4f, 0),
                new Vector3(50f,  groundY + 0.9f, 0),  // detrás del muro secreto
                new Vector3(66f,  groundY + 4.3f, 0),
            };
            for (int i = 0; i < gotas.Length; i++)
                CloneAt(gotaTemplate, gotas[i], $"GotaDeCera_{i + 1}");
            creados.Add("5 gotas de cera");
        }
        else faltantes.Add("GotaDeCera (no encontrada — colócalas a mano)");

        // ── 6. Enemigos ────────────────────────────────────────────────────
        if (beastTemplate != null)
        {
            CloneAt(beastTemplate, new Vector3(16f, groundY + 1.2f, 0), "WaxBeast_A1");
            CloneAt(beastTemplate, new Vector3(35f, groundY + 1.2f, 0), "WaxBeast_B1");
            CloneAt(beastTemplate, new Vector3(38f, groundY + 1.2f, 0), "WaxBeast_B2_emboscada");
            creados.Add("3 Bestias de Cera (1 tutorial + emboscada de 2)");
        }
        else faltantes.Add("WaxBeast");

        if (guardTemplate != null)
        {
            // Guardia sobre la plataforma alta B3
            guardTemplate.transform.position = new Vector3(43f, groundY + 4.6f, 0);
            Undo.RecordObject(guardTemplate.transform, "mover guardia");
            creados.Add("CandleGuard reposicionado sobre Plat_B3");
        }
        else faltantes.Add("CandleGuard");

        // ── 7. Peligros (si los scripts existen) ───────────────────────────
        // Foso de púas en el hueco del suelo
        if (TypeExists("SpikePit"))
        {
            GameObject pit = new GameObject("SpikePit_foso");
            Undo.RegisterCreatedObjectUndo(pit, "crear SpikePit");
            pit.transform.position = new Vector3(28.5f, groundY - 2f, 0);
            var bc = pit.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(5f, 1.5f);
            pit.AddComponent(Type.GetType("SpikePit, Assembly-CSharp"));
            creados.Add("SpikePit en el foso (x:26-31)");
        }
        else faltantes.Add("SpikePit (script no existe)");

        // Muro de cera con secreto
        if (TypeExists("WaxWall"))
        {
            GameObject wall = CloneAt(sueloTemplate, new Vector3(48f, groundY + 1.7f, 0), "WaxWall_secreto");
            SetWidth(wall, 0.8f);
            wall.transform.localScale = new Vector3(wall.transform.localScale.x, 2.6f, 1f);
            // quitar componentes de suelo que no aplican y agregar WaxWall
            var sr = wall.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(0.91f, 0.83f, 0.69f); // crema E8D4B0
            wall.AddComponent(Type.GetType("WaxWall, Assembly-CSharp"));
            wall.tag = "Untagged";
            creados.Add("Muro de Cera secreto en x=48 (gota escondida detrás)");
        }
        else faltantes.Add("WaxWall (script no existe)");

        // ── 8. Meta al final ───────────────────────────────────────────────
        if (meta != null)
        {
            Undo.RecordObject(meta.transform, "mover meta");
            meta.transform.position = new Vector3(79f, groundY + 1.5f, 0);
            creados.Add("Meta movida a x=79");
        }
        else faltantes.Add("Meta / LevelEnd");

        // ── 9. Resumen ─────────────────────────────────────────────────────
        Debug.Log("══════ LEVEL EXPANDER — RESUMEN ══════");
        foreach (var c in creados)   Debug.Log("  ✔ " + c);
        foreach (var f in faltantes) Debug.LogWarning("  ✘ FALTA: " + f);
        Debug.Log("Pendientes manuales: FireTrap en x=58, DripEmitter en (63,4), Pirómante en x=73.");
        Debug.Log("Recuerda guardar la escena (Ctrl+S). Todo es des-hacible con Ctrl+Z.");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────
    static GameObject Find(string name) =>
        UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .FirstOrDefault(g => g.name == name);

    static GameObject CloneAt(GameObject template, Vector3 pos, string name)
    {
        GameObject c = UnityEngine.Object.Instantiate(template, pos, Quaternion.identity);
        c.name = name;
        Undo.RegisterCreatedObjectUndo(c, "crear " + name);
        return c;
    }

    static void ResizeGround(GameObject suelo, Vector3 pos, float width, string name)
    {
        Undo.RecordObject(suelo.transform, "redimensionar suelo");
        suelo.transform.position = pos;
        SetWidth(suelo, width);
        suelo.name = name;
    }

    static void SetWidth(GameObject go, float width)
    {
        Vector3 s = go.transform.localScale;
        go.transform.localScale = new Vector3(width, s.y, s.z);
    }

    static bool TypeExists(string typeName) =>
        Type.GetType(typeName + ", Assembly-CSharp") != null;
}
#endif
