#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ParryCounterTest 씬 전용 — Q 키를 PlayerHitController.OnParry 발화로 매핑한다.
/// 총알/적 없이 ParryCounterUI의 콤보/랭크/페이드 시각 튜닝을 빠르게 반복하기 위함.
/// </summary>
public class ParryCounterTestTrigger : MonoBehaviour
{
    [SerializeField] private PlayerHitController playerHitController;

    void Update()
    {
        if (playerHitController == null) return;
        if (Keyboard.current == null) return;
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            playerHitController.DebugTriggerParry();
        }
    }
}
#endif
