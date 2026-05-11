using System;
using System.Collections;

using UnityEngine;

/// <summary>
/// 순간이동 패턴. 두 가지 방식으로 발동한다.
///
/// [패턴 시스템 발동] BossEnemy가 일반 패턴으로 실행할 때 플레이어에게서 먼 거리로 이동.
/// [근접 감지 발동]  Update()에서 플레이어가 proximityTriggerDistance 이내로 접근하면
///                  쿨타임(proximityCooldown)이 지났을 때 즉시 원거리로 이동.
///                  처형 사거리(~10m)를 벗어난 거리(기본 11m)를 기준으로 한다.
/// </summary>
public class TeleportPattern : BossPattern
{
    [Header("Teleport Distance")]
    [SerializeField] private float minDistance = 18f;
    [SerializeField] private float maxDistance = 28f;

    [Header("Timing")]
    [SerializeField] private float teleportDelay = 0.3f;
    [SerializeField] private float afterDelay = 1f;

    [Header("Proximity Trigger")]
    [SerializeField] private float proximityTriggerDistance = 11f;
    [SerializeField] private float proximityCooldown = 4f;

    [Header("Direction")]
    [SerializeField] private float directionSpread = 60f;

    [Header("Play Area Bounds")]
    [Tooltip("순간이동 목적지를 이 Collider의 AABB(XZ) 안으로 제한한다. 비어있으면 playAreaLookupName으로 씬에서 자동 탐색.")]
    [SerializeField] private Collider playAreaBounds;
    [Tooltip("프리팹 동적 생성 시 SerializeField가 비면 이 이름의 GameObject를 찾아 Collider를 사용한다.")]
    [SerializeField] private string playAreaLookupName = "PlayAreaBounds";
    [Tooltip("경계에서 얼마나 안쪽으로 당길지 (보스 반경만큼 여유).")]
    [SerializeField] private float boundsPadding = 1f;

    [Header("VFX")]
    [SerializeField] private GameObject disappearVFX;
    [SerializeField] private GameObject appearVFX;
    [SerializeField] private float vfxLifetime = 2f;

    [Header("Sound")]
    [SerializeField] private AudioClip teleportSound;


    private BossEnemy cachedBoss;
    private float lastProximityTeleportTime = float.NegativeInfinity;

    private void Start()
    {
        cachedBoss = GetComponentInParent<BossEnemy>();

        if (playAreaBounds == null && !string.IsNullOrEmpty(playAreaLookupName))
        {
            GameObject go = GameObject.Find(playAreaLookupName);
            if (go != null) go.TryGetComponent(out playAreaBounds);
        }
    }

    private void Update()
    {
        if (cachedBoss == null || cachedBoss.Target == null) return;
        if (cachedBoss.Hp <= 0f) return;  // 보스 사망 시 순간이동 금지
        if (Time.time < lastProximityTeleportTime + proximityCooldown) return;

        float dist = Vector3.Distance(cachedBoss.transform.position, cachedBoss.Target.position);
        if (dist < proximityTriggerDistance)
        {
            lastProximityTeleportTime = Time.time;
            Debug.Log($"[TeleportPattern] 플레이어 범위 안 접근 ({dist:F1}m) — 순간이동 스킬 사용");
            TeleportTo(cachedBoss, cachedBoss.Target);
        }
    }

    protected override void ExecutePattern(BossEnemy boss, Action onComplete)
    {
        // 패턴 시스템에서 발동하지 않음 — 근접 감지로만 작동
        onComplete?.Invoke();
    }


    /// <summary>보스를 플레이어 기준 원거리 랜덤 위치로 즉시 이동시킨다.</summary>
    private void TeleportTo(BossEnemy boss, Transform target)
    {
        StartCoroutine(TeleportCoroutine(boss, target));
    }
    private IEnumerator TeleportCoroutine(BossEnemy boss, Transform target)
    {
        Stage2Boss stage2Boss = boss as Stage2Boss;

        // 텔레포트 시작 — 이 시점부터 넉백 차단
        stage2Boss?.SetTeleporting(true);

        // 사라지는 이펙트
        if (disappearVFX != null)
        {
            GameObject vfx = Instantiate(disappearVFX, boss.transform.position, Quaternion.identity);
            Destroy(vfx, vfxLifetime);
        }

        yield return new WaitForSeconds(teleportDelay);

        // 이동 — 플레이어에게서 멀어지는 방향 기준으로 ±directionSpread 범위 내 이동
        Vector3 awayDir = boss.transform.position - target.position;
        awayDir.y = 0f;
        if (awayDir.sqrMagnitude < 0.01f) awayDir = Vector3.forward;
        awayDir.Normalize();

        float baseAngle = Mathf.Atan2(awayDir.x, awayDir.z) * Mathf.Rad2Deg;
        float finalAngle = baseAngle + UnityEngine.Random.Range(-directionSpread, directionSpread);
        float distance = UnityEngine.Random.Range(minDistance, maxDistance);
        Vector3 offset = Quaternion.Euler(0f, finalAngle, 0f) * Vector3.forward * distance;
        Vector3 newPos = target.position + offset;
        newPos.y = boss.transform.position.y;

        if (playAreaBounds != null)
        {
            Bounds b = playAreaBounds.bounds;
            newPos.x = Mathf.Clamp(newPos.x, b.min.x + boundsPadding, b.max.x - boundsPadding);
            newPos.z = Mathf.Clamp(newPos.z, b.min.z + boundsPadding, b.max.z - boundsPadding);
        }

        boss.transform.position = newPos;

        // 텔레포트 완료 — 넉백 허용 재개
        stage2Boss?.SetTeleporting(false);

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(teleportSound);

        // 나타나는 이펙트
        if (appearVFX != null)
        {
            GameObject vfx = Instantiate(appearVFX, boss.transform.position, Quaternion.identity);
            Destroy(vfx, vfxLifetime);
        }
    }
}
