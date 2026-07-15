#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Text;

/// <summary>
/// CANDLE FURY — Diagnóstico del fondo (Entrega 3).
/// Menú: Candle Fury → Diagnosticar Fondo.
/// Funciona en modo edición Y en Play (ejecútalo en Play si el bug
/// solo aparece jugando). Imprime posición y bounds del Background,
/// posición de cámara y player, el rectángulo visible de la cámara,
/// y si el fondo cubre o no cada borde (con la distancia exacta).
/// </summary>
public static class BackgroundDiagnostic
{
    [MenuItem("Candle Fury/Diagnosticar Fondo")]
    public static void Diagnose()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"══════ DIAGNÓSTICO DE FONDO ({(Application.isPlaying ? "PLAY" : "EDITOR")}) ══════");

        // ── Cámara ──────────────────────────────────────────────────────────
        Camera cam = Camera.main;
        Rect camRect = new Rect();
        if (cam != null)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector3 cp = cam.transform.position;
            camRect = new Rect(cp.x - halfW, cp.y - halfH, halfW * 2f, halfH * 2f);
            sb.AppendLine($"Cámara: pos ({cp.x:0.##}, {cp.y:0.##}) · orto {halfH} · aspect {cam.aspect:0.###}");
            sb.AppendLine($"  Rect visible: x[{camRect.xMin:0.##} .. {camRect.xMax:0.##}]  y[{camRect.yMin:0.##} .. {camRect.yMax:0.##}]  ({camRect.width:0.##} x {camRect.height:0.##} u)");
        }
        else sb.AppendLine("Cámara: ✘ NO HAY Camera.main");

        // ── Player ──────────────────────────────────────────────────────────
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
            sb.AppendLine($"Player: ({player.transform.position.x:0.##}, {player.transform.position.y:0.##})");
        else sb.AppendLine("Player: ✘ no encontrado");

        // ── Background ──────────────────────────────────────────────────────
        GameObject bg = GameObject.Find("Background");
        if (bg == null)
        {
            sb.AppendLine("Background: ✘ NO EXISTE ningún objeto 'Background' en la escena.");
            Debug.Log(sb.ToString());
            return;
        }

        var renderers = bg.GetComponentsInChildren<SpriteRenderer>(true);
        sb.AppendLine($"Background: pos ({bg.transform.position.x:0.##}, {bg.transform.position.y:0.##}) · {renderers.Length} SpriteRenderers hijos");

        if (renderers.Length > 0)
        {
            Bounds total = renderers[0].bounds;
            int visibles = 0, sinSprite = 0;
            foreach (var r in renderers)
            {
                total.Encapsulate(r.bounds);
                if (r.enabled && r.gameObject.activeInHierarchy) visibles++;
                if (r.sprite == null) sinSprite++;
            }
            sb.AppendLine($"  Bounds TOTAL: x[{total.min.x:0.##} .. {total.max.x:0.##}]  y[{total.min.y:0.##} .. {total.max.y:0.##}]  ({total.size.x:0.##} x {total.size.y:0.##} u)");
            sb.AppendLine($"  Tile ejemplo: {renderers[0].bounds.size.x:0.##} x {renderers[0].bounds.size.y:0.##} u · sprite '{(renderers[0].sprite != null ? renderers[0].sprite.name : "NULL")}' · order {renderers[0].sortingOrder}");
            sb.AppendLine($"  Renderers activos: {visibles}/{renderers.Length}" + (sinSprite > 0 ? $" · ⚠ {sinSprite} SIN sprite" : ""));

            var par = bg.GetComponent<ParallaxLayer>();
            sb.AppendLine($"  ParallaxLayer: {(par != null ? (par.enabled ? "presente y activo" : "presente pero DESACTIVADO") : "✘ AUSENTE")}");

            // ── Veredicto de cobertura ──────────────────────────────────────
            if (cam != null)
            {
                sb.AppendLine("Cobertura del rect visible de la cámara:");
                Check(sb, "IZQUIERDA", camRect.xMin - total.min.x);
                Check(sb, "DERECHA  ", total.max.x - camRect.xMax);
                Check(sb, "ABAJO    ", camRect.yMin - total.min.y);
                Check(sb, "ARRIBA   ", total.max.y - camRect.yMax);
            }
        }
        else sb.AppendLine("  ✘ El Background no tiene ningún SpriteRenderer hijo.");

        Debug.Log(sb.ToString());
    }

    static void Check(StringBuilder sb, string lado, float margen)
    {
        sb.AppendLine(margen >= 0f
            ? $"  ✔ {lado}: cubierto con {margen:0.##} u de margen"
            : $"  ✘ {lado}: DESCUBIERTO — faltan {-margen:0.##} u de fondo por ese lado");
    }
}
#endif
