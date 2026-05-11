/// <summary>
/// 피격 처리 인터페이스. 총알에 맞았을 때 호출된다.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 피격 처리. 총알의 데미지를 받아 HP 감소, 사망 판정 등을 수행한다.
    /// <para>
    /// <b>[계약]</b> 구현체는 총알을 소모하려면 반드시 <c>bullet.Remove()</c>를 직접 호출해야 한다.
    /// 총알을 반사(패리/가드)하는 경우에는 <c>Remove()</c>를 호출하지 않아 총알을 유지할 수 있다.
    /// </para>
    /// </summary>
    void Hit(Bullet bullet);
}
