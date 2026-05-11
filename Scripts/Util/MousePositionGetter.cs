using UnityEngine;
using UnityEngine.InputSystem;

public static class MousePositionGetter
{
    public static Vector3 GetMousePositionInWorld(Vector3 target)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return target;

        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, target.y, 0));

        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);

            hitPoint.y = target.y;

            return hitPoint;
        }

        return target;
    }
}