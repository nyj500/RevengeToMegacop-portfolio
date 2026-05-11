using UnityEngine;

public class Stage1BossBullet : Bullet
{
    override protected void OnTriggerEnter(UnityEngine.Collider other)
    {
        base.OnTriggerEnter(other);
        GetComponentInChildren<BulletVFX>()?.PlayHit(transform.position, Quaternion.identity);
    }
}
