using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistema de Cera — corazón de Candle Fury.
/// La cera se agota con el tiempo y con habilidades de fuego.
/// Matar enemigos repone cera. Al llegar a 0 → GAME OVER.
/// </summary>
public class WaxSystem : MonoBehaviour
{
    // ── Configuración ─────────────────────────────────────────────────────────
    [Header("Cera")]
    [SerializeField] float maxWax            = 100f;
    [SerializeField] float waxDrainPerSecond = 4f;    // pérdida pasiva de cera

    [Header("Llama (multiplicador de consumo)")]
    [Tooltip("Mientras usa habilidades de fuego, la cera se consume N veces más rápido")]
    [SerializeField] float flameMultiplier   = 3f;    // activo solo cuando useFireDrain=true

    // ── Estado ────────────────────────────────────────────────────────────────
    float currentWax;
    bool  useFireDrain;      // activado desde PlayerCombat al usar habilidad de fuego

    // ── Propiedades públicas ──────────────────────────────────────────────────
    public float CurrentWax  => currentWax;
    public float MaxWax      => maxWax;
    public float WaxPercent  => currentWax / maxWax;
    public bool  IsDead      => currentWax <= 0f;

    // ── Eventos ───────────────────────────────────────────────────────────────
    [Header("Eventos")]
    public UnityEvent OnDeath;
    public UnityEvent<float, float> OnWaxChanged;   // (current, max)

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        currentWax = maxWax;
        OnWaxChanged?.Invoke(currentWax, maxWax);
    }

    void Update()
    {
        if (IsDead) return;

        float drain = waxDrainPerSecond * (useFireDrain ? flameMultiplier : 1f);
        RemoveWax(drain * Time.deltaTime);
    }

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>Agrega cera (drop de enemigo, power-up, etc.)</summary>
    public void AddWax(float amount)
    {
        if (IsDead) return;
        currentWax = Mathf.Min(currentWax + amount, maxWax);
        OnWaxChanged?.Invoke(currentWax, maxWax);
    }

    /// <summary>Quita cera (daño, uso de habilidad de fuego, etc.)</summary>
    public void RemoveWax(float amount)
    {
        if (IsDead) return;
        currentWax = Mathf.Max(currentWax - amount, 0f);
        OnWaxChanged?.Invoke(currentWax, maxWax);

        if (currentWax <= 0f)
            Die();
    }

    /// <summary>Amplía la cera máxima (mejora obtenida al derrotar jefes)</summary>
    public void IncreaseMaxWax(float amount)
    {
        maxWax    += amount;
        currentWax = Mathf.Min(currentWax + amount, maxWax);
        OnWaxChanged?.Invoke(currentWax, maxWax);
    }

    /// <summary>Llama a esto mientras una habilidad de fuego esté activa</summary>
    public void SetFireDrain(bool active)
    {
        useFireDrain = active;
    }

    // ── Muerte ────────────────────────────────────────────────────────────────
    void Die()
    {
        currentWax = 0f;
        Debug.Log("[WaxSystem] GAME OVER — La cera se agotó.");
        OnDeath?.Invoke();
        // GameManager.Instance.GameOver() se llama vía el evento OnDeath del Inspector
    }
}
