using UnityEngine;
using System.Collections;

public class BurningEdgePickup : PowerUpBase
{
    [SerializeField] float damageMul = 1.25f;
    [SerializeField] float duration = 15f;

    protected override void Apply(GameObject player)
    {
        PlayerCombat pc = player.GetComponent<PlayerCombat>();
        if (pc != null) pc.StartCoroutine(BoostRoutine(pc));
    }

    IEnumerator BoostRoutine(PlayerCombat pc)
    {
        float prev = pc.damageMultiplier;
        pc.damageMultiplier *= damageMul;
        yield return new WaitForSeconds(duration);
        pc.damageMultiplier = prev;
    }
}