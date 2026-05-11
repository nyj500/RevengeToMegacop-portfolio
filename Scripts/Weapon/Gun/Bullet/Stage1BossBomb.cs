using System.Collections.Generic;
using UnityEngine;

public class Stage1BossBomb : Bullet
{
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private float bombSpeed = 8f;
    [SerializeField] private float currentArcHeightRatio = 0.4f; // 거리 대비 호 높이 비율
    [SerializeField] private float minArcHeight = 3f;            // 최소 호 높이 (가까울 때 보장)
    [SerializeField] private float minFlightTime = 1.2f;         // 최소 비행 시간(초)
    [SerializeField] private float fuseTime = 4f;
    [SerializeField] private float explosionRadius = 3f;

    private Vector3 startPos;
    private Vector3 targetPos;
    private float flightTime;
    private float launchDistance;
    private float currentArcHeight;
    private float elapsed;
    private float totalElapsed;
    private bool isLaunched;
    private bool playerDirectlyHit;
    private bool bossDirectlyHit;
    private bool wasReflectedByShield;
    private Vector3 lastForward;

    override protected void OnTriggerEnter(Collider other)
    {
        if (!isLaunched) return;
        base.OnTriggerEnter(other);

        GameObject obj = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
        if (obj.GetComponent<PlayerStateController>() != null)
            playerDirectlyHit = true;
        else if (other.gameObject.GetComponent<Stage1BossShield>() != null)
        {
            wasReflectedByShield = true;
            bossDirectlyHit = true;
        }
        else if (obj.GetComponent<IDamageable>() != null)
            bossDirectlyHit = true;
    }

    public void Launch(Vector3 start, Vector3 target, float overrideLaunchDistance = -1f)
    {
        startPos = start;
        targetPos = target;
        Speed = bombSpeed;
        float distXZ = Vector3.Distance(new Vector3(start.x, 0f, start.z), new Vector3(target.x, 0f, target.z));
        launchDistance = overrideLaunchDistance > 0f ? overrideLaunchDistance : distXZ;
        flightTime = CalculateFlightTime(start, target);
        currentArcHeight = Mathf.Max(launchDistance * currentArcHeightRatio, minArcHeight);
        elapsed = 0f;
        totalElapsed = 0f;
        isLaunched = true;
        playerDirectlyHit = false;
        bossDirectlyHit = false;
        wasReflectedByShield = false;

        Vector3 horizontal = new Vector3(target.x - start.x, 0f, target.z - start.z);
        if (horizontal.sqrMagnitude > 0.01f)
            transform.forward = horizontal.normalized;
        lastForward = transform.forward;

        GetComponentInChildren<BulletVFX>()?.PlayMuzzle();
    }

    private float CalculateFlightTime(Vector3 from, Vector3 to)
    {
        float distance = Vector3.Distance(new Vector3(from.x, 0f, from.z), new Vector3(to.x, 0f, to.z));
        return Mathf.Max(distance / Speed, minFlightTime);
    }

    void Update()
    {
        if (!isLaunched) return;

        elapsed += Time.deltaTime;
        totalElapsed += Time.deltaTime;

        DetectAndApplyReflect();
        ApplyParabolicMovement();

        if (elapsed >= flightTime || totalElapsed >= fuseTime)
            Explode();
    }

    private void DetectAndApplyReflect()
    {
        Vector3 currentForwardH = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 lastForwardH = new Vector3(lastForward.x, 0f, lastForward.z).normalized;
        if (Vector3.Dot(currentForwardH, lastForwardH) < 0.95f)
        {
            startPos = transform.position;
            targetPos = transform.position + currentForwardH * launchDistance;
            targetPos.y = 0f;
            flightTime = Mathf.Max(launchDistance / Speed, minFlightTime);
            elapsed = 0f;
        }
    }

    private void ApplyParabolicMovement()
    {
        float t = Mathf.Clamp01(elapsed / flightTime);
        Vector3 pos = Vector3.Lerp(startPos, targetPos, t);
        pos.y = Mathf.Lerp(startPos.y, targetPos.y, t) + currentArcHeight * Mathf.Sin(Mathf.PI * t);

        Vector3 nextPos = Vector3.Lerp(startPos, targetPos, Mathf.Clamp01((elapsed + Time.deltaTime) / flightTime));
        nextPos.y = Mathf.Lerp(startPos.y, targetPos.y, Mathf.Clamp01((elapsed + Time.deltaTime) / flightTime))
                    + currentArcHeight * Mathf.Sin(Mathf.PI * Mathf.Clamp01((elapsed + Time.deltaTime) / flightTime));

        Vector3 moveDirection = (nextPos - pos);
        if (moveDirection != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(moveDirection);

        transform.position = pos;

        Vector3 horizontalMove = new Vector3(moveDirection.x, 0f, moveDirection.z);
        if (horizontalMove != Vector3.zero)
            lastForward = horizontalMove.normalized;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = new Color(1f, 0.3f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    void OnDisable()
    {
        if (isLaunched && Application.isPlaying) Explode(); // Bullet.Update()의 destroyTime이 fuseTime보다 먼저 만료될 경우 강제 폭발
    }

    private void Explode() // 폭발 데미지 적용(패링 불가) 및 이펙트 재생
    {
        if (!isLaunched) return;
        isLaunched = false;

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        bool playerProcessed = false;
        bool bossProcessed = false;
        HashSet<GameObject> processedMobs = new HashSet<GameObject>();
        foreach (var hit in hits)
        {
            GameObject obj = hit.attachedRigidbody ? hit.attachedRigidbody.gameObject : hit.gameObject;

            if (!playerProcessed)
            {
                PlayerStateController playerState = obj.GetComponent<PlayerStateController>();
                if (playerState != null)
                {
                    if (!playerDirectlyHit)
                        playerState.TakeDamage(Damage);
                    playerProcessed = true;
                    continue;
                }
            }

            if (!bossProcessed)
            {
                Stage1Boss stage1Boss = obj.GetComponent<Stage1Boss>();
                if (stage1Boss != null)
                {
                    if (!bossDirectlyHit)
                        stage1Boss.Hit(this);
                    if (!wasReflectedByShield)
                        stage1Boss.EnterStun();
                    bossProcessed = true;
                    continue;
                }
            }

            if (!processedMobs.Contains(obj))
            {
                Enemy enemy = obj.GetComponent<Enemy>();
                if (enemy != null && !(enemy is Stage1Boss))
                {
                    enemy.Hit(this);
                    processedMobs.Add(obj);
                }
            }
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAtPoint(explosionSound, transform.position);

        if (explosionPrefab != null)
        {
            GameObject explosionVFX = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            ParticleSystem explosionPS = explosionVFX.GetComponent<ParticleSystem>();
            if (explosionPS != null)
                Destroy(explosionVFX, explosionPS.main.duration);
        }
        GetComponentInChildren<BulletVFX>()?.PlayHit(transform.position, Quaternion.identity);

        Remove();
    }
}
