using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private float maxOffset = 3.0f;

    [SerializeField] private float mouseInfluence = 2.0f;
    [SerializeField] private CameraShake cameraShake;

    private Camera mainCamera;

    private Vector3 baseOffset;
    private Vector3 currentVelocity;

    private void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("CameraController: MainCamera not found in scene.");
        }

        if (target != null)
        {
            baseOffset = transform.position - target.position;
        }
    }

    private void LateUpdate()
    {
        if (target == null || mainCamera == null) return;

        if (Mouse.current == null) return;
        Vector3 viewportPosition = mainCamera.ScreenToViewportPoint(Mouse.current.position.ReadValue());

        float offsetX = (viewportPosition.x - 0.5f) * mouseInfluence;
        float offsetZ = (viewportPosition.y - 0.5f) * mouseInfluence;

        Vector3 targetOffset = new Vector3(offsetX, 0, offsetZ);

        targetOffset = Vector3.ClampMagnitude(targetOffset, maxOffset);

        Vector3 cameraPosition = target.position + baseOffset + targetOffset;

        transform.position = Vector3.SmoothDamp(transform.position, cameraPosition, ref currentVelocity, smoothTime);

        if (cameraShake != null)
        {
            transform.position += cameraShake.CurrentShakeOffset;
        }
    }
}
