using UnityEngine;

// Trampa de fuego — pulsa cada N segundos
public class FireTrap : MonoBehaviour
{
    [Header("Pulso de fuego")]
    [SerializeField] float pulseInterval = 2f;
    [SerializeField] float activeDuration = 0.7f;
    [SerializeField] int damage = 20;

    [Header("Visuales (opcional)")]
    [SerializeField] GameObject flameVisual;
    [SerializeField] Collider2D damageCollider;

    float timer;
    bool isActive;

    void Start()
    {
        if (damageCollider != null) damageCollider.enabled = false;
        if (flameVisual != null) flameVisual.SetActive(false);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (!isActive && timer >= pulseInterval) { Activate(); timer = 0f; }
        else if (isActive && timer >= activeDuration) { Deactivate(); timer = 0f; }
    }

    void Activate()
    {
        isActive = true;
        if (damageCollider != null) damageCollider.enabled = true;
        if (flameVisual != null) flameVisual.SetActive(true);
    }

    void Deactivate()
    {
        isActive = false;
        if (damageCollider != null) damageCollider.enabled = false;
        if (flameVisual != null) flameVisual.SetActive(false);
    }

    void OnTriggerStay2D(Collider2D col)
    {
        if (!isActive) return;
        if (col.CompareTag("Player"))
            col.GetComponent<WaxSystem>()?.RemoveWax(damage * Time.deltaTime * 2f);
    }
}