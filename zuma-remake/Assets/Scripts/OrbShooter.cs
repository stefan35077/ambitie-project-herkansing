using UnityEngine;
using static UnityEngine.Rendering.HableCurve;
using UnityEngine.InputSystem;

public class OrbShooter : MonoBehaviour
{
    public PathSystem path;

    private float mouseDistOnPath;
    private Vector3 mouseClosestPoint;

    private Vector3 mouseWorldPos;
    private bool hasMousePos;

    void Update()
    {
        var cam = Camera.main;
        if (!cam) return;

        // New Input System: pointer/mouse screen position
        Vector2 screenPos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Pointer.current.position.ReadValue();

        Ray ray = cam.ScreenPointToRay(screenPos);

        // Plane at Y = 0 (change to match your path height if needed)
        Plane plane = new Plane(Vector3.forward, Vector3.zero);

        if (plane.Raycast(ray, out float enter))
        {
            mouseWorldPos = ray.GetPoint(enter);
            hasMousePos = true;
        }
        else
        {
            hasMousePos = false;
        }

        this.gameObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, mouseWorldPos - this.gameObject.transform.position);

        if (hasMousePos && path != null)
        {
            mouseDistOnPath = path.GetClosestDistanceOnPath(mouseWorldPos, out mouseClosestPoint);
        }
    }

    void OnDrawGizmos()
    {
        if (!hasMousePos) return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(mouseWorldPos, 0.25f);
        Gizmos.DrawLine(mouseWorldPos, mouseWorldPos + Vector3.up * 2f);

        if (path != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(mouseClosestPoint, 0.25f);
            Gizmos.DrawLine(mouseWorldPos, mouseClosestPoint);
        }
    }
}
