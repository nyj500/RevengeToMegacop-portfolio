using MoreMountains.Feedbacks;

using UnityEngine;

/// <summary>
/// 적 피격 시 Feel 기반 시각 피드백을 재생하는 컴포넌트.
/// 머티리얼 플래시(Flicker)로 피격 시각 효과를 제공한다.
/// EnemySpawner가 적 생성 시 동적으로 추가하고 Initialize()를 호출한다.
/// </summary>
public class EnemyHitFeedback : MonoBehaviour
{
    [Header("머티리얼 플래시")]
    [SerializeField] private float flickerDuration = 0.2f;
    [SerializeField] private float flickerPeriod = 0.04f;
    [SerializeField] private Color flickerColor = Color.white;

    private MMF_Player hitPlayer;

    /// <summary>
    /// 피드백을 초기화하고 적의 OnHit 이벤트에 구독한다.
    /// </summary>
    public void Initialize(Enemy enemy)
    {
        if (enemy != null)
        {
            enemy.OnHit += OnHit;
        }
        SetupFeedbackPlayer();
    }

    private void SetupFeedbackPlayer()
    {
        GameObject feedbackObject = new GameObject("HitFeedbacks");
        feedbackObject.transform.SetParent(transform, false);
        hitPlayer = feedbackObject.AddComponent<MMF_Player>();
        hitPlayer.InitializationMode = MMFeedbacks.InitializationModes.Script;
        hitPlayer.CanPlayWhileAlreadyPlaying = true;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer.sharedMaterial == null || !targetRenderer.sharedMaterial.HasProperty("_BaseColor"))
                continue;

            MMF_Flicker flicker = hitPlayer.AddFeedback(typeof(MMF_Flicker)) as MMF_Flicker;
            flicker.BoundRenderer = targetRenderer;
            flicker.FlickerDuration = flickerDuration;
            flicker.FlickerPeriod = flickerPeriod;
            flicker.FlickerColor = flickerColor;
            // URP 쉐이더는 _Color 대신 _BaseColor 사용. PropertyName 모드로 직접 지정
            flicker.Mode = MMF_Flicker.Modes.PropertyName;
            flicker.PropertyName = "_BaseColor";
            flicker.UseMaterialPropertyBlocks = true;
        }

        hitPlayer.Initialization();
    }

    private void OnHit(Enemy enemy)
    {
        if (hitPlayer != null)
        {
            hitPlayer.PlayFeedbacks(transform.position);
        }
    }
}
