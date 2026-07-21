using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Parpadeo genérico de una Light2D (mismo patrón de dos senos que
/// FlameLightFlicker, pero SIN dependencia de WaxSystem — sirve para
/// proyectiles y luces de ambiente).
/// </summary>
[RequireComponent(typeof(Light2D))]
public class LightFlicker2D : MonoBehaviour
{
    [SerializeField] float flickerAmount = 0.25f;  // ±25% de intensidad
    [SerializeField] float speed         = 9f;

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
    }

    void Update()
    {
        float t = Time.time * speed + offset;
        float f = 1f + (Mathf.Sin(t) * 0.6f + Mathf.Sin(t * 2.7f) * 0.4f) * flickerAmount;
        light2d.intensity             = baseIntensity * f;
        light2d.pointLightOuterRadius = baseRadius * (1f + (f - 1f) * 0.5f);
    }
}
