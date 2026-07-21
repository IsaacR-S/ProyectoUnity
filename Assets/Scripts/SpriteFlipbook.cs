using UnityEngine;

/// <summary>
/// Flipbook mínimo por código (sin Animator): cicla un array de sprites
/// en el SpriteRenderer. Usado por la bola de fuego del jugador.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFlipbook : MonoBehaviour
{
    [SerializeField] Sprite[] frames;
    [SerializeField] float    frameTime = 0.08f;

    SpriteRenderer sr;
    float timer;
    int   index;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        timer += Time.deltaTime;
        if (timer >= frameTime)
        {
            timer -= frameTime;
            index = (index + 1) % frames.Length;
            sr.sprite = frames[index];
        }
    }
}
