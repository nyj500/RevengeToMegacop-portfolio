using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 카메라가 마우스 커서 방향으로 살짝 따라 움직이도록 하는 Cinemachine 확장.
/// 탑다운 시점(XZ 평면) 기준으로 오프셋을 적용한다.
/// CinemachineCamera에 부착해서 사용하며, 파이프라인의 Body 단계 이후 SmoothDamp로 보간한다.
/// </summary>
[AddComponentMenu("Cinemachine/Procedural/Extensions/Cinemachine Mouse Offset")]
[ExecuteAlways]
[SaveDuringPlay]
public class CinemachineMouseOffset : CinemachineExtension
{
    [Tooltip("뷰포트 중앙→가장자리 거리에 곱하는 영향력 (월드 단위)")]
    [SerializeField] private float mouseInfluence = 15f;

    [Tooltip("오프셋 최대 거리 (월드 단위)")]
    [SerializeField] private float maxOffset = 7f;

    [Tooltip("SmoothDamp 시간 (초)")]
    [SerializeField] private float smoothTime = 0.2f;

    private Vector3 currentOffset;
    private Vector3 currentVelocity;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Body) return;
        if (Mouse.current == null) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 viewportPosition = mainCamera.ScreenToViewportPoint(Mouse.current.position.ReadValue());
        float offsetX = (viewportPosition.x - 0.5f) * mouseInfluence;
        float offsetZ = (viewportPosition.y - 0.5f) * mouseInfluence;
        Vector3 targetOffset = Vector3.ClampMagnitude(new Vector3(offsetX, 0f, offsetZ), maxOffset);

        if (deltaTime < 0f)
        {
            currentOffset = targetOffset;
            currentVelocity = Vector3.zero;
        }
        else
        {
            float dt = deltaTime > 0f ? deltaTime : Time.deltaTime;
            currentOffset = Vector3.SmoothDamp(currentOffset, targetOffset, ref currentVelocity, smoothTime, Mathf.Infinity, dt);
        }

        state.PositionCorrection += currentOffset;
    }
}
