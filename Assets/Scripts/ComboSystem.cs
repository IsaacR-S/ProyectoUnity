using UnityEngine;
using TMPro;

/// <summary>
/// Sistema de combos — MECÁNICA NUEVA (Entrega 3).
/// Matar enemigos seguidos multiplica la cera recibida.
/// x1 → x1.5 → x2 → x3 (máximo). El combo se pierde tras 5s sin matar.
/// Colocar en un GameObject vacío llamado "ComboSystem".
/// </summary>
public class ComboSystem : MonoBehaviour
{
    public static ComboSystem Instance { get; private set; }

    [Header("Combo")]
    [SerializeField] float comboWindow = 5f;          // segundos para mantener el combo
    [SerializeField] float[] multipliers = { 1f, 1.5f, 2f, 3f };

    [Header("UI (opcional)")]
    [SerializeField] TMP_Text comboText;              // texto "COMBO x2"

    int   streak;
    float timer;

    public float CurrentMultiplier
    {
        get
        {
            // streak 1 = x1, streak 2 = x1.5, streak 3 = x2, streak 4+ = x3
            if (streak <= 0) return multipliers[0];
            int idx = Mathf.Min(streak - 1, multipliers.Length - 1);
            return multipliers[idx];
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (streak <= 0) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) ResetCombo();
    }

    /// <summary>Llamar desde EnemyBase.Die() ANTES de dar la cera.</summary>
    public void RegisterKill()
    {
        streak++;
        timer = comboWindow;
        UpdateUI();
    }

    void ResetCombo()
    {
        streak = 0;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (comboText == null) return;
        if (streak >= 2)
        {
            comboText.text = $"COMBO x{CurrentMultiplier:0.#}";
            comboText.color = streak >= 3 ? new Color(1f, 0.42f, 0f) : new Color(1f, 0.72f, 0.19f);
        }
        else
        {
            comboText.text = "";
        }
    }
}
