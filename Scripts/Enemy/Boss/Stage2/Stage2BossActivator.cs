using UnityEngine;

/// 테스트용: 게임 시작 시 자동으로 보스를 활성화
public class Stage2BossActivator : MonoBehaviour
{
    [SerializeField] private Stage2Boss boss;
    [SerializeField] private Transform player;

    void Start()
    {
        if (boss != null && player != null)
        {
            boss.ActivateBoss(player);
        }
    }
}
