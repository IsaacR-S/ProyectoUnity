using UnityEngine;

// Gota ardiente individual que cae
[RequireComponent(typeof(Rigidbody2D))]
public class BurningDrop : MonoBehaviour
{
    [SerializeField] int damage = 10;

    void Start() => Destroy(gameObject, 4f);

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
            col.gameObject.GetComponent<WaxSystem>()?.RemoveWax(damage);
        Destroy(gameObject);
    }
}