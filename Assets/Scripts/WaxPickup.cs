using UnityEngine;

/// <summary>
/// Gota de cera coleccionable en el suelo.
/// Se genera desde EnemyBase.Die() o colocada manualmente en el nivel.
/// </summary>
public class WaxPickup : MonoBehaviour
{
    [SerializeField] float waxAmount = 8f;
    [SerializeField] float bobSpeed  = 2f;
    [SerializeField] float bobHeight = 0.15f;

    Vector3 startPos;

    void Start() => startPos = transform.position;

    void Update()
    {
        // Efecto de flotación
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;
        col.GetComponent<WaxSystem>()?.AddWax(waxAmount);
        Destroy(gameObject);
    }
}
