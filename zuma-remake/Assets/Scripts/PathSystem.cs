using System.Collections.Generic;
using UnityEngine;

public class PathSystem : MonoBehaviour
{
    [Header("Spline")]
    public Transform[] controlPoints;

    [Tooltip("How many samples per Catmull-Rom segment (higher = smoother, more points)")]
    public int segments = 50;

    [Header("Debug")]
    public bool drawGizmos = true;
    public float gizmoRadius = 0.08f;

    // Baked polyline representation of the spline (used for fast distance-based lookups)
    private readonly List<Vector3> bakedPoints = new();
    // Cumulative distance at each baked point (same length as bakedPoints)
    private readonly List<float> cumLen = new();
    public float TotalLength { get; private set; } // Total path length in world units

    void Awake()
    {
        // Build bakedPoints + cumLen at runtime so GetPos() works immediately
        Bake();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Re-bake in the editor when you tweak points/segments (without entering play mode)
        if (!Application.isPlaying)
            Bake();
    }
#endif

    public void Bake()
    {
        bakedPoints.Clear();
        cumLen.Clear();
        TotalLength = 0f;

        if (controlPoints == null || controlPoints.Length < 4) return;
        if (segments < 2) segments = 2;

        // Sample each Catmull-Rom span and convert it into a dense polyline with a distance table
        for (int i = 0; i < controlPoints.Length - 3; i++)
        {
            // Avoid duplicating the shared boundary point between spans
            int startJ = (i == 0) ? 0 : 1;

            for (int j = startJ; j <= segments; j++)
            {
                float t = j / (float)segments;

                Vector3 p = GetCatmullRomPosition(
                    t,
                    controlPoints[i].position,
                    controlPoints[i + 1].position,
                    controlPoints[i + 2].position,
                    controlPoints[i + 3].position
                );

                // Build cumulative length so we can move at constant world-units/sec later
                if (bakedPoints.Count == 0)
                {
                    bakedPoints.Add(p);
                    cumLen.Add(0f);
                }
                else
                {
                    TotalLength += Vector3.Distance(bakedPoints[^1], p);
                    bakedPoints.Add(p);
                    cumLen.Add(TotalLength);
                }
            }
        }
    }

    public Vector3 GetPos(float distance)
    {
        if (bakedPoints.Count == 0) return transform.position;

        // Distance is in world units along the path
        distance = Mathf.Clamp(distance, 0f, TotalLength);

        // Find the baked segment that contains this distance
        int j = BinarySearchCumLen(distance);
        if (j <= 0) return bakedPoints[0];

        int i = j - 1;

        // Interpolate within the found segment using the cumulative distance table
        float segStart = cumLen[i];
        float segEnd = cumLen[j];
        float segLen = segEnd - segStart;

        float u = (segLen > 0.000001f) ? (distance - segStart) / segLen : 0f;
        return Vector3.Lerp(bakedPoints[i], bakedPoints[j], u);
    }

    int BinarySearchCumLen(float distance)
    {
        // Returns the first index where cumLen[index] >= distance
        int lo = 0;
        int hi = cumLen.Count - 1;

        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (cumLen[mid] < distance) lo = mid + 1;
            else hi = mid;
        }

        return lo;
    }

    Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        // Standard Catmull-Rom spline formula
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        if (bakedPoints.Count == 0)
        {
            if (controlPoints == null || controlPoints.Length < 4) return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < controlPoints.Length - 3; i++)
            {
                for (int j = 0; j <= Mathf.Max(segments, 2); j++)
                {
                    float t = j / (float)Mathf.Max(segments, 2);
                    Vector3 point = GetCatmullRomPosition(
                        t,
                        controlPoints[i].position,
                        controlPoints[i + 1].position,
                        controlPoints[i + 2].position,
                        controlPoints[i + 3].position
                    );

                    Gizmos.DrawSphere(point, gizmoRadius);
                }
            }
            return;
        }

        Gizmos.color = Color.cyan;
        for (int i = 0; i < bakedPoints.Count; i++)
            Gizmos.DrawSphere(bakedPoints[i], gizmoRadius);
    }
}
