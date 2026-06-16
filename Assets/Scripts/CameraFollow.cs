using UnityEngine;

/// <summary>
/// Cámara suave para side-scroller.
/// Sigue al jugador en X, mantiene Y con suavizado.
/// Soporta SCREEN SHAKE — llamar a CameraFollow.Shake(intensidad, duracion).
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Objetivo")]
    [SerializeField] Transform target;
    [SerializeField] float     smoothSpeed = 8f;

    [Header("Offset de la cámara")]
    [SerializeField] Vector3 offset = new Vector3(2f, 1f, -10f);

    [Header("Límites del nivel")]
    [SerializeField] bool  useLimits = false;
    [SerializeField] float minX = -100f;
    [SerializeField] float maxX = 1000f;
    [SerializeField] float minY = -50f;
    [SerializeField] float maxY =  100f;

    // ── Estado del shake ─────────────────────────────────────────────────────
    float shakeIntensity;
    float shakeDuration;
    float shakeTimer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        float   x       = desired.x;
        float   y       = desired.y;

        if (useLimits)
        {
            x = Mathf.Clamp(x, minX, maxX);
            y = Mathf.Clamp(y, minY, maxY);
        }

        Vector3 targetPos = new Vector3(x, y, transform.position.z);
        Vector3 smoothPos = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);

        // ── Agregar shake encima de la posición suavizada
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float t      = shakeTimer / shakeDuration;       // 1 → 0
            float power  = shakeIntensity * t;               // se atenúa con el tiempo
            float offX   = Random.Range(-1f, 1f) * power;
            float offY   = Random.Range(-1f, 1f) * power;
            smoothPos.x += offX;
            smoothPos.y += offY;
        }

        transform.position = smoothPos;
    }

    /// <summary>
    /// Sacude la cámara. Llamarse desde cualquier script:
    ///   CameraFollow.Instance?.Shake(0.15f, 0.2f);
    /// </summary>
    /// <param name="intensity">Qué tanto se mueve (0.1 suave, 0.5 fuerte)</param>
    /// <param name="duration">Cuánto dura en segundos (0.1 a 0.4 ideal)</param>
    public void Shake(float intensity, float duration)
    {
        // Si ya hay un shake, usa el más fuerte (no se "apilan")
        if (duration > shakeTimer)
        {
            shakeIntensity = intensity;
            shakeDuration  = duration;
            shakeTimer     = duration;
        }
    }
}
