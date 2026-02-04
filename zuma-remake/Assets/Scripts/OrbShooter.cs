using UnityEngine;
using static UnityEngine.Rendering.HableCurve;
using UnityEngine.InputSystem;

public class OrbShooter : MonoBehaviour
{
    public PathSystem path;
    public ChainController chain;

    private float mouseDistOnPath;
    private Vector3 mouseClosestPoint;

    private Vector3 mouseWorldPos;
    private bool hasMousePos;

    private int debugNearestIndex = -1;
    private float debugInsertDist;
    private Vector3 debugInsertWorldPos;

    void Update()
    {
        GetMousePos();
        LoopThroughBalls();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            chain.InsertBall(debugInsertDist);
            Debug.Log("Inserted ball at planned dist: " + debugInsertDist);
        }
    }

    public void LoopThroughBalls()
    {
        if (chain == null || chain.balls == null || chain.balls.Count == 0) return;

        int nearestIndex = 0;
        float best = float.PositiveInfinity;

        for (int i = 0; i < chain.balls.Count; i++)
        {
            float d = Mathf.Abs(chain.balls[i].dist - mouseDistOnPath);
            if (d < best)
            {
                best = d;
                nearestIndex = i;
            }
        }

        debugInsertDist = mouseDistOnPath;

        if (mouseDistOnPath > chain.balls[nearestIndex].dist)
            debugInsertDist = chain.balls[nearestIndex].dist + chain.spacing;
        else
            debugInsertDist = chain.balls[nearestIndex].dist - chain.spacing;

        debugNearestIndex = nearestIndex;
        debugInsertWorldPos = chain.path.GetPos(debugInsertDist);
    }


    public void GetMousePos()
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

        if (debugNearestIndex < 0) return;

        // Nearest ball
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(
            chain.balls[debugNearestIndex].tr.position,
            0.25f
        );

        // Insert position
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(debugInsertWorldPos, 0.3f);

        // Connection line
        Gizmos.DrawLine(
            chain.balls[debugNearestIndex].tr.position,
            debugInsertWorldPos
        );
    }
}
