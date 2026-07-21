using UnityEngine;

/// <summary>
/// Proyectil de llama del jugador.
/// Se destruye al tocar un enemigo o un muro.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class FlameProjectile : MonoBehaviour
{
    int       damage;
    LayerMask enemyLayer;
    float     lifetime = 3f;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Luz azul parpadeante para verse volar en la oscuridad de Level2
        ProjectileGlow.Attach(this, new Color(0.290f, 0.831f, 1f),
                              radius: 1.6f, intensity: 2f, flicker: true);
    }

    void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f) Destroy(gameObject);
    }

    public void Initialize(int dmg, float direction, float speed, LayerMask layer)
    {
        damage     = dmg;
        enemyLayer = layer;
        rb.linearVelocity   = new Vector2(direction * speed, 0f);

        // Voltear sprite si va a la izquierda
        if (direction < 0)
        {
            Vector3 s = transform.localScale;
            s.x = -Mathf.Abs(s.x);
            transform.localScale = s;
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        // Golpear enemigo
        if (((1 << col.gameObject.layer) & enemyLayer) != 0)
        {
            if (col.TryGetComponent<EnemyBase>(out var enemy))
                enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Golpear suelo o pared (layer Ground)
        if (col.gameObject.CompareTag("Ground") || col.gameObject.layer == LayerMask.NameToLayer("Ground"))
            Destroy(gameObject);
    }
}
