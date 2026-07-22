using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla el estado general del juego: inicio, GAME OVER, victoria.
/// Una sola vida — sin checkpoint.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ── Configuración ─────────────────────────────────────────────────────────
    [Header("Escenas")]
    [SerializeField] string gameOverScene = "GameOver";
    [SerializeField] string winScene      = "Win";
    [SerializeField] string mainMenuScene = "MainMenu";

    [Header("Delay antes de GAME OVER")]
    [SerializeField] float gameOverDelay = 2f;

    // ── Estado ────────────────────────────────────────────────────────────────
    bool gameEnded;

    // ── Kill counter (para HUD) ───────────────────────────────────────────────
    int killCount;
    public int KillCount => killCount;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── GAME OVER ─────────────────────────────────────────────────────────────
    /// <summary>Llamado desde WaxSystem.OnDeath</summary>
    public void GameOver()
    {
        if (gameEnded) return;
        gameEnded = true;
        Debug.Log("[GameManager] GAME OVER");
        StartCoroutine(LoadSceneDelayed(gameOverScene, gameOverDelay));
    }

    // ── VICTORIA ──────────────────────────────────────────────────────────────
    public void WinGame()
    {
        if (gameEnded) return;
        gameEnded = true;
        Debug.Log("[GameManager] ¡VICTORIA!");
        StartCoroutine(WinRoutine());
    }

    IEnumerator WinRoutine()
    {
        yield return new WaitForSeconds(1f);

        // Si algún día existe una escena "Win", se usa; si no, el juego
        // TERMINA aquí: panel de victoria y todo congelado.
        if (Application.CanStreamedLevelBeLoaded(winScene))
        {
            SceneManager.LoadScene(winScene);
        }
        else
        {
            UIManager.Instance?.ShowWin();
            Time.timeScale = 0f;   // fin del juego (los botones de UI siguen funcionando)
        }
    }

    // ── Registrar kill ────────────────────────────────────────────────────────
    public void RegisterKill()
    {
        killCount++;
        UIManager.Instance?.UpdateKillCount(killCount);
    }

    // ── Reiniciar ─────────────────────────────────────────────────────────────
    public void RestartGame()
    {
        Time.timeScale = 1f;   // por si venimos de la pantalla de victoria
        gameEnded  = false;
        killCount  = 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        gameEnded = false;
        killCount = 0;
        SceneManager.LoadScene(mainMenuScene);
    }

    // ── Auxiliar ──────────────────────────────────────────────────────────────
    IEnumerator LoadSceneDelayed(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Si la escena no está en Build Settings (GameOver/Win aún no existen),
        // no intentamos cargarla: el panel del UIManager ya muestra el resultado.
        if (Application.CanStreamedLevelBeLoaded(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            Debug.LogWarning($"[GameManager] Escena '{sceneName}' no está en Build Settings — se queda el panel de UI.");
    }
}
