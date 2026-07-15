using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Utilidad estática (NO es MonoBehaviour): agrega una pequeña Light2D
/// a un proyectil para que se vea volar en la oscuridad del mundo oscuro
/// (Level2). Se llama desde el Awake de FlameProjectile,
/// EnemyFlameProjectile y MagicOrb.
/// En Level1 la luz también existe pero apenas se nota con la luz ambiente.
/// </summary>
public static class ProjectileGlow
{
    public static void Attach(MonoBehaviour host, Color color, float radius = 1f)
    {
        // Si el prefab ya trae una luz configurada a mano, respetarla
        if (host.GetComponentInChildren<Light2D>() != null) return;

        GameObject go = new GameObject("Glow");
        go.transform.SetParent(host.transform, false);

        Light2D l = go.AddComponent<Light2D>();
        l.lightType             = Light2D.LightType.Point;
        l.color                 = color;
        l.pointLightOuterRadius = radius;
        l.pointLightInnerRadius = 0.1f;
        l.falloffIntensity      = 0.7f;
        l.intensity             = 1.2f;
    }
}
