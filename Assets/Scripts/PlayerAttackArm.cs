using System.Collections;
using UnityEngine;

/// <summary>
/// Brazo overlay procedural del Príncipe Vela: vive en el hijo "Arm"
/// (siempre activo, con su SpriteRenderer apagado en reposo) y se anima
/// por corrutinas — sin Animator. El flip del jugador (localScale.x
/// negativo) voltea el brazo automáticamente.
/// Configurado por el menú "Candle Fury/Configurar Brazo Jugador".
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAttackArm : MonoBehaviour
{
    [SerializeField] Sprite swordSprite;
    [SerializeField] Sprite castSprite;
    [SerializeField] float  swingTime = 0.18f;
    [SerializeField] float  shootTime = 0.15f;
    [SerializeField] float  pulseTime = 0.25f;

    SpriteRenderer sr;
    Vector3   basePos;
    Transform body;      // raíz del Player (scale-punch del pulso)
    Coroutine current;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.enabled = false;
        basePos = transform.localPosition;
        body    = transform.parent;
    }

    public void PlaySword() => Play(SwordSwing());
    public void PlayShoot() => Play(ShootPunch());
    public void PlayPulse() => Play(PulseRaise());

    void Play(IEnumerator routine)
    {
        if (current != null) StopCoroutine(current);   // no acumular brazos
        ResetVisual();
        current = StartCoroutine(routine);
    }

    void ResetVisual()
    {
        transform.localPosition = basePos;
        transform.localRotation = Quaternion.identity;
        if (body != null)
            body.localScale = new Vector3(Mathf.Sign(body.localScale.x), 1f, 1f);
        sr.enabled = false;
    }

    IEnumerator SwordSwing()
    {
        sr.sprite  = swordSprite;
        sr.enabled = true;
        float t = 0f;
        while (t < swingTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / swingTime) * Mathf.PI * 0.5f); // ease-out
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(70f, -50f, k));
            yield return null;
        }
        ResetVisual();
        current = null;
    }

    IEnumerator ShootPunch()
    {
        sr.sprite  = castSprite;
        sr.enabled = true;
        float t = 0f;
        while (t < shootTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / shootTime) * Mathf.PI);        // ida y vuelta
            transform.localPosition = basePos + new Vector3(0.20f * k, 0f, 0f);
            yield return null;
        }
        ResetVisual();
        current = null;
    }

    IEnumerator PulseRaise()
    {
        sr.sprite  = castSprite;
        sr.enabled = true;
        transform.localRotation = Quaternion.Euler(0f, 0f, 90f);                 // brazo alzado
        float t = 0f;
        while (t < pulseTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / pulseTime) * Mathf.PI);
            if (body != null)
            {
                float sign = Mathf.Sign(body.localScale.x);   // preservar flip
                float mag  = 1f + 0.08f * k;
                body.localScale = new Vector3(sign * mag, mag, 1f);
            }
            yield return null;
        }
        ResetVisual();
        current = null;
    }
}
