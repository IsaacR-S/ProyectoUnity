using UnityEngine;

/// <summary>
/// Orbe de fuego mágico que persigue al jugador.
/// Lanzado por el RoyalPyromancer.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MagicOrb : MonoBehaviour
{
    [SerializeField] float speed          = 4f;
    [SerializeField] float lifetime       = 5f;
    [SerializeField] float homingStrength = 2f;

    Transform target;
    int       damage;
    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        // Luz magenta pequeña para verse volar en la oscuridad de Level2
        ProjectileGlow.Attach(this, new Color(1f, 0.251f, 0.502f), 1f);
    }

    public void Initialize(Transform t, int dmg)
    {
        target = t;
        damage = dmg;
    }

    void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f) Destroy(gameObject);
    }

    void FixedUpdate()
    {
        if (target == null) return;
        Vector2 toTarget = ((Vector2)target.position - (Vector2)transform.position).normalized;
        Vector2 currentDir = rb.linearVelocity.normalized;
        if (currentDir == Vector2.zero) currentDir = toTarget;
        Vector2 newDir = Vector2.Lerp(currentDir, toTarget, homingStrength * Time.fixedDeltaTime);
        rb.linearVelocity = newDir.normalized * speed;
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            col.GetComponent<WaxSystem>()?.RemoveWax(damage);
            Destroy(gameObject);
        }
        else if (col.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
