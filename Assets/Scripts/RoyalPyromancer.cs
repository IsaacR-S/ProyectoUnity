using System.Collections;
using UnityEngine;

/// <summary>
/// Pirómante Real — Mini-jefe del Nivel 1.
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
        // Configurar stats ANTES del base.Awake()
        maxHealth     = 150;
        waxDrop       = 0f;       // no cera directa — suelta Núcleo de Llama
        contactDamage = 18;
        moveSpeed     = 0f;       // estático

        base.Awake();
        player    = GameObject.FindGameObjectWithTag("Player")?.transform;
        castTimer = castInterval;
    }

    protected override void Update()
    {
        if (isDead) return;
        if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;

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

        GameManager.Instance?.RegisterKill();
        Debug.Log("[RoyalPyromancer] DERROTADO");

        // Destruir inmediatamente
        Destroy(gameObject);
    }
}
