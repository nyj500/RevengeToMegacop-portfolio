using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Pool;

public class BulletPool : MonoBehaviour
{
    private static BulletPool instance;
    public static BulletPool Instance => instance;

    private readonly Dictionary<GameObject, ObjectPool<Bullet>> pools = new();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public Bullet Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        Bullet bullet = GetOrCreatePool(prefab).Get();
        if (bullet == null)
        {
            Debug.LogError($"BulletPool: prefab '{prefab.name}'에 Bullet 컴포넌트가 없습니다.");
            return null;
        }
        bullet.transform.SetPositionAndRotation(position, rotation);
        bullet.Prepare();
        bullet.gameObject.SetActive(true);
        return bullet;
    }

    public void Release(GameObject prefab, Bullet bullet)
    {
        if (!pools.TryGetValue(prefab, out ObjectPool<Bullet> pool))
        {
            Destroy(bullet.gameObject);
            return;
        }
        pool.Release(bullet);
    }

    private ObjectPool<Bullet> GetOrCreatePool(GameObject prefab)
    {
        if (pools.TryGetValue(prefab, out ObjectPool<Bullet> pool)) return pool;

        pool = new ObjectPool<Bullet>(
            createFunc: () =>
            {
                GameObject bulletObject = Instantiate(prefab, transform);
                if (!bulletObject.TryGetComponent<Bullet>(out Bullet bullet))
                {
                    Debug.LogError($"BulletPool: prefab '{prefab.name}'에 Bullet 컴포넌트가 없습니다.");
                    Destroy(bulletObject);
                    return null;
                }
                bullet.SetPrefab(prefab);
                return bullet;
            },
            actionOnGet: _ => { },
            actionOnRelease: bullet => bullet.gameObject.SetActive(false),
            actionOnDestroy: bullet => Destroy(bullet.gameObject),
            defaultCapacity: 20,
            maxSize: 200
        );
        pools[prefab] = pool;
        return pool;
    }
}
