using UnityEngine;

public class Stage1BossAnimationBridge : MonoBehaviour
{
    [SerializeField] private Stage1Boss boss;

    public void OnFireAnimationEvent()
    {
        if (boss != null)
            boss.OnFireAnimationEvent();
    }

    public void OnAnimationCompleteEvent()
    {
        if (boss != null)
            boss.OnAnimationCompleteEvent();
    }
}
