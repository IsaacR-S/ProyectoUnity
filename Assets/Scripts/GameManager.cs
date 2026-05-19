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
        StartCoroutine(LoadSceneDelayed(winScene, 1.5f));
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
        gameEnded  = false;
        killCount  = 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        gameEnded = false;
        killCount = 0;
        SceneManager.LoadScene(mainMenuScene);
    }

    // ── Auxiliar ──────────────────────────────────────────────────────────────
    IEnumerator LoadSceneDelayed(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
}
