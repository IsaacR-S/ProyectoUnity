using System.Collections;
using UnityEngine;

/// <summary>
/// Combate del Príncipe Vela.
/// Habilidades FÍSICAS: no cuestan cera.
/// Habilidades de FUEGO: consumen cera mientras se usan.
/// </summary>
[RequireComponent(typeof(WaxSystem))]
[RequireComponent(typeof(PlayerController))]
public class PlayerCombat : MonoBehaviour
{
    // ── Habilidades físicas ───────────────────────────────────────────────────
    [Header("Espadazo (físico — sin costo)")]
    [SerializeField] Transform  attackPoint;
    [SerializeField] float      attackRadius   = 0.6f;
    [SerializeField] int        attackDamage   = 20;
    [SerializeField] float      attackCooldown = 0.35f;
    [SerializeField] LayerMask  enemyLayer;

    // ── Habilidades de fuego ──────────────────────────────────────────────────
    [Header("Pulso de llama (fuego — consume cera)")]
    [SerializeField] float pulseRadius     = 3f;
    [SerializeField] int   pulseDamage     = 35;
    [SerializeField] float pulseCooldown   = 1.2f;
    [SerializeField] float pulseWaxCost    = 15f;   // costo fijo en cera
    [SerializeField] GameObject pulseWavePrefab;     // anillo visual de la onda

    [Header("Proyectil de llama (fuego — consume cera)")]
    [SerializeField] GameObject flamePrefab;         // prefab simple con Rigidbody2D
    [SerializeField] float      flameSpeed    = 12f;
    [SerializeField] float      flameCooldown = 0.8f;
    [SerializeField] float      flameWaxCost  = 10f;

    // ── Componentes ───────────────────────────────────────────────────────────
    WaxSystem        waxSystem;
    PlayerController controller;
    Animator         anim;
    PlayerAttackArm  arm;

    // ── Timers ────────────────────────────────────────────────────────────────
    float attackTimer;
    float pulseTimer;
    float flameTimer;

    // ── Estadísticas (suben con mejoras) ─────────────────────────────────────
    [HideInInspector] public float damageMultiplier = 1f;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        waxSystem  = GetComponent<WaxSystem>();
        controller = GetComponent<PlayerController>();
        anim       = GetComponent<Animator>();
        arm        = GetComponentInChildren<PlayerAttackArm>();
    }

    void Update()
    {
        if (waxSystem.IsDead) return;

        attackTimer -= Time.deltaTime;
        pulseTimer  -= Time.deltaTime;
        flameTimer  -= Time.deltaTime;

        // ── Espadazo: Z o botón Fire1
        if ((Input.GetKeyDown(KeyCode.Z) || Input.GetButtonDown("Fire1")) && attackTimer <= 0f)
            SwordAttack();

        // ── Pulso de llama: X
        if (Input.GetKeyDown(KeyCode.X) && pulseTimer <= 0f)
            FlameCheck();

        // ── Proyectil: C o Fire2
        if ((Input.GetKeyDown(KeyCode.C) || Input.GetButtonDown("Fire2")) && flameTimer <= 0f)
            ShootFlame();
    }

    // ── Espadazo (físico) ─────────────────────────────────────────────────────
    void SwordAttack()
    {
        attackTimer = attackCooldown;
        anim.SetTrigger("Attack");
        arm?.PlaySword();

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, enemyLayer);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxSword);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<EnemyBase>(out var enemy))
                enemy.TakeDamage(Mathf.RoundToInt(attackDamage * damageMultiplier));
        }
    }

    // ── Pulso de llama (fuego) ────────────────────────────────────────────────
    void FlameCheck()
    {
        if (waxSystem.CurrentWax < pulseWaxCost)
        {
            Debug.Log("[Combat] Sin cera suficiente para el Pulso.");
            return;
        }

        pulseTimer = pulseCooldown;
        waxSystem.RemoveWax(pulseWaxCost);
        anim.SetTrigger("Pulse");
        arm?.PlayPulse();

        if (pulseWavePrefab != null)
            Instantiate(pulseWavePrefab, transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pulseRadius, enemyLayer);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxFire);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<EnemyBase>(out var enemy))
                enemy.TakeDamage(Mathf.RoundToInt(pulseDamage * damageMultiplier));
        }

        // Pequeño retroceso visual (opcional)
        StartCoroutine(PulseEffect());
    }

    IEnumerator PulseEffect()
    {
        waxSystem.SetFireDrain(true);
        yield return new WaitForSeconds(0.25f);
        waxSystem.SetFireDrain(false);
    }

    // ── Proyectil de llama (fuego) ────────────────────────────────────────────
    void ShootFlame()
    {
        if (flamePrefab == null) return;
        if (waxSystem.CurrentWax < flameWaxCost)
        {
            Debug.Log("[Combat] Sin cera suficiente para el proyectil.");
            return;
        }

        flameTimer = flameCooldown;
        waxSystem.RemoveWax(flameWaxCost);
        anim.SetTrigger("Shoot");
        arm?.PlayShoot();

        float dir = controller.FacingRight ? 1f : -1f;
        GameObject proj = Instantiate(flamePrefab, attackPoint.position, Quaternion.identity);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxFire);
        if (proj.TryGetComponent<FlameProjectile>(out var fp))
        {
            fp.Initialize(Mathf.RoundToInt(25 * damageMultiplier), dir, flameSpeed, enemyLayer);
        }
        else
        {
            // fallback: simple rigidbody push
            if (proj.TryGetComponent<Rigidbody2D>(out var projRb))
                projRb.linearVelocity = new Vector2(dir * flameSpeed, 0f);
        }
    }

    // ── Debug ─────────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pulseRadius);
    }
}
