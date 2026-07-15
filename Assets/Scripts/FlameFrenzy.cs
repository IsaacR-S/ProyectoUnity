using UnityEngine;

/// <summary>
/// FRENESÍ DE LLAMA — MECÁNICA NUEVA (Entrega 3).
/// Refuerza el loop "matar para vivir":
/// cada vez que el príncipe mata un enemigo, entra en FRENESÍ durante 3 s:
///   • +30% de velocidad de movimiento
///   • el drenaje pasivo de cera SE PAUSA (la llama "se alimenta" de la kill)
/// Matar otro enemigo durante el frenesí reinicia el temporizador,
/// así que encadenar kills = correr más rápido y no gastar cera.
///
/// CÓMO SE USA:
///   1. Agregar este componente al Player (el menú
///      "Candle Fury/Aplicar Estética" lo agrega automáticamente).
///   2. (Opcional) Asignar en flameRenderer el SpriteRenderer de la llama
///      (hijo FlameVisual) para que se tiña de celeste durante el frenesí.
/// No necesita más configuración: detecta las kills leyendo
/// GameManager.KillCount, sin tocar los scripts de enemigos.
/// </summary>
[RequireComponent(typeof(WaxSystem))]
[RequireComponent(typeof(PlayerController))]
public class FlameFrenzy : MonoBehaviour
{
    [Header("Frenesí")]
    [SerializeField] float duration   = 3f;     // segundos de frenesí por kill
    [SerializeField] float speedBonus = 1.3f;   // multiplicador de velocidad

    [Header("Feedback visual (opcional)")]
    [SerializeField] SpriteRenderer flameRenderer;              // ej. hijo FlameVisual
    [SerializeField] Color          frenzyTint = new Color(0.6f, 0.95f, 1f);

    WaxSystem        wax;
    PlayerController controller;

    int   lastKills;
    float timer;
    bool  active;
    Color originalTint;

    void Awake()
    {
        wax        = GetComponent<WaxSystem>();
        controller = GetComponent<PlayerController>();
    }

    void Start()
    {
        // GameManager persiste entre escenas: partimos del conteo actual
        if (GameManager.Instance != null) lastKills = GameManager.Instance.KillCount;
        if (flameRenderer != null)        originalTint = flameRenderer.color;
    }

    void Update()
    {
        if (wax.IsDead)
        {
            if (active) Deactivate();
            return;
        }

        // ¿Hubo una kill nueva desde el último frame?
        int kills = GameManager.Instance != null ? GameManager.Instance.KillCount : lastKills;
        if (kills > lastKills)
        {
            lastKills = kills;
            Activate();
        }

        if (!active) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) Deactivate();
    }

    void Activate()
    {
        timer = duration;                        // cada kill reinicia el tiempo
        if (active) return;

        active = true;
        controller.speedMultiplier = speedBonus;
        wax.SetDrainPaused(true);                // la llama no consume cera
        if (flameRenderer != null) flameRenderer.color = frenzyTint;
        Debug.Log("[FlameFrenzy] ¡FRENESÍ! +velocidad y drenaje pausado.");
    }

    void Deactivate()
    {
        active = false;
        controller.speedMultiplier = 1f;
        wax.SetDrainPaused(false);
        if (flameRenderer != null) flameRenderer.color = originalTint;
    }
}
