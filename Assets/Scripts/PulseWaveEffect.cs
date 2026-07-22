using UnityEngine;

/// <summary>
/// Onda expansiva del Pulso de llama: escala el anillo desde startDiameter
/// hasta endDiameter (= diámetro real del daño del pulso) con fade de alpha,
/// y se autodestruye. Sin física: es solo feedback visual.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PulseWaveEffect : MonoBehaviour
{
    [SerializeField] float duration      = 0.35f;
    [SerializeField] float startDiameter = 0.5f;
    [SerializeField] float endDiameter   = 6f;    // = pulseRadius 3 × 2

    SpriteRenderer sr;
    float t;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        float eased = Mathf.Sin(k * Mathf.PI * 0.5f);                  // ease-out
        float diam  = Mathf.Lerp(startDiameter, endDiameter, eased);
        float spriteDiam = sr.sprite != null ? sr.sprite.bounds.size.x : 1f;
        float s = diam / spriteDiam;
        transform.localScale = new Vector3(s, s, 1f);

        Color c = sr.color;
        c.a = 1f - k;
        sr.color = c;

        if (k >= 1f) Destroy(gameObject);
    }
}
