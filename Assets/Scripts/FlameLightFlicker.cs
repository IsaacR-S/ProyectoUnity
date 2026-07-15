using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Parpadeo de la LUZ 2D del Príncipe Vela (reemplaza a la llama
/// placeholder: el sprite oficial ya trae la llama azul dibujada,
/// así que en vez de un segundo sprite flotante, esta clase modula
/// sutilmente la intensidad y el radio de la Light2D del jugador).
///
/// Sincronizado con WaxSystem: con poca cera la luz alumbra menos
/// (refuerza "la cera es tu vida Y tu luz" en el mundo oscuro de Level2).
///
/// USO: componente en el hijo "LuzLlama" del Player (junto a su Light2D).
/// El menú "Candle Fury/Asignar Sprites" o "Aplicar Estética" lo configura.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class FlameLightFlicker : MonoBehaviour
{
    [Header("Parpadeo")]
    [SerializeField] float flickerAmount = 0.15f;  // ±15% de intensidad
    [SerializeField] float speed         = 5f;

    [Header("Sincronización con cera")]
    [SerializeField] bool      syncWithWax = true;
    [SerializeField] WaxSystem waxSystem;
    [SerializeField] float     minWaxFactor = 0.45f; // brillo con cera en 0%

    Light2D light2d;
    float   baseIntensity;
    float   baseRadius;
    float   offset;

    void Awake()
    {
        light2d       = GetComponent<Light2D>();
        baseIntensity = light2d.intensity;
        baseRadius    = light2d.pointLightOuterRadius;
        offset        = Random.Range(0f, 100f);

        if (waxSystem == null)
            waxSystem = GetComponentInParent<WaxSystem>();
    }

    void Update()
    {
        // Parpadeo orgánico con dos senos desfasados
        float t = Time.time * speed + offset;
        float flicker = 1f + (Mathf.Sin(t) * 0.6f + Mathf.Sin(t * 2.7f) * 0.4f) * flickerAmount;

        // Con poca cera, la llama (y su luz) es más débil
        float waxFactor = 1f;
        if (syncWithWax && waxSystem != null)
            waxFactor = Mathf.Lerp(minWaxFactor, 1f, waxSystem.WaxPercent);

        light2d.intensity             = baseIntensity * flicker * waxFactor;
        light2d.pointLightOuterRadius = baseRadius * Mathf.Lerp(0.75f, 1f, waxFactor);
    }
}
