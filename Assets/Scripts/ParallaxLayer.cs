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
    bool      initialized;
    int       frames;

    void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (cam == null)
        {
            if (Camera.main == null) return;
            cam = Camera.main.transform;
        }

        // La referencia se captura en el SEGUNDO frame, no en Start():
        // en el frame 1 CameraFollow todavía no movió la cámara hacia el
        // player, y capturar antes dejaba el fondo desincronizado.
        if (!initialized)
        {
            frames++;
            if (frames < 2) return;
            startPos    = transform.position;
            startCamX   = cam.position.x;
            startCamY   = cam.position.y;
            initialized = true;
            return;
        }

        float dx = (cam.position.x - startCamX) * parallaxFactor;
        float dy = followY ? (cam.position.y - startCamY) * parallaxFactor : 0f;
        transform.position = new Vector3(startPos.x + dx, startPos.y + dy, startPos.z);
    }
}
