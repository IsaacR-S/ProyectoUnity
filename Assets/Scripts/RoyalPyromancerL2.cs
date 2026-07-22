using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Pirómante Real del NIVEL 2 — JEFE FINAL (Entrega 3).
/// Variante espectral del jefe: más grande, tono hielo y aura azul-violeta.
/// Todo se aplica EN RUNTIME desde esta clase, así ningún menú del editor
/// (Asignar Sprites, Aplicar Estética) puede desconfigurarlo, y el jefe
/// de Level1 (RoyalPyromancerL1) queda intacto.
///
/// Diferencias vs. el base:
///   • HP 200 (en vez de 150) — se fija tras base.Awake() porque el Awake
///     del base lo sobreescribiría; currentHealth se inicializa después,
///     en el Start() de EnemyBase, así que el orden es correcto.
///   • Escala ×1.5 (conserva el signo de X) + medio paso hacia arriba para
///     no quedar enterrado. El CapsuleCollider2D escala solo con el transform.
///   • Tint hielo #9AD4FF (claro: NO oscurece el sprite bajo las luces).
///   • Luz "LuzEnemiga" en #7A60FF con radio 5 (proporcional al tamaño).
///   • Orbes: usa el prefab MagicOrb_L2 (violeta) si está asignado — lo
///     asigna el menú "Vestir Nivel" en Level2. El MagicOrb original de L1
///     no se toca.
/// </summary>
public class RoyalPyromancerL2 : RoyalPyromancer
{
    [Header("Jefe final (solo Level2)")]
    [SerializeField] float      sizeMultiplier = 1.5f;
    [SerializeField] Color      iceTint        = new Color(0.604f, 0.831f, 1f);   // #9AD4FF
    [SerializeField] Color      auraColor      = new Color(0.478f, 0.376f, 1f);   // #7A60FF
    [SerializeField] float      auraRadius     = 5f;
    [SerializeField] GameObject magicOrbL2Prefab;   // orbe violeta propio (MagicOrb_L2)

    protected override void Awake()
    {
        base.Awake();   // stats del base (150 HP) + referencias de EnemyBase

        // Jefe final: más vida (currentHealth se fija en Start de EnemyBase)
        maxHealth = 200;

        // 1.5× conservando el signo de X para el flip
        Vector3 s = transform.localScale;
        transform.localScale = new Vector3(s.x * sizeMultiplier, s.y * sizeMultiplier, s.z);

        // Subirlo para que el sprite agrandado no quede enterrado en el suelo
        float extra = (sizeMultiplier - 1f) * 0.5f;
        transform.position += Vector3.up * Mathf.Max(0.3f, extra);

        // Tono hielo claro (aditivo a la vista, no mata la visibilidad)
        if (sr != null) sr.color = iceTint;

        // Orbe propio del jefe final (no compartido con L1)
        if (magicOrbL2Prefab != null) magicOrbPrefab = magicOrbL2Prefab;
    }

    protected override void Start()
    {
        base.Start();

        // Aura azul-violeta (pisa lo que haya configurado el editor)
        Transform luz = transform.Find("LuzEnemiga");
        if (luz != null && luz.TryGetComponent(out Light2D l))
        {
            l.color                 = auraColor;
            l.pointLightOuterRadius = auraRadius;
            l.intensity             = 1.3f;
        }
    }
}
