using UnityEngine;

public class ReinforcedWickPickup : PowerUpBase
{
    [SerializeField] float maxWaxBonus = 10f;
    protected override void Apply(GameObject player)
    {
        player.GetComponent<WaxSystem>()?.IncreaseMaxWax(maxWaxBonus);
    }
}