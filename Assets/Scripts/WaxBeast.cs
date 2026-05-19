using UnityEngine;

/// <summary>
/// Bestia de Cera — primer enemigo del juego.
/// No tiene llama. Se siente atraída por la intensa llama azul del protagonista.
/// Patrulla normalmente; al detectar al jugador, lo persigue.
/// </summary>
public class WaxBeast : EnemyBase
{
    [Header("Bestia de Cera")]
    [SerializeField] float detectionRange  = 6f;    // rango para detectar al jugador
    [SerializeField] float chaseSpeed      = 4.5f;  // velocidad al perseguir

    Transform player;
    bool      chasingPlayer;

    protected override void Awake()
    {
        base.Awake();
        maxHealth    = 40;
        waxDrop      = 10f;
        contactDamage = 8;
        player       = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    protected override void Update()
    {
        if (isDead) return;

        CheckDetection();

        if (chasingPlayer)
            ChasePlayer();
        else
            Patrol();
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

        // Voltear sprite hacia el jugador
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
