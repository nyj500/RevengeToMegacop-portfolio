using UnityEngine;

public class Stage1BossMissile : Bullet
{
    [SerializeField] private float turnSpeed = 180f;
    [SerializeField] private float lifetime = 5f;

    private Transform target;
    private Transform playerTarget;
    private Transform bossTransform;
    private float elapsed;

    override protected void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);
        GetComponentInChildren<BulletVFX>()?.PlayHit(transform.position, Quaternion.identity);
    }

    public void Launch(Transform playerTarget, Transform boss)
    {
        this.playerTarget = playerTarget;
        target = playerTarget;
        bossTransform = boss;
        elapsed = 0f;
    }

    protected override void OnReflected(bool isParry)
    {
        target = (target == playerTarget) ? bossTransform : playerTarget;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= lifetime)
        {
            Remove();
            return;
        }

        if (target != null)
        {
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }

        transform.Translate(Vector3.forward * Speed * Time.deltaTime);
        SnapToGroundHeight();
    }
}
