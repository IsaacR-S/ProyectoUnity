using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Visuales del Pirómante Real (SRP: separados de la lógica de combate de
/// RoyalPyromancer). Idle: levitación con bob sinusoidal. Cast: pose de
/// casteo + pulso de luz magenta en el CastPoint + scale-punch que preserva
/// el flip. El flash rojo de daño sigue viviendo en EnemyBase.
/// El menú "Candle Fury/Asignar Sprites" cablea castSprite y castLight.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class RoyalPyromancerVisuals : MonoBehaviour
{
    [Header("Levitación")]
    [SerializeField] float bobAmplitude = 0.08f;
    [SerializeField] float bobPeriod    = 2.5f;

    [Header("Casteo")]
    [SerializeField] Sprite  castSprite;
    [SerializeField] float   castPoseTime  = 0.5f;
    [SerializeField] Light2D castLight;
    [SerializeField] float   castLightPeak = 2f;

    SpriteRenderer sr;
    Sprite    idleSprite;
    float     baseY;
    float     bobT;
    Coroutine castRoutine;

    void Awake()
    {
        sr         = GetComponent<SpriteRenderer>();
        idleSprite = sr.sprite;
        baseY      = transform.position.y;
    }

    void Update()
    {
        // El jefe es estático (moveSpeed 0, Rigidbody2D kinemático):
        // bob directo sobre transform.position.y sin pelear con la física.
        bobT += Time.deltaTime;
        float y = baseY + Mathf.Sin(bobT * 2f * Mathf.PI / bobPeriod) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    public void OnCastStart()
    {
        if (castRoutine != null) StopCoroutine(castRoutine);
        castRoutine = StartCoroutine(CastPose());
    }

    IEnumerator CastPose()
    {
        if (castSprite != null) sr.sprite = castSprite;

        float t = 0f;
        while (t < castPoseTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / castPoseTime) * Mathf.PI);   // 0→1→0
            if (castLight != null) castLight.intensity = castLightPeak * k;
            float sign = Mathf.Sign(transform.localScale.x);                    // preservar flip
            float mag  = 1f + 0.06f * k;
            transform.localScale = new Vector3(sign * mag, mag, 1f);
            yield return null;
        }

        if (castLight != null) castLight.intensity = 0f;
        transform.localScale = new Vector3(Mathf.Sign(transform.localScale.x), 1f, 1f);
        sr.sprite   = idleSprite;
        castRoutine = null;
    }
}
