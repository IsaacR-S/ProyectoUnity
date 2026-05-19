using System.Collections;
using UnityEngine;

/// <summary>
/// Guardia Vela — enemigo con llama roja.
/// Patrulla y dispara proyectiles de fuego cuando detecta al jugador.
/// Aparece en zonas más cercanas al castillo.
/// </summary>
public class CandleGuard : EnemyBase
{
    [Header("Guardia Vela")]
    [SerializeField] float    detectionRange   = 8f;
    [SerializeField] float    attackRange      = 5f;
    [SerializeField] GameObject fireProjectilePrefab;
    [SerializeField] Transform  firePoint;
    [SerializeField] float    fireRate         = 2.5f;   // segundos entre disparos
    [SerializeField] float    projectileSpeed  = 8f;
    [SerializeField] int      projectileDamage = 18;     // en cera

    Transform player;
    float     fireTimer;
    bool      playerInRange;

    protected override void Awake()
    {
        base.Awake();
        maxHealth     = 80;
        waxDrop       = 20f;
        contactDamage = 12;
        player        = GameObject.FindGameObjectWithTag("Player")?.transform;
        fireTimer     = fireRate;
    }

    protected override void Update()
    {
        if (isDead) return;

        CheckDetection();
        fireTimer -= Time.deltaTime;

        if (playerInRange)
        {
            FacePlayer();
            TryShoot();
        }
        else
        {
            Patrol();
        }
    }

    void CheckDetection()
    {
        if (player == null) return;
        float dist   = Vector2.Distance(transform.position, player.position);
        playerInRange = dist <= detectionRange;
    }

    void FacePlayer()
    {
        if (player == null) return;
        Vector3 s = transform.localScale;
        float dir = player.position.x > transform.position.x ? 1f : -1f;
        s.x = dir > 0 ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        transform.localScale = s;
        // Frenar mientras ataca
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    void TryShoot()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > attackRange)   return;
        if (fireTimer > 0f)       return;
        if (fireProjectilePrefab == null) return;

        fireTimer = fireRate;
        StartCoroutine(ShootRoutine());
    }

    IEnumerator ShootRoutine()
    {
        if (anim != null) anim.SetTrigger("Shoot");
        yield return new WaitForSeconds(0.25f);   // pequeño delay para la animación

        if (player == null) yield break;
        Transform origin = firePoint != null ? firePoint : transform;

        GameObject proj = Instantiate(fireProjectilePrefab, origin.position, Quaternion.identity);
        if (proj.TryGetComponent<EnemyFlameProjectile>(out var efp))
        {
            float dir = player.position.x > origin.position.x ? 1f : -1f;
            efp.Initialize(projectileDamage, dir, projectileSpeed);
        }
        else if (proj.TryGetComponent<Rigidbody2D>(out var prb))
        {
            float dir = player.position.x > origin.position.x ? 1f : -1f;
            prb.linearVelocity = new Vector2(dir * projectileSpeed, 0f);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
