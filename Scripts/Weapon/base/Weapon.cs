using UnityEngine;

/// <summary>
/// 무기 기반 클래스. useDelay로 사용 빈도를 제한한다.
/// TryUse()를 호출하면 쿨타임이 지났을 때만 Use()를 실행한다.
/// [필수 구현] Use()
/// </summary>
public abstract class Weapon : MonoBehaviour
{
    [SerializeField] private float useDelay;

    [field: SerializeField] public float Range { get; private set; }
    private float previousTime;

    protected virtual void Awake()
    {
        previousTime = -useDelay;
    }

    /// <summary>
    /// 쿨타임을 체크하고 사용 가능하면 Use()를 호출한다.
    /// 외부에서 무기를 사용할 때 이 메서드를 호출한다.
    /// </summary>
    public void TryUse()
    {
        float currentTime = Time.time;
        if (useDelay <= currentTime - previousTime)
        {
            previousTime = currentTime;
            Use();
        }
    }

    /// <summary>
    /// 무기 실제 사용 로직. [필수 구현]
    /// TryUse()의 쿨타임 통과 후 호출된다.
    /// </summary>
    protected abstract void Use();
}
