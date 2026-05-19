using UnityEngine;

/// <summary>
/// Clase base de todos los enemigos.
/// Hereda de aquí: WaxBeast y CandleGuard.
/// </summary>
public abstract class EnemyBase : MonoBehaviour
{
    // ── Stats configurables ───────────────────────────────────────────────────
    [Header("Stats base")]
    [SerializeField] protected int   maxHealth   = 50;
    [SerializeField] protected float moveSpeed   = 2.5f;
    [SerializeField] protected float waxDrop     = 12f;   // cera que otorga al morir
    [SerializeField] protected int   contactDamage = 10;  // daño al tocar al jugador

    // ── Patrulla ──────────────────────────────────────────────────────────────
    [Header("Patrulla")]
    [SerializeField] protected float patrolDistance = 4f;

    // ── Estado ────────────────────────────────────────────────────────────────
    protected int   currentHealth;
    protected bool  isDead;
    protected bool  movingRight = true;
    protected float startX;

    // ── Componentes ───────────────────────────────────────────────────────────
    protected Rigidbody2D rb;
    protected Animator    anim;

    // ── Propiedades ───────────────────────────────────────────────────────────
    public bool IsDead => isDead;

    // ─────────────────────────────────────────────────────────────────────────
    protected virtual void Awake()
    {
        rb            = GetComponent<Rigidbody2D>();
        anim          = GetComponent<Animator>();
        currentHealth = maxHealth;
        startX        = transform.position.x;
    }

    protected virtual void Update()
    {
        if (isDead) return;
        Patrol();
    }

    // ── Patrulla simple ───────────────────────────────────────────────────────
    protected virtual void Patrol()
    {
        float speed = movingRight ? moveSpeed : -moveSpeed;
        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);

        // Voltea al llegar al límite de patrulla
        if (movingRight  && transform.position.x >= startX + patrolDistance)  Flip();
        if (!movingRight && transform.position.x <= startX - patrolDistance)  Flip();
    }

    protected void Flip()
    {
        movingRight = !movingRight;
        Vector3 s = transform.localScale;
        s.x = -s.x;
        transform.localScale = s;
    }

    // ── Recibir daño ──────────────────────────────────────────────────────────
    public virtual void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        if (anim != null) anim.SetTrigger("Hit");

        if (currentHealth <= 0)
            Die();
    }

    // ── Muerte ────────────────────────────────────────────────────────────────
    protected virtual void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        // Reponer cera al jugador
        WaxSystem ws = FindFirstObjectByType<WaxSystem>();
        if (ws != null)
            ws.AddWax(waxDrop);

        if (anim != null) anim.SetTrigger("Death");
        Destroy(gameObject, 0.8f);  // tiempo para que termine la animación de muerte
    }

    // ── Daño por contacto ─────────────────────────────────────────────────────
    protected virtual void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            WaxSystem ws = col.gameObject.GetComponent<WaxSystem>();
            if (ws != null) ws.RemoveWax(contactDamage);
        }
    }
}
