using UnityEngine;
using UnityEngine.InputSystem;

public class OrbShooter : MonoBehaviour
{
    public ChainController chain;

    [Header("Hit Settings (Zuma-style)")]
    public float hitRadius = 0.8f; // tune: roughly ball diameter * 0.9

    [Header("Preview")]
    public Transform previewSocket;
    private GameObject previewInstance;
    public int currentColorId = -1;

    // Mouse
    private Vector3 mouseWorldPos;
    private bool hasMousePos;

    // Path projection (still useful for debugging)
    private float mouseDistOnPath;
    private Vector3 mouseClosestPoint;

    // Debug gizmos
    private int debugHitIndex = -1;
    private float debugInsertDist;
    private Vector3 debugInsertWorldPos;

    void Start()
    {
        RollNextBall();
    }

    void Update()
    {
        if (!chain || !chain.path) return;

        GetMousePos();
        if (!hasMousePos) return;

        // Keep this for debug (not for choosing hit ball)
        mouseDistOnPath = chain.path.GetClosestDistanceOnPath(mouseWorldPos, out mouseClosestPoint);

        // Choose which ball you're "hitting" in world space (Zuma rule)
        debugHitIndex = FindHitBallIndex(mouseWorldPos, hitRadius);

        // Compute planned insert spot for gizmos
        ComputeDebugInsertFromHit();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (debugHitIndex == -1)
            {
                Debug.Log("No hit: click closer to the chain.");
                return;
            }

            chain.InsertBallAtHitIndex(debugHitIndex, mouseWorldPos, currentColorId);
            RollNextBall();
        }

        this.transform.rotation = Quaternion.LookRotation(Vector3.forward, mouseWorldPos - this.transform.position);
    }

    void RollNextBall()
    {
        if (!chain || chain.ballPrefabs == null || chain.ballPrefabs.Count == 0)
        {
            Debug.LogError("OrbShooter: chain or ballPrefabs missing.");
            return;
        }

        if (chain.TryGetOnlyColor(out int only))
            currentColorId = only;
        else
            currentColorId = Random.Range(0, chain.ballPrefabs.Count);

        // remove old preview
        if (previewInstance) Destroy(previewInstance);

        if (!previewSocket)
        {
            Debug.LogWarning("OrbShooter: previewSocket not assigned.");
            return;
        }

        // spawn new preview as a child of socket
        GameObject prefab = chain.ballPrefabs[currentColorId];
        previewInstance = Instantiate(prefab, previewSocket);

        // lock it perfectly to the socket
        previewInstance.transform.localPosition = Vector3.zero;
        previewInstance.transform.localRotation = Quaternion.identity;
        previewInstance.transform.localScale = Vector3.one;

        // make sure it doesn't mess with physics
        foreach (var col in previewInstance.GetComponentsInChildren<Collider>())
            col.enabled = false;

        var rb = previewInstance.GetComponentInChildren<Rigidbody>();
        if (rb) rb.isKinematic = true;
    }

    int FindHitBallIndex(Vector3 worldPos, float radius)
    {
        if (chain.balls == null || chain.balls.Count == 0) return -1;

        int bestIndex = -1;
        float bestSqr = radius * radius;

        for (int i = 0; i < chain.balls.Count; i++)
        {
            var b = chain.balls[i];

            // Ignore hidden balls (dist < 0)
            if (b.rend != null && !b.rend.enabled) continue;

            float sqr = (worldPos - b.tr.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    void ComputeDebugInsertFromHit()
    {
        debugInsertDist = 0f;
        debugInsertWorldPos = Vector3.zero;

        if (debugHitIndex < 0) return;
        if (debugHitIndex >= chain.balls.Count) return;

        float baseDist = chain.balls[debugHitIndex].dist;

        float distBefore = Mathf.Clamp(baseDist + chain.spacing, 0f, chain.path.TotalLength);
        float distAfter = Mathf.Clamp(baseDist - chain.spacing, 0f, chain.path.TotalLength);

        Vector3 posBefore = chain.path.GetPos(distBefore);
        Vector3 posAfter = chain.path.GetPos(distAfter);

        // Pick the closer of the two valid slots around the hit ball
        bool insertBefore = (mouseWorldPos - posBefore).sqrMagnitude <= (mouseWorldPos - posAfter).sqrMagnitude;

        debugInsertDist = insertBefore ? distBefore : distAfter;
        debugInsertWorldPos = chain.path.GetPos(debugInsertDist);
    }

    void GetMousePos()
    {
        var cam = Camera.main;
        if (!cam) { hasMousePos = false; return; }

        Vector2 screenPos = (Mouse.current != null)
            ? Mouse.current.position.ReadValue()
            : Pointer.current.position.ReadValue();

        Ray ray = cam.ScreenPointToRay(screenPos);

        // Match your game plane (you used z-plane)
        float zPlane = chain ? chain.transform.position.z : 0f;
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, zPlane));

        if (plane.Raycast(ray, out float enter))
        {
            mouseWorldPos = ray.GetPoint(enter);
            hasMousePos = true;
        }
        else
        {
            hasMousePos = false;
        }
    }

    void OnDrawGizmos()
    {
        if (!hasMousePos) return;

        // Mouse position
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(mouseWorldPos, 0.20f);

        // Closest point on path (debug only)
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(mouseClosestPoint, 0.22f);
        Gizmos.DrawLine(mouseWorldPos, mouseClosestPoint);

        if (!chain || chain.balls == null || chain.balls.Count == 0) return;

        // Hit radius visualization
        Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(mouseWorldPos, hitRadius);

        // Hit ball
        if (debugHitIndex >= 0 && debugHitIndex < chain.balls.Count)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(chain.balls[debugHitIndex].tr.position, 0.24f);

            // Planned insert spot (slot before/after)
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(debugInsertWorldPos, 0.26f);
            Gizmos.DrawLine(chain.balls[debugHitIndex].tr.position, debugInsertWorldPos);
        }
    }
}
