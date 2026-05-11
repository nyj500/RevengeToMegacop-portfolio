using UnityEngine;
using UnityEngine.VFX;

public class ArrowBullet : Bullet
{
    [SerializeField] private GameObject impactPrefab;
    [SerializeField] private float impactLifetime = 2f;

    private float enableTime;
    private VisualEffect[] visualEffects;
    private TrailRenderer[] trailRenderers;
    private Transform[] childTransforms;
    private Vector3[] childInitialLocalPositions;
    private Quaternion[] childInitialLocalRotations;

    void Awake()
    {
        visualEffects = GetComponentsInChildren<VisualEffect>(true);
        trailRenderers = GetComponentsInChildren<TrailRenderer>(true);

        // 벤더 데모 스크립트 비활성화 — Start에서 Destroy(gameObject, lifeTime)를 걸어
        // 자식 VFX를 파괴하고, Update에서 이동·OnCollisionEnter에서 Impact를 중복 스폰하는
        // 동작이 풀링과 충돌한다. ArrowLife 파라미터만 보존해 VFX Graph 수명 표시를 유지.
        foreach (ArrowMoevemnt demo in GetComponentsInChildren<ArrowMoevemnt>(true))
        {
            if (demo.VFXGraph != null)
                demo.VFXGraph.SetFloat("ArrowLife", demo.lifeTime);
            demo.enabled = false;
        }

        Rigidbody rootRb = GetComponent<Rigidbody>();
        Collider rootCol = GetComponent<Collider>();
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>(true))
        {
            if (rb == rootRb) continue;
            rb.isKinematic = true;
        }
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            if (col == rootCol) continue;
            col.enabled = false;
        }

        Transform[] all = GetComponentsInChildren<Transform>(true);
        int count = 0;
        for (int i = 0; i < all.Length; i++) if (all[i] != transform) count++;
        childTransforms = new Transform[count];
        childInitialLocalPositions = new Vector3[count];
        childInitialLocalRotations = new Quaternion[count];
        int idx = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == transform) continue;
            childTransforms[idx] = all[i];
            childInitialLocalPositions[idx] = all[i].localPosition;
            childInitialLocalRotations[idx] = all[i].localRotation;
            idx++;
        }
    }

    void OnEnable()
    {
        enableTime = Time.time;

        for (int i = 0; i < childTransforms.Length; i++)
        {
            childTransforms[i].SetLocalPositionAndRotation(
                childInitialLocalPositions[i],
                childInitialLocalRotations[i]);
        }

        for (int i = 0; i < visualEffects.Length; i++)
        {
            visualEffects[i].Reinit();
            visualEffects[i].Play();
        }

        for (int i = 0; i < trailRenderers.Length; i++)
        {
            trailRenderers[i].Clear();
        }
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (Time.time < enableTime + 0.1f) return;
        if (other == null) return;

        GameObject obj = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;

        if (obj.CompareTag("Enemy"))
        {
            // 반사탄이 적(보스/클론)에 맞음 → Hit + 제거
            base.OnTriggerEnter(other);
            SpawnImpact();
            Remove();
        }
        else
        {
            // 플레이어에 닿음 → base가 패링/피격 판단 (패링이면 Reflect, 피격이면 데미지)
            // 여기서 Remove 안 함 → 패링 시 반사돼서 계속 날아감
            base.OnTriggerEnter(other);
            SpawnImpact();
        }
    }
    private void SpawnImpact()
    {
        if (impactPrefab == null) return;
        GameObject impact = Instantiate(impactPrefab, transform.position, transform.rotation);
        Destroy(impact, impactLifetime);
        
    }
}
