using UnityEngine;

public class Stage1BossWave : MonoBehaviour
{
    [SerializeField] private float maxRadius = 15f;
    [SerializeField] private float waveSpeed = 8f;
    [SerializeField] private float waveWidth = 2f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private ParticleSystem ringParticles; // 링 둘레에 방출할 파티클
    [SerializeField] private Transform magicCircleVFX;
    [SerializeField] private float magicCircleScalePerRadius = 0.02367f; // scale / maxRadius 비율
    [SerializeField] private GameObject muzzlePrefab;
    [SerializeField] private GameObject hitPrefab;

    private float currentRadius;
    private bool playerHit;

    void Start()
    {
        if (magicCircleVFX != null)
            magicCircleVFX.localScale = Vector3.one * (maxRadius * magicCircleScalePerRadius);

        SpawnVFX(muzzlePrefab, transform.position);
    }

    void Update()
    {
        currentRadius += waveSpeed * Time.deltaTime;

        if (ringParticles != null)
        {
            var shape = ringParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = currentRadius;
            shape.radiusThickness = 0f; // 둘레에서만 방출
        }

        if (!playerHit)
            DetectPlayer();

        if (currentRadius >= maxRadius)
            Destroy(gameObject);
    }

    private void DetectPlayer()
    {
        float innerRadius = Mathf.Max(0f, currentRadius - waveWidth);

        Collider[] hits = Physics.OverlapSphere(transform.position, currentRadius);
        foreach (var hit in hits)
        {
            GameObject obj = hit.attachedRigidbody ? hit.attachedRigidbody.gameObject : hit.gameObject;
            PlayerStateController playerState = obj.GetComponent<PlayerStateController>();
            if (playerState == null) continue;

            float dist = Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(obj.transform.position.x, 0f, obj.transform.position.z)
            );

            if (dist >= innerRadius)
            {
                playerState.TakeDamage(damage);
                playerHit = true;
                SpawnVFX(hitPrefab, obj.transform.position);
                break;
            }
        }
    }

    private void SpawnVFX(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;
        GameObject vfx = Instantiate(prefab, position, Quaternion.identity);
        ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
        if (ps != null)
            Destroy(vfx, ps.main.duration);
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, currentRadius);
        Gizmos.color = new Color(0f, 0.8f, 1f, 1f);
        Gizmos.DrawWireSphere(transform.position, currentRadius);
        if (currentRadius > waveWidth)
        {
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, currentRadius - waveWidth);
        }
    }
    #endif
}
