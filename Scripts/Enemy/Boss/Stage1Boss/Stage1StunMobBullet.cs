using UnityEngine;

public class Stage1StunMobBullet : Bullet
{
    private bool reflected;

    protected override void OnReflected(bool isParry)
    {
        reflected = true;
    }

    override protected void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        GameObject obj = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
        bool wasReflected = reflected;
        base.OnTriggerEnter(other);
        bool hitEnemy = obj.CompareTag("Enemy") || other.gameObject.CompareTag("Enemy");
        if (!hitEnemy || wasReflected)
            GetComponentInChildren<BulletVFX>()?.PlayHit(transform.position, Quaternion.identity);
    }
}
