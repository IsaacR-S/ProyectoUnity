using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistema de Cera — corazón de Candle Fury.
/// La cera se agota con el tiempo y con habilidades de fuego.
/// Matar enemigos repone cera. Al llegar a 0 → GAME OVER.
/// </summary>
public class WaxSystem : MonoBehaviour
{
    [Header("Cera")]
    [SerializeField] float maxWax            = 100f;
    [SerializeField] float waxDrainPerSecond = 4f;

    [Header("Llama (multiplicador de consumo)")]
    [SerializeField] float flameMultiplier   = 3f;

    float currentWax;
    bool  useFireDrain;
    bool  drainPaused;       // power-up "Cera Sellada"

    public float CurrentWax => currentWax;
    public float MaxWax     => maxWax;
    public float WaxPercent => currentWax / maxWax;
    public bool  IsDead     => currentWax <= 0f;

    [Header("Eventos")]
    public UnityEvent OnDeath;
    public UnityEvent<float, float> OnWaxChanged;

    void Start()
    {
        currentWax = maxWax;
        OnWaxChanged?.Invoke(currentWax, maxWax);
    }

    void Update()
    {
        if (IsDead || drainPaused) return;
        float drain = waxDrainPerSecond * (useFireDrain ? flameMultiplier : 1f);
        RemoveWax(drain * Time.deltaTime);
    }

    public void AddWax(float amount)
    {
        if (IsDead) return;
        currentWax = Mathf.Min(currentWax + amount, maxWax);
        OnWaxChanged?.Invoke(currentWax, maxWax);
    }

    public void RemoveWax(float amount)
    {
        if (IsDead) return;
        currentWax = Mathf.Max(currentWax - amount, 0f);
        OnWaxChanged?.Invoke(currentWax, maxWax);
        if (currentWax <= 0f) Die();
    }

    public void IncreaseMaxWax(float amount)
    {
        maxWax    += amount;
        currentWax = Mathf.Min(currentWax + amount, maxWax);
        OnWaxChanged?.Invoke(currentWax, maxWax);
    }

    public void SetFireDrain(bool active)   => useFireDrain = active;
    public void SetDrainPaused(bool paused) => drainPaused  = paused;

    void Die()
    {
        currentWax = 0f;
        Debug.Log("[WaxSystem] GAME OVER — La cera se agotó.");
        OnDeath?.Invoke();
    }
}
