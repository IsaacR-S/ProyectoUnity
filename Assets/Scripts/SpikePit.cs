using UnityEngine;

// Foso de púas — muerte instantánea al caer
public class SpikePit : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;
        WaxSystem ws = col.GetComponent<WaxSystem>();
        if (ws != null) ws.RemoveWax(99999f);
    }
}