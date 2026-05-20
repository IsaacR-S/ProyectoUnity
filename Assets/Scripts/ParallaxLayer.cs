using UnityEngine;

/// <summary>
/// Capa de fondo con efecto parallax.
/// parallaxFactor:
///   - 0.1 = capa más lejana (apenas se mueve)
///   - 0.5 = capa media
///   - 0.9 = casi al ritmo de la cámara
/// </summary>
public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] [Range(0f, 1f)] float parallaxFactor = 0.3f;
    [SerializeField] bool followY = false;

    Transform cam;
    Vector3   startPos;
    float     startCamX, startCamY;

    void Start()
    {
        cam       = Camera.main.transform;
        startPos  = transform.position;
        startCamX = cam.position.x;
        startCamY = cam.position.y;
    }

    void LateUpdate()
    {
        if (cam == null) return;
        float dx = (cam.position.x - startCamX) * parallaxFactor;
        float dy = followY ? (cam.position.y - startCamY) * parallaxFactor : 0f;
        transform.position = new Vector3(startPos.x + dx, startPos.y + dy, startPos.z);
    }
}
