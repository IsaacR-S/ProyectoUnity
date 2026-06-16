using UnityEngine;

/// <summary>
/// Clase base de todos los enemigos.
/// </summary>
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Stats base")]
    [SerializeField] protected int   maxHealth     = 50;
    [SerializeField] protected float moveSpeed     = 2.5f;
    [SerializeField] protected float waxDrop       = 25f;
    [SerializeField] protected int   contactDamage = 10;

    [Header("Patrulla")]
    [SerializeField] protected float patrolDistance = 4f;

    [Header("Invulnerabilidad post-hit")]
    [SerializeField] protected float hitInvulnTime = 0.15f;

    [Header("Screen shake")]
    [SerializeField] protected float hitShakeIntensity   = 0.08f;
    [SerializeField] protected float hitShakeDuration    = 0.1f;
    [SerializeField] protected float deathShakeIntensity = 0.2f;
    [SerializeField] protected float deathShakeDuration  = 0.25f;

    protected int    currentHealth;
    protected bool   isDead;
    protected bool   movingRight = true;
    protected float  startX;
    protected float  invulnTimer;

    protected Rigidbody2D    rb;
    protected Animator       anim;
    protected SpriteRenderer sr;
    protected DropTable      dropTable;

    public bool IsDead => isDead;

    protected virtual void Awake()
    {
        rb        = GetComponent<Rigidbody2D>();
        anim      = GetComponent<Animator>();
        sr        = GetComponent<SpriteRenderer>();
        dropTable = GetComponent<DropTable>();
        startX    = transform.position.x;
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;
    }

    protected virtual void Update()
    {
        if (isDead) return;
        if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;
        Patrol();
    }

    protected virtual void Patrol()
    {
        float speed = movingRight ? moveSpeed : -moveSpeed;
        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);
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

    public virtual void TakeDamage(int damage)
    {
        if (isDead || invulnTimer > 0f) return;
        currentHealth -= damage;
        invulnTimer = hitInvulnTime;

        if (sr != null) StartCoroutine(DamageFlash());
        if (anim != null) anim.SetTrigger("Hit");

        // Shake suave al golpear al enemigo
        CameraFollow.Instance?.Shake(hitShakeIntensity, hitShakeDuration);

        Debug.Log($"[{gameObject.name}] HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0) Die();
    }

    System.Collections.IEnumerator DamageFlash()
    {
        Color orig = sr.color;
        sr.color = Color.red;
        yield return new WaitForSeconds(0.08f);
        if (sr != null) sr.color = orig;
    }

    protected virtual void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        if (waxDrop > 0f)
        {
            WaxSystem ws = FindAnyObjectByType<WaxSystem>();
            if (ws != null) ws.AddWax(waxDrop);
        }

        if (dropTable != null)
            dropTable.Roll(transform.position);

        GameManager.Instance?.RegisterKill();

        // Shake más fuerte al matar enemigo
        CameraFollow.Instance?.Shake(deathShakeIntensity, deathShakeDuration);

        Debug.Log($"[{gameObject.name}] MUERTO — el jugador recibe {waxDrop} cera");
        Destroy(gameObject);
    }

    protected virtual void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            WaxSystem ws = col.gameObject.GetComponent<WaxSystem>();
            if (ws != null) ws.RemoveWax(contactDamage);
        }
    }
}
