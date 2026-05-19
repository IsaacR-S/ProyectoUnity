using UnityEngine;

/// <summary>
/// Proyectil de fuego lanzado por el CandleGuard.
/// Al tocar al jugador le quita cera.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyFlameProjectile : MonoBehaviour
{
    int   damage;
    float lifetime = 4f;

    void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f) Destroy(gameObject);
    }

    public void Initialize(int dmg, float direction, float speed)
    {
        damage = dmg;
        GetComponent<Rigidbody2D>().linearVelocity = new Vector2(direction * speed, 0f);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            col.GetComponent<WaxSystem>()?.RemoveWax(damage);
            Destroy(gameObject);
        }
        else if (col.CompareTag("Ground") || col.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
