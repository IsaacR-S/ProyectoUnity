using UnityEngine;

// Gota ardiente individual que cae
[RequireComponent(typeof(Rigidbody2D))]
public class BurningDrop : MonoBehaviour
{
    [SerializeField] int damage = 10;

    void Awake()
    {
        // Brasa visible en la oscuridad de Level2 (#FF6B3D, radio 1)
        ProjectileGlow.Attach(this, new Color(1f, 0.42f, 0.239f), 1f, 1f, true);
    }

    void Start() => Destroy(gameObject, 4f);

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
            col.gameObject.GetComponent<WaxSystem>()?.RemoveWax(damage);
        Destroy(gameObject);
    }
}