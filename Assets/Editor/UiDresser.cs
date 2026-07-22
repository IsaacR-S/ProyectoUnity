#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CANDLE FURY — Viste la UI: barra de cera pixelart arriba a la izquierda
/// (frame 9-slice + fill teñible + icono de llama, sin perilla), panel de
/// game over oculto por defecto con fondo oscuro y botón centrado, y cablea
/// las referencias del UIManager que estaban en null (waxFill, gameOverPanel,
/// gameOverKillText). CanvasScaler pasa a Scale With Screen Size. Idempotente.
/// Entrada batch: -executeMethod UiDresser.DressAllScenes
/// </summary>
public static class UiDresser
{
    const string Dir = "Assets/Sprites/UI/";
    // PPU 50 → los bordes de 3/4 px se ven ~6/8 px en el canvas (ref PPU 100)
    const float PpuTarget = 50f;

    static readonly string[] ScenePaths =
        { "Assets/Scenes/Level1.unity", "Assets/Scenes/Level2.unity" };

    [MenuItem("Candle Fury/Vestir UI")]
    public static void DressActiveScene()
    {
        Dress();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Recuerda GUARDAR la escena (Ctrl+S) y repetir en la otra escena.");
    }

    public static void DressAllScenes()
    {
        foreach (string path in ScenePaths)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Dress();
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"✔ UI vestida en {path}");
        }
    }

    static void Dress()
    {
        Sprite frame = PixelSpriteImport.Import(Dir + "ui_bar_frame.png", 10f / PpuTarget, null, new Vector4(3, 3, 3, 3));
        Sprite fill  = PixelSpriteImport.Import(Dir + "ui_bar_fill.png",   4f / PpuTarget);
        Sprite icon  = PixelSpriteImport.Import(Dir + "ui_icon_flame.png", 12f / PpuTarget);
        Sprite btn   = PixelSpriteImport.Import(Dir + "ui_button.png",    16f / PpuTarget, null, new Vector4(4, 4, 4, 4));
        if (frame == null || fill == null || icon == null || btn == null)
        {
            Debug.LogError("[UiDresser] Faltan sprites en Assets/Sprites/UI/");
            return;
        }

        foreach (var ui in Object.FindObjectsByType<UIManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var soUi = new SerializedObject(ui);

            // ── CanvasScaler: escalar con la resolución ────────────────────
            var scaler = ui.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                Undo.RecordObject(scaler, "vestir UI");
                scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(800, 600);
                scaler.matchWidthOrHeight  = 0.5f;
            }

            // ── Barra de cera ──────────────────────────────────────────────
            var slider = soUi.FindProperty("waxSlider").objectReferenceValue as Slider;
            if (slider != null)
            {
                var rt = slider.GetComponent<RectTransform>();
                Undo.RecordObject(rt, "vestir UI");
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(20f, -16f);
                rt.sizeDelta        = new Vector2(200f, 16f);

                Undo.RecordObject(slider, "vestir UI");
                slider.interactable  = false;
                slider.transition    = Selectable.Transition.None;
                slider.targetGraphic = null;
                slider.handleRect    = null;

                // Fondo (frame 9-slice) como primer hijo
                Transform bg = slider.transform.Find("Background");
                if (bg == null)
                {
                    var go = new GameObject("Background", typeof(RectTransform));
                    Undo.RegisterCreatedObjectUndo(go, "vestir UI");
                    go.transform.SetParent(slider.transform, false);
                    go.transform.SetAsFirstSibling();
                    bg = go.transform;
                }
                var bgRt = (RectTransform)bg;
                bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
                var bgImg = bg.GetComponent<Image>();
                if (bgImg == null) bgImg = Undo.AddComponent<Image>(bg.gameObject);
                bgImg.sprite = frame;
                bgImg.type   = Image.Type.Sliced;
                bgImg.color  = Color.white;

                // Fill con sprite teñible
                var fillRect = slider.fillRect;
                if (fillRect != null)
                {
                    var fillImg = fillRect.GetComponent<Image>();
                    if (fillImg != null)
                    {
                        Undo.RecordObject(fillImg, "vestir UI");
                        fillImg.sprite = fill;
                        fillImg.type   = Image.Type.Simple;
                        soUi.FindProperty("waxFill").objectReferenceValue = fillImg;
                    }
                    var fa = (RectTransform)fillRect.parent;   // Fill Area
                    Undo.RecordObject(fa, "vestir UI");
                    fa.anchorMin = Vector2.zero; fa.anchorMax = Vector2.one;
                    fa.offsetMin = new Vector2(3f, 3f); fa.offsetMax = new Vector2(-3f, -3f);
                }

                // Sin perilla
                Transform handleArea = slider.transform.Find("Handle Slide Area");
                if (handleArea != null && handleArea.gameObject.activeSelf)
                    handleArea.gameObject.SetActive(false);

                // Icono de llama a la izquierda
                Transform ic = slider.transform.Find("FlameIcon");
                if (ic == null)
                {
                    var go = new GameObject("FlameIcon", typeof(RectTransform));
                    Undo.RegisterCreatedObjectUndo(go, "vestir UI");
                    go.transform.SetParent(slider.transform, false);
                    ic = go.transform;
                }
                var icRt = (RectTransform)ic;
                icRt.anchorMin = icRt.anchorMax = new Vector2(0f, 0.5f);
                icRt.pivot = new Vector2(1f, 0.5f);
                icRt.anchoredPosition = new Vector2(-3f, 0f);
                icRt.sizeDelta = new Vector2(16f, 16f);
                var icImg = ic.GetComponent<Image>();
                if (icImg == null) icImg = Undo.AddComponent<Image>(ic.gameObject);
                icImg.sprite = icon;
            }

            // ── Panel de game over ────────────────────────────────────────
            Transform panel = null;
            foreach (var t in ui.GetComponentsInChildren<Transform>(true))
                if (t.name == "PanelGameOver") { panel = t; break; }
            if (panel != null)
            {
                var panelGo = panel.gameObject;
                var prt = panelGo.GetComponent<RectTransform>();
                if (prt == null) prt = Undo.AddComponent<RectTransform>(panelGo);   // convierte el Transform
                panel = panelGo.transform;   // AddComponent<RectTransform> reemplaza el Transform: re-obtener
                prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
                prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;

                var pImg = panelGo.GetComponent<Image>();
                if (pImg == null) pImg = Undo.AddComponent<Image>(panelGo);
                pImg.sprite = null;
                pImg.color  = new Color(0f, 0f, 0f, 0.78f);

                // Botón centrado con sprite pixelart
                Transform button = panel.Find("Button");
                TMP_FontAsset font = null;
                if (button != null)
                {
                    var brt = button.GetComponent<RectTransform>();
                    Undo.RecordObject(brt, "vestir UI");
                    brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f, 0.5f);
                    brt.anchoredPosition = new Vector2(0f, -30f);
                    brt.sizeDelta        = new Vector2(220f, 44f);
                    var bImg = button.GetComponent<Image>();
                    if (bImg != null)
                    {
                        Undo.RecordObject(bImg, "vestir UI");
                        bImg.sprite = btn;
                        bImg.type   = Image.Type.Sliced;
                        bImg.color  = Color.white;
                    }
                    var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (label != null) font = label.font;
                }

                // Texto de kills sobre el botón
                Transform kills = panel.Find("GameOverKills");
                if (kills == null)
                {
                    var go = new GameObject("GameOverKills", typeof(RectTransform));
                    Undo.RegisterCreatedObjectUndo(go, "vestir UI");
                    go.transform.SetParent(panel, false);
                    kills = go.transform;
                }
                var krt = (RectTransform)kills;
                krt.anchorMin = krt.anchorMax = krt.pivot = new Vector2(0.5f, 0.5f);
                krt.anchoredPosition = new Vector2(0f, 40f);
                krt.sizeDelta        = new Vector2(420f, 40f);
                var ktxt = kills.GetComponent<TextMeshProUGUI>();
                if (ktxt == null) ktxt = Undo.AddComponent<TextMeshProUGUI>(kills.gameObject);
                if (font != null) ktxt.font = font;
                ktxt.fontSize  = 26f;
                ktxt.alignment = TextAlignmentOptions.Center;
                if (string.IsNullOrEmpty(ktxt.text)) ktxt.text = "Enemigos derrotados: 0";

                // Oculto por defecto; ShowGameOver() lo activa al morir
                if (panelGo.activeSelf) panelGo.SetActive(false);

                soUi.FindProperty("gameOverPanel").objectReferenceValue    = panelGo;
                soUi.FindProperty("gameOverKillText").objectReferenceValue = ktxt;
            }
            else
            {
                Debug.LogWarning("[UiDresser] No se encontró 'PanelGameOver' bajo el Canvas");
            }

            soUi.ApplyModifiedProperties();
            Debug.Log($"✔ UI de {ui.gameObject.name} vestida");
        }
    }
}
#endif
