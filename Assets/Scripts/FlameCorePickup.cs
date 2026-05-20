using UnityEngine;

public class FlameCorePickup : PowerUpBase
{
    protected override void Apply(GameObject player)
    {
        WaxSystem ws = player.GetComponent<WaxSystem>();
        if (ws != null) ws.AddWax(ws.MaxWax);
    }
}