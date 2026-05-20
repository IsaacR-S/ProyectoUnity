using UnityEngine;

public class WaxDropPickup : PowerUpBase
{
    [SerializeField] float waxAmount = 15f;
    protected override void Apply(GameObject player)
    {
        player.GetComponent<WaxSystem>()?.AddWax(waxAmount);
    }
}