using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 플레이어 피격 시 Feel 기반 후처리 피드백을 재생하는 컴포넌트.
/// - 비네트: 직접 Volume 제어 (연속 피격 시 현재 강도에서 보간 상승)
/// - 색수차: MMF_ChromaticAberration_URP (단발성, Feel 샤커 사용)
/// </summary>
public class PlayerHitFeedback : MonoBehaviour
{
    [SerializeField] private PlayerHitController playerHitController;

    [Header("피격 - 비네트")]
    [SerializeField] private float hitVignetteDuration = 0.35f;
    [SerializeField] private float hitVignetteIntensity = 0.65f;
    [SerializeField] private Color hitVignetteColor = Color.red;

    [Header("피격 - 색수차")]
    [SerializeField] private float hitChromaticDuration = 0.2f;
    [SerializeField] private float hitChromaticIntensity = 0.8f;

    private MMF_Player chromaticPlayer;

    private Vignette vignette;
    private float vignetteBaseIntensity;
    private Color vignetteBaseColor;
    private float vignetteDisplayed = 0f;
    private float vignetteDecayTarget = 0f;
    private float vignetteTimer = 0f;

    void Start()
    {
        if (playerHitController != null)
        {
            playerHitController.OnDamaged += OnDamaged;
        }

        SetupChromaticFeedback();
        SetupVignette();
    }

    void OnDestroy()
    {
        if (playerHitController != null)
        {
            playerHitController.OnDamaged -= OnDamaged;
        }

        if (vignette != null)
        {
            vignette.intensity.Override(vignetteBaseIntensity);
            vignette.color.Override(vignetteBaseColor);
        }
    }

    void Update()
    {
        UpdateVignette();
    }

    private void SetupChromaticFeedback()
    {
        GameObject feedbackObject = new GameObject("PlayerHitChromaticFeedbacks");
        feedbackObject.transform.SetParent(transform, false);

        chromaticPlayer = feedbackObject.AddComponent<MMF_Player>();
        chromaticPlayer.InitializationMode = MMFeedbacks.InitializationModes.Script;

        MMF_ChromaticAberration_URP chromatic = chromaticPlayer.AddFeedback(typeof(MMF_ChromaticAberration_URP)) as MMF_ChromaticAberration_URP;
        chromatic.Duration = hitChromaticDuration;
        chromatic.RemapIntensityZero = 0f;
        chromatic.RemapIntensityOne = hitChromaticIntensity;
        chromatic.RelativeIntensity = false;
        chromatic.ResetTargetValuesAfterShake = true;

        chromaticPlayer.Initialization();
    }

    private void SetupVignette()
    {
        Volume volume = FindAnyObjectByType<Volume>();
        if (volume == null) return;

        if (!volume.profile.TryGet(out vignette)) return;

        vignetteBaseIntensity = vignette.intensity.value;
        vignetteBaseColor = vignette.color.value;
    }

    private void UpdateVignette()
    {
        if (vignette == null) return;

        if (vignetteTimer > 0f)
        {
            vignetteTimer -= Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(vignetteTimer / hitVignetteDuration);
            vignetteDecayTarget = hitVignetteIntensity * normalized * normalized;
        }
        else
        {
            vignetteDecayTarget = 0f;
        }

        // 올라갈 땐 빠르게(20), 내려갈 땐 자연스럽게(8)
        float lerpSpeed = vignetteDisplayed < vignetteDecayTarget ? 20f : 8f;
        vignetteDisplayed = Mathf.Lerp(vignetteDisplayed, vignetteDecayTarget, Time.unscaledDeltaTime * lerpSpeed);

        vignette.intensity.Override(vignetteBaseIntensity + vignetteDisplayed);
        vignette.color.Override(vignetteDisplayed > 0.01f ? hitVignetteColor : vignetteBaseColor);
    }

    private void OnDamaged()
    {
        TriggerVignette();

        if (chromaticPlayer != null)
        {
            chromaticPlayer.PlayFeedbacks();
        }
    }

    private void TriggerVignette()
    {
        // 타이머를 전체 지속시간으로 리셋.
        // 다음 Update에서 decayTarget이 피크로 설정되고,
        // displayed가 현재 강도에서 보간 상승하여 연속 피격에도 끊김 없음.
        vignetteTimer = hitVignetteDuration;
    }
}
