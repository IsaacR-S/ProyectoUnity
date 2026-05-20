using UnityEngine;

// Emite gotas ardientes desde el techo
public class BurningDripEmitter : MonoBehaviour
{
    [SerializeField] GameObject burningDropPrefab;
    [SerializeField] float spawnInterval = 1.5f;
    [SerializeField] float randomOffset = 0.5f;

    float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval) { timer = 0f; Spawn(); }
    }

    void Spawn()
    {
        if (burningDropPrefab == null) return;
        Vector3 pos = transform.position + new Vector3(Random.Range(-randomOffset, randomOffset), 0, 0);
        Instantiate(burningDropPrefab, pos, Quaternion.identity);
    }
}