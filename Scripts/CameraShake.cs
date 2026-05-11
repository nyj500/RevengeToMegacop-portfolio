using UnityEngine;

/// <summary>
/// 카메라 셰이크 오프셋을 계산하는 컴포넌트.
/// Camera 오브젝트에 부착하고, CameraController가 CurrentShakeOffset을 읽어 최종 위치에 더한다.
/// </summary>
public class CameraShake : MonoBehaviour
{
    [SerializeField] private float maximumIntensity = 0.5f;

    private float shakeIntensity;
    private float shakeDuration;
    private float shakeTimeRemaining;

    /// <summary>
    /// CameraController가 LateUpdate에서 읽어가는 현재 프레임의 셰이크 오프셋 (XZ 평면).
    /// </summary>
    public Vector3 CurrentShakeOffset { get; private set; }

    void Update()
    {
        if (shakeTimeRemaining <= 0f)
        {
            CurrentShakeOffset = Vector3.zero;
            return;
        }

        shakeTimeRemaining -= Time.unscaledDeltaTime;

        float normalizedTime = Mathf.Clamp01(shakeTimeRemaining / shakeDuration);
        float decay = normalizedTime * normalizedTime;

        Vector2 randomCircle = Random.insideUnitCircle * shakeIntensity * decay;
        CurrentShakeOffset = new Vector3(randomCircle.x, 0f, randomCircle.y);
    }

    /// <summary>
    /// 셰이크를 시작한다. 현재 남은 셰이크보다 강한 경우에만 덮어쓴다.
    /// </summary>
    /// <param name="intensity">셰이크 강도 (최대 maximumIntensity로 클램프)</param>
    /// <param name="duration">지속 시간(초)</param>
    public void Shake(float intensity, float duration)
    {
        if (duration <= 0f) return;
        intensity = Mathf.Min(intensity, maximumIntensity);

        if (shakeTimeRemaining > 0f)
        {
            float remainingStrength = shakeIntensity * (shakeTimeRemaining / shakeDuration);
            if (intensity <= remainingStrength) return;
        }

        shakeIntensity = intensity;
        shakeDuration = duration;
        shakeTimeRemaining = duration;
    }
}
