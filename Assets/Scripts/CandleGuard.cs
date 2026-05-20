using System.Collections;
using UnityEngine;

/// <summary>
/// Guardia Vela — enemigo con llama roja.
/// Persigue al jugador, dispara proyectiles cuando lo tiene en rango,
/// y hace ataque cuerpo a cuerpo si está pegado.
/// </summary>
public class CandleGuard : EnemyBase
{
    [Header("Guardia Vela")]
    [SerializeField] float    detectionRange   = 10f;
    [SerializeField] float    attackRange      = 6f;
    [SerializeField] float    meleeRange       = 1.5f;
    [SerializeField] float    chaseSpeed       = 3.5f;

    [Header("Disparo (opcional)")]
    [SerializeField] GameObject fireProjectilePrefab;
    [SerializeField] Transform  firePoint;
    [SerializeField] float    fireRate         = 2.5f;
    [SerializeField] float    projectileSpeed  = 8f;
    [SerializeField] int      projectileDamage = 18;

    [Header("Ataque cuerpo a cuerpo")]
    [SerializeField] int   meleeDamage   = 15;
    [SerializeField] float meleeCooldown = 1.2f;

    Transform player;
    float     fireTimer;
    float     meleeTimer;
    bool      playerDetected;

    protected override void Awake()
    {
        maxHealth     = 80;
        waxDrop       = 40f;
        contactDamage = 12;

        base.Awake();
        player    = GameObject.FindGameObjectWithTag("Player")?.transform;
        fireTimer = fireRate;
    }

    protected override void Update()
    {
        if (isDead) return;
        if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;

        fireTimer  -= Time.deltaTime;
        meleeTimer -= Time.deltaTime;

        CheckDetection();

        if (!playerDetected)
        {
            // No ve al jugador: patrulla normal
            Patrol();
            return;
        }

        // Sí ve al jugador: decide qué hacer según la distancia
        float dist = Vector2.Distance(transform.position, player.position);
        FacePlayer();

        if (dist <= meleeRange)
        {
            // Muy cerca: ataque cuerpo a cuerpo
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            TryMelee();
        }
        else if (dist <= attackRange && fireProjectilePrefab != null)
        {
            // Distancia media: dispara desde lejos
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            TryShoot();
        }
        else
        {
            // Lejos: persigue al jugador
            ChasePlayer();
        }
    }

    void CheckDetection()
    {
        if (player == null) return;
        float dist     = Vector2.Distance(transform.position, player.position);
        playerDetected = dist <= detectionRange;
    }

    void FacePlayer()
    {
        if (player == null) return;
        Vector3 s = transform.localScale;
        float dir = player.position.x > transform.position.x ? 1f : -1f;
        s.x = dir > 0 ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        transform.localScale = s;
    }

    void ChasePlayer()
    {
        if (player == null) return;
        float dir = player.position.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * chaseSpeed, rb.linearVelocity.y);
    }

    void TryMelee()
    {
        if (meleeTimer > 0f) return;
        if (player == null)  return;

        meleeTimer = meleeCooldown;
        if (anim != null) anim.SetTrigger("Attack");

        // Verifica que el jugador siga en rango y aplica daño
        if (Vector2.Distance(transform.position, player.position) <= meleeRange)
        {
            WaxSystem ws = player.GetComponent<WaxSystem>();
            if (ws != null) ws.RemoveWax(meleeDamage);
            Debug.Log($"[CandleGuard] Ataque cuerpo a cuerpo → {meleeDamage} daño");
        }
    }

    void TryShoot()
    {
        if (fireTimer > 0f) return;
        fireTimer = fireRate;
        StartCoroutine(ShootRoutine());
    }

    IEnumerator ShootRoutine()
    {
        if (anim != null) anim.SetTrigger("Shoot");
        yield return new WaitForSeconds(0.25f);

        if (player == null || isDead) yield break;
        Transform origin = firePoint != null ? firePoint : transform;

        GameObject proj = Instantiate(fireProjectilePrefab, origin.position, Quaternion.identity);
        float dir = player.position.x > origin.position.x ? 1f : -1f;

        if (proj.TryGetComponent<EnemyFlameProjectile>(out var efp))
        {
            efp.Initialize(projectileDamage, dir, projectileSpeed);
        }
        else if (proj.TryGetComponent<Rigidbody2D>(out var prb))
        {
            prb.linearVelocity = new Vector2(dir * projectileSpeed, 0f);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}
