using UnityEngine;

/// <summary>
/// Tabla de drops aleatorios.
/// Asignar a cada enemigo para que suelte power-ups con probabilidades configurables.
/// </summary>
[System.Serializable]
public class DropEntry
{
    public GameObject prefab;
    [Range(0f, 100f)] public float chance;     // porcentaje de probabilidad
}

public class DropTable : MonoBehaviour
{
    [Header("Tabla de drops (cada uno se evalúa por separado)")]
    [SerializeField] DropEntry[] drops;

    [Header("Drop garantizado (siempre suelta esto)")]
    [SerializeField] GameObject guaranteedDrop;

    public void Roll(Vector3 position)
    {
        if (guaranteedDrop != null)
            Instantiate(guaranteedDrop, position, Quaternion.identity);

        if (drops == null) return;
        foreach (var d in drops)
        {
            if (d.prefab == null) continue;
            if (Random.Range(0f, 100f) <= d.chance)
                Instantiate(d.prefab, position + (Vector3)(Random.insideUnitCircle * 0.3f), Quaternion.identity);
        }
    }
}
