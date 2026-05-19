using UnityEngine;

/// <summary>
/// Cámara suave para side-scroller.
/// Sigue al jugador en X, mantiene Y estable (con suavizado).
/// Tiene límites de nivel configurables.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo")]
    [SerializeField] Transform target;
    [SerializeField] float     smoothSpeed  = 8f;

    [Header("Offset de la cámara")]
    [SerializeField] Vector3   offset       = new Vector3(2f, 1f, -10f);

    [Header("Límites del nivel")]
    [SerializeField] float     minX = -100f;
    [SerializeField] float     maxX =  100f;
    [SerializeField] float     minY =  -5f;
    [SerializeField] float     maxY =   10f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired  = target.position + offset;
        float   clampedX = Mathf.Clamp(desired.x, minX, maxX);
        float   clampedY = Mathf.Clamp(desired.y, minY, maxY);

        Vector3 targetPos = new Vector3(clampedX, clampedY, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
    }
}
