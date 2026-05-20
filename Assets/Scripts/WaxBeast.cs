using UnityEngine;

/// <summary>
/// Bestia de Cera — primer enemigo del juego.
/// </summary>
public class WaxBeast : EnemyBase
{
    [Header("Bestia de Cera")]
    [SerializeField] float detectionRange = 6f;
    [SerializeField] float chaseSpeed     = 4.5f;

    Transform player;
    bool      chasingPlayer;

    protected override void Awake()
    {
        // Configurar stats ANTES del base.Awake() para evitar bugs
        maxHealth     = 40;
        waxDrop       = 25f;   // suelta buena cera
        contactDamage = 8;

        base.Awake();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    protected override void Update()
    {
        if (isDead) return;
        if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;

        CheckDetection();

        if (chasingPlayer) ChasePlayer();
        else               Patrol();
    }

    void CheckDetection()
    {
        if (player == null) return;
        float dist = Vector2.Distance(transform.position, player.position);
        chasingPlayer = dist <= detectionRange;
    }

    void ChasePlayer()
    {
        if (player == null) return;
        float dir = player.position.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * chaseSpeed, rb.linearVelocity.y);

        Vector3 s = transform.localScale;
        s.x = dir > 0 ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        transform.localScale = s;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
