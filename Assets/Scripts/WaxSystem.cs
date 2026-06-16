using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistema de Cera — corazón de Candle Fury. (con shake + AUDIO)
/// </summary>
public class WaxSystem : MonoBehaviour
{
    [Header("Cera")]
    [SerializeField] float maxWax            = 100f;
    [SerializeField] float waxDrainPerSecond = 3.5f;

    [Header("Llama (multiplicador de consumo)")]
    [SerializeField] float flameMultiplier = 3f;

    [Header("Screen shake")]
    [SerializeField] float damageShakeIntensity = 0.15f;
    [SerializeField] float damageShakeDuration  = 0.2f;
    [SerializeField] float deathShakeIntensity  = 0.4f;
    [SerializeField] float deathShakeDuration   = 0.6f;

    float currentWax;
    bool  useFireDrain;
    bool  drainPaused;

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
        ApplyDrain(drain * Time.deltaTime);
    }

    // Drenaje pasivo — NO dispara sonido ni shake
    void ApplyDrain(float amount)
    {
        if (IsDead) return;
        currentWax = Mathf.Max(currentWax - amount, 0f);
        OnWaxChanged?.Invoke(currentWax, maxWax);
        if (currentWax <= 0f) Die();
    }

    public void AddWax(float amount)
    {
        if (IsDead) return;
        currentWax = Mathf.Min(currentWax + amount, maxWax);
        OnWaxChanged?.Invoke(currentWax, maxWax);

        // Sonido de recoger cera
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxPickup);
    }

    public void RemoveWax(float amount)
    {
        if (IsDead) return;
        currentWax = Mathf.Max(currentWax - amount, 0f);
        OnWaxChanged?.Invoke(currentWax, maxWax);

        // Shake + sonido cuando el jugador recibe daño real (no por drenaje pasivo)
        if (amount > 1f)
        {
            CameraFollow.Instance?.Shake(damageShakeIntensity, damageShakeDuration);
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxPlayerHurt);
        }

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

        CameraFollow.Instance?.Shake(deathShakeIntensity, deathShakeDuration);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxGameOver);
        if (AudioManager.Instance != null && AudioManager.Instance.musicGameOver != null)
            AudioManager.Instance.PlayMusic(AudioManager.Instance.musicGameOver);

        OnDeath?.Invoke();
    }
}