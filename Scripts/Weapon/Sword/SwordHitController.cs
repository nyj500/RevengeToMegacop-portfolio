using UnityEngine;

public class SwordHitController : MonoBehaviour, IDamageable
{
    public void Hit(Bullet bullet)
    {
        bullet.Remove();
    }
}
