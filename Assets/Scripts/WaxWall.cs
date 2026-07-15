using UnityEngine;

// Muro de cera — INDESTRUCTIBLE por decisión de diseño (Entrega 3):
// nadie puede destruirlo. El "secreto" detrás del muro se alcanza
// saltando por encima (doble salto / dash).
public class WaxWall : MonoBehaviour
{
    // Se mantiene el método por compatibilidad, pero ya no destruye el muro.
    public void HitByFire(int dmg)
    {
        Debug.Log("[WaxWall] El muro de cera es indestructible.");
    }
}