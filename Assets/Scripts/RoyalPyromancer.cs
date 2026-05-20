using System.Collections;
using UnityEngine;

/// <summary>
/// Pirómante Real — Mini-jefe del Nivel 1.
/// Estático. Invoca orbes de fuego mágico que persiguen al jugador.
/// Drop: Núcleo de Llama (llena la cera al máximo).
/// </summary>
public class RoyalPyromancer : EnemyBase
{
    [Header("Pirómante Real")]
    [SerializeField] float    detectionRange  = 12f;
    [SerializeField] GameObject magicOrbPrefab;
    [SerializeField] Transform  castPoint;
    [SerializeField] float    castInterval    = 3f;
    [SerializeField] int      orbsPerCast     = 2;
    [SerializeField] float    timeBetweenOrbs = 0.4f;

    [Header("Núcleo de Llama (drop)")]
    [SerializeField] GameObject flameCorePrefab;

    Transform player;
    float     castTimer;
    bool      playerDetected;

    protected override void Awake()
    {
        base.Awake();
        maxHealth     = 150;
        waxDrop       = 0f;
        contactDamage = 18;
        moveSpeed     = 0f;
        player        = GameObject.FindGameObjectWithTag("Player")?.transform;
        castTimer     = castInterval;
        currentHealth = maxHealth;
    }

    protected override void Update()
    {
        if (isDead) return;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        CheckPlayer();
        if (!playerDetected) return;

        FacePlayer();
        castTimer -= Time.deltaTime;
        if (castTimer <= 0f)
        {
            castTimer = castInterval;
            StartCoroutine(CastOrbs());
        }
    }

    void CheckPlayer()
    {
        if (player == null) return;
        playerDetected = Vector2.Distance(transform.position, player.position) <= detectionRange;
    }

    void FacePlayer()
    {
        if (player == null) return;
        Vector3 s = transform.localScale;
        float dir = player.position.x > transform.position.x ? 1f : -1f;
        s.x = dir > 0 ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        transform.localScale = s;
    }

    IEnumerator CastOrbs()
    {
        if (anim != null) anim.SetTrigger("Cast");
        for (int i = 0; i < orbsPerCast; i++)
        {
            if (isDead) yield break;
            if (player != null && magicOrbPrefab != null)
            {
                Transform origin = castPoint != null ? castPoint : transform;
                GameObject orb = Instantiate(magicOrbPrefab, origin.position, Quaternion.identity);
                if (orb.TryGetComponent<MagicOrb>(out var mo))
                    mo.Initialize(player, 20);
            }
            yield return new WaitForSeconds(timeBetweenOrbs);
        }
    }

    protected override void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        if (flameCorePrefab != null)
            Instantiate(flameCorePrefab, transform.position, Quaternion.identity);
        if (anim != null) anim.SetTrigger("Death");
        GameManager.Instance?.RegisterKill();
        Destroy(gameObject, 1.2f);
    }
}
