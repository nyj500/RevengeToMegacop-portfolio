using System.Collections;

using UnityEngine;

/// <summary>
/// Stage2Boss 애니메이션 제어.
/// Stage2Boss 루트에 붙이고, Animator는 자식(Character_Geisha_Black)에서 자동으로 찾는다.
/// Stage2Boss.cs와 각 공격 패턴에서 호출한다.
/// </summary>
public class Stage2BossAnimator : MonoBehaviour
{
    private Animator animator;

    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int MoveDirectionHash = Animator.StringToHash("MoveDirection");

    private void Awake()
    {
        // GetComponentInChildren은 자기 자신도 포함하므로, 자식에서만 탐색
        foreach (Animator a in GetComponentsInChildren<Animator>())
        {
            if (a.gameObject != gameObject)
            {
                animator = a;
                break;
            }
        }

        if (animator == null)
            Debug.LogWarning("[Stage2BossAnimator] 자식에서 Animator를 찾을 수 없습니다.");
        else
            Debug.Log($"[Stage2BossAnimator] Animator 연결됨: {animator.gameObject.name}, Controller: {animator.runtimeAnimatorController?.name}");
    }

    /// <summary>공격 패턴 발사 시 호출.</summary>
    public void PlayAttack()
    {
        Debug.Log("[Stage2BossAnimator] PlayAttack 호출됨");
        animator?.SetTrigger(AttackHash);
    }

    /// <summary>반사탄 피격 또는 처형 시 호출.</summary>
    public void PlayHit()
    {
        Debug.Log("[Stage2BossAnimator] PlayHit 호출됨");
        animator?.SetTrigger(HitHash);
    }

    /// <summary>보스 사망 시 호출.</summary>
    public void PlayDie() => animator?.SetTrigger(DieHash);

    /// <summary>Die 애니메이션 재생이 완료될 때까지 대기한다. 최대 5초 이내 완료되지 않으면 강제 종료한다.</summary>
    public IEnumerator WaitForDieAnimation()
    {
        if (animator == null) yield break;

        float timeout = 5f;
        float elapsed = 0f;

        // Die 상태로 전환될 때까지 대기
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Die"))
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout) yield break;
            yield return null;
        }

        // Die 애니메이션 재생 완료 대기
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout) yield break;
            yield return null;
        }
    }

    /// <summary>스트레이핑 이동 시작/정지 시 호출.</summary>
    public void SetMoving(bool isMoving)
    {
        if (animator != null) animator.SetBool(IsMovingHash, isMoving);
    }

    /// <summary>스트레이핑 방향 설정. -1 = 왼쪽 스트레이핑, +1 = 오른쪽 스트레이핑.</summary>
    public void SetMoveDirection(float direction)
    {
        if (animator != null) animator.SetFloat(MoveDirectionHash, direction);
    }
}
