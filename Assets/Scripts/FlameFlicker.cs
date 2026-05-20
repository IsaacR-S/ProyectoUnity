using UnityEngine;

/// <summary>
/// Llama parpadeante que se sincroniza con la cera del jugador.
/// Asignar a un hijo del Player con un SpriteRenderer (sprite de llama azul).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class FlameFlicker : MonoBehaviour
{
    [Header("Parpadeo de escala")]
    [SerializeField] float scaleMin = 0.85f;
    [SerializeField] float scaleMax = 1.15f;
    [SerializeField] float speed    = 4f;

    [Header("Parpadeo de transparencia")]
    [SerializeField] float alphaMin = 0.75f;
    [SerializeField] float alphaMax = 1f;

    [Header("Sincronización con cera")]
    [SerializeField] bool      syncWithWax = true;
    [SerializeField] WaxSystem waxSystem;

    SpriteRenderer sr;
    Vector3        baseScale;
    float          flickerOffset;

    void Awake()
    {
        sr            = GetComponent<SpriteRenderer>();
        baseScale     = transform.localScale;
        flickerOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float t     = Mathf.PingPong((Time.time + flickerOffset) * speed, 1f);
        float scale = Mathf.Lerp(scaleMin, scaleMax, t);
        float alpha = Mathf.Lerp(alphaMin, alphaMax, t);

        if (syncWithWax && waxSystem != null)
        {
            float waxPct = waxSystem.WaxPercent;
            scale       *= Mathf.Lerp(0.5f, 1f, waxPct);
        }

        transform.localScale = baseScale * scale;
        Color c = sr.color;
        sr.color = new Color(c.r, c.g, c.b, alpha);
    }
}
