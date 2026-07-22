using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla el HUD: barra de cera, kills, zona actual.
/// Requiere las referencias de UI asignadas en el Inspector.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Barra de Cera")]
    [SerializeField] Slider   waxSlider;
    [SerializeField] Image    waxFill;        // la imagen de relleno de la barra
    [SerializeField] Color    waxHighColor  = new Color(1f, 0.85f, 0.4f);   // dorado
    [SerializeField] Color    waxLowColor   = new Color(0.9f, 0.2f, 0.0f);  // rojo
    [SerializeField] float    lowWaxThresh  = 0.3f;     // % para cambiar color

    [Header("Textos del HUD")]
    [SerializeField] TMP_Text killCountText;
    [SerializeField] TMP_Text zoneText;
    [SerializeField] TMP_Text waxPercentText;

    [Header("Panel GAME OVER (panel vacío por defecto)")]
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] TMP_Text   gameOverKillText;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Suscribirse al WaxSystem del jugador
        WaxSystem ws = FindAnyObjectByType<WaxSystem>();
        if (ws != null)
        {
            ws.OnWaxChanged.AddListener(UpdateWaxBar);
            ws.OnDeath.AddListener(ShowGameOver);
        }
    }

    // ── Barra de Cera ─────────────────────────────────────────────────────────
    public void UpdateWaxBar(float current, float max)
    {
        if (waxSlider == null) return;
        float pct      = current / max;
        waxSlider.value = pct;

        if (waxFill != null)
            waxFill.color = Color.Lerp(waxLowColor, waxHighColor, pct / lowWaxThresh);

        if (waxPercentText != null)
            waxPercentText.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max);
    }

    // ── Kill counter ──────────────────────────────────────────────────────────
    public void UpdateKillCount(int count)
    {
        if (killCountText != null)
            killCountText.text = "Kills: " + count;
    }

    // ── Zona ─────────────────────────────────────────────────────────────────
    public void SetZoneText(string zoneName)
    {
        if (zoneText != null)
            zoneText.text = zoneName;
    }

    // ── GAME OVER UI ──────────────────────────────────────────────────────────
    void ShowGameOver()
    {
        if (gameOverPanel == null) return;
        gameOverPanel.SetActive(true);
        if (gameOverKillText != null)
            gameOverKillText.text = "Enemigos eliminados: " + GameManager.Instance?.KillCount;
    }

    // ── VICTORIA (reutiliza el panel de Game Over) ────────────────────────────
    public void ShowWin()
    {
        if (gameOverPanel == null) return;
        gameOverPanel.SetActive(true);

        // Cambiar el título del panel (el primer texto que no sea el contador)
        foreach (var t in gameOverPanel.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t == gameOverKillText) continue;
            t.text  = "¡VICTORIA!";
            t.color = new Color(1f, 0.85f, 0.4f);   // dorado
            break;
        }

        if (gameOverKillText != null)
            gameOverKillText.text = "Completaste Candle Fury — Enemigos eliminados: "
                                    + GameManager.Instance?.KillCount;
    }

    // ── Botones del panel Game Over ───────────────────────────────────────────
    public void OnRetryButton()  => GameManager.Instance?.RestartGame();
    public void OnMenuButton()   => GameManager.Instance?.LoadMainMenu();
}
