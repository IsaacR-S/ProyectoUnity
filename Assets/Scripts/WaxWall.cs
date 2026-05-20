using UnityEngine;

// Muro de cera — solo se rompe con habilidades de fuego
public class WaxWall : MonoBehaviour
{
    [SerializeField] int hp = 1;

    public void HitByFire(int dmg)
    {
        hp -= dmg;
        if (hp <= 0) Destroy(gameObject);
    }
}