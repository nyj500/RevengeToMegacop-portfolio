using UnityEngine;

/// <summary>
/// 총알 기반 클래스. 매 프레임 Speed만큼 전진하며 destroyTime 이후 풀로 반환된다.
/// BulletPool.Get() 호출 시 Prepare()가 자동으로 실행되어 상태가 초기화된다.
/// </summary>
public abstract class Bullet : MonoBehaviour
{
    public float Speed { get; set; }
    [field: SerializeField] public float Damage { get; private set; }

    [SerializeField] private float flyHeight = 1f;
    [SerializeField] private LayerMask groundLayer;

    private float destroyTime;
    private bool isReflected = false;
    private bool isReleased = false;
    private GameObject owner;
    private GameObject prefab;

    internal void SetPrefab(GameObject prefab)
    {
        this.prefab = prefab;
    }

    /// <summary>
    /// 풀에서 꺼낼 때 자동 호출된다. Speed, 반사 여부, 소유자 등 상태를 초기화한다.
    /// </summary>
    public void Prepare()
    {
        Speed = 0f;
        isReflected = false;
        isReleased = false;
        owner = null;
        SetDestroyTime();
    }

    /// <summary>
    /// 총알 소유자를 설정한다. 소유자는 OnTriggerEnter 피격 판정에서 제외된다.
    /// </summary>
    public void SetOwner(GameObject owner)
    {
        this.owner = owner;
    }

    /// <summary>
    /// 총알을 반사시킨다. isParry가 true면 마우스 방향으로, false면 ±60° 랜덤 방향으로 튕긴다.
    /// </summary>
    public void Reflect(GameObject owner, bool isParry)
    {
        this.owner = owner;
        isReflected = true;
        Vector3 targetDirection = isParry ? GetParryDirection() : GetRandomDirection();

        transform.forward = new Vector3(targetDirection.x, 0, targetDirection.z);
        SetDestroyTime(isParry ? 3f : 0.3f);
        OnReflected(isParry);
    }

    /// <summary>
    /// [선택 override] 반사 직후 호출된다. 자식 클래스에서 반사 시점 추가 처리를 구현할 수 있다.
    /// </summary>
    protected virtual void OnReflected(bool isParry) { }

    private Vector3 GetParryDirection()
    {
        return (MousePositionGetter.GetMousePositionInWorld(transform.position) - transform.position).normalized;
    }

    private Vector3 GetRandomDirection()
    {
        float randomDegree = Random.Range(-60f, 60f);
        return Quaternion.Euler(0, randomDegree, 0) * GetParryDirection();
    }

    private void SetDestroyTime(float destroyDelay = 3f)
    {
        destroyTime = Time.time + destroyDelay;
    }

    void Update()
    {
        transform.Translate(Vector3.forward * (Speed * Time.deltaTime));
        SnapToGroundHeight();
        if (destroyTime < Time.time) Remove();
    }

    /// <summary>
    /// 총알이 지형 높이를 추적하도록 Y 위치를 조정한다.
    /// groundLayer가 설정된 경우에만 동작한다.
    /// </summary>
    protected void SnapToGroundHeight()
    {
        if (groundLayer == 0) return;
        Vector3 rayOrigin = transform.position + Vector3.up * 10f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20f, groundLayer))
        {
            Vector3 position = transform.position;
            position.y = hit.point.y + flyHeight;
            transform.position = position;
        }
    }

    virtual protected void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        GameObject obj = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
        if (obj == owner) return;

        if (!isReflected && obj.CompareTag("Enemy")) return;

        if (obj.TryGetComponent<IDamageable>(out IDamageable damageable))
            damageable.Hit(this);
    }

    /// <summary>
    /// 총알을 제거하고 풀로 반환한다. 이미 반환된 경우 중복 실행을 방지한다.
    /// </summary>
    public void Remove()
    {
        if (isReleased) return;
        isReleased = true;

        if (prefab == null)
        {
            Destroy(gameObject);
            return;
        }
        BulletPool.Instance.Release(prefab, this);
    }
}
