using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Trigger al final del nivel.
/// Al entrar el jugador, carga la siguiente escena (siguiente nivel)
/// o, si es el último nivel, dispara la victoria del juego.
/// </summary>
public class LevelEnd : MonoBehaviour
{
    [Header("Siguiente nivel")]
    [Tooltip("Nombre EXACTO de la escena a cargar (debe estar en Build Settings). " +
             "Déjalo vacío si este es el último nivel.")]
    [SerializeField] string nextSceneName = "";

    [Tooltip("Si está activo y no hay siguiente nivel, llama a GameManager.WinGame()")]
    [SerializeField] bool isFinalLevel = false;

    bool used = false;   // evita que se dispare dos veces

    void OnTriggerEnter2D(Collider2D col)
    {
        if (used) return;
        if (!col.CompareTag("Player")) return;
        used = true;

        if (!isFinalLevel && !string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log($"[LevelEnd] Cargando siguiente nivel: {nextSceneName}");
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.Log("[LevelEnd] ¡Último nivel completado! Victoria.");
            GameManager.Instance?.WinGame();
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        var box = GetComponent<BoxCollider2D>();
        Gizmos.DrawWireCube(transform.position, box != null ? (Vector3)box.size : Vector3.one);
    }
}