using UnityEngine;

public abstract class PowerUpBase : MonoBehaviour
{
    [Header("Animación")]
    [SerializeField] protected float bobSpeed = 2f;
    [SerializeField] protected float bobHeight = 0.15f;
    [SerializeField] protected float rotateSpeed = 30f;

    [Header("Vida útil (segundos, 0 = permanente)")]
    [SerializeField] protected float lifetime = 0f;

    protected Vector3 startPos;
    protected float timer;

    protected virtual void Start() => startPos = transform.position;

    protected virtual void Update()
    {
        float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, startPos.y + y, startPos.z);
        transform.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime);

        if (lifetime > 0f)
        {
            timer += Time.deltaTime;
            if (timer >= lifetime) Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;
        Apply(col.gameObject);
        Destroy(gameObject);
    }

    protected abstract void Apply(GameObject player);
}