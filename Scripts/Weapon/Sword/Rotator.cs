using UnityEngine;

public class Rotator : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 1f;

    void Update()
    {
        transform.Rotate(Vector3.up, 360f * rotationSpeed * Time.deltaTime);
    }
}
