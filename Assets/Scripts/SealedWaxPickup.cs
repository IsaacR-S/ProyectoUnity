using UnityEngine;
using System.Collections;

public class SealedWaxPickup : PowerUpBase
{
    [SerializeField] float duration = 8f;

    protected override void Apply(GameObject player)
    {
        WaxSystem ws = player.GetComponent<WaxSystem>();
        if (ws != null) ws.StartCoroutine(SealRoutine(ws));
    }

    IEnumerator SealRoutine(WaxSystem ws)
    {
        ws.SetDrainPaused(true);
        yield return new WaitForSeconds(duration);
        ws.SetDrainPaused(false);
    }
}