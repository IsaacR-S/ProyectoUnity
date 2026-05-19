using UnityEngine;

/// <summary>
/// Trigger al final del nivel.
/// Al entrar el jugador, llama a GameManager.WinGame().
/// </summary>
public class LevelEnd : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
            GameManager.Instance?.WinGame();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider2D>()?.size ?? Vector3.one);
    }
}
