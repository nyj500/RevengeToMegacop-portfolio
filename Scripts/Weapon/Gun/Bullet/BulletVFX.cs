using System.Collections.Generic;
using UnityEngine;

public class BulletVFX : MonoBehaviour
{
    [SerializeField] private GameObject muzzlePrefab;
    [SerializeField] private GameObject hitPrefab;
    [SerializeField] private List<GameObject> trails;

    /// <summary>
    /// Muzzle 이펙트를 생성한다. 오브젝트 풀에서 재사용되는 총알은 Start()가 재실행되지 않으므로 직접 호출 필요.
    /// </summary>
    public void PlayMuzzle()
    {
        if (muzzlePrefab == null) return;

        GameObject muzzleVFX = Instantiate(muzzlePrefab, transform.position, transform.rotation);
        ParticleSystem muzzlePS = muzzleVFX.GetComponent<ParticleSystem>();
        if (muzzlePS != null)
            Destroy(muzzleVFX, muzzlePS.main.duration);
        else if (muzzleVFX.transform.childCount > 0)
        {
            ParticleSystem childPS = muzzleVFX.transform.GetChild(0).GetComponent<ParticleSystem>();
            if (childPS != null)
                Destroy(muzzleVFX, childPS.main.duration);
        }
    }

    /// <summary>
    /// 총알이 무언가에 맞았을 때 호출. Hit 이펙트를 생성한다.
    /// </summary>
    public void PlayHit(Vector3 position, Quaternion rotation)
    {
        if (hitPrefab == null) return;

        GameObject hitVFX = Instantiate(hitPrefab, position, rotation);
        ParticleSystem hitPS = hitVFX.GetComponent<ParticleSystem>();
        if (hitPS != null)
            Destroy(hitVFX, hitPS.main.duration);
        else if (hitVFX.transform.childCount > 0)
        {
            ParticleSystem childPS = hitVFX.transform.GetChild(0).GetComponent<ParticleSystem>();
            if (childPS != null)
                Destroy(hitVFX, childPS.main.duration);
        }
    }

    /// <summary>
    /// 총알이 제거되기 직전에 호출. 트레일을 부모에서 분리하고 파티클이 끝난 뒤 Destroy한다.
    /// Remove() 호출 전에 실행해야 한다.
    /// </summary>
    public void DetachTrails()
    {
        foreach (GameObject trail in trails)
        {
            if (trail == null) continue;

            trail.transform.parent = null;
            ParticleSystem trailPS = trail.GetComponent<ParticleSystem>();
            if (trailPS != null)
            {
                trailPS.Stop();
                Destroy(trail, trailPS.main.duration + trailPS.main.startLifetime.constantMax);
            }
        }
    }
}
