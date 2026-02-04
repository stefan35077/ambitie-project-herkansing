using System.Collections.Generic;
using UnityEngine;

public class ChainController : MonoBehaviour
{
    [Header("Refs")]
    public PathSystem path;

    [Header("Ball Prefabs (one per color)")]
    public List<GameObject> ballPrefabs = new();

    [Header("Chain")]
    public int ballCount = 20;
    public float speed = 3f;
    public float startHeadDist = 3f;
    public float catchUpSpeed = 3f;

    [Tooltip("If 0, spacing is auto-calculated from prefab diameter.")]
    public float spacing = 0f;

    [Header("End of Path")]
    public float endPadding = 0.3f;
    public bool loopForTesting = false;

    float headDist;

    [Header("Match Rules")]
    public int matchCount = 3;


    private bool debugHasMatch;
    private int debugMatchStart, debugMatchEnd;

    struct MatchRange
    {
        public int start;
        public int end;
        public int Count => end - start + 1;
    }

    public class Ball
    {
        public Transform tr;
        public float dist;
        public Renderer rend;
        public int colorId; // index in ballPrefabs
    }

    [Header("Runtime Chain (read-only)")]
    public List<Ball> balls = new();

    void Start()
    {
        if (!path)
        {
            Debug.LogError("ChainController: 'path' is not assigned.");
            enabled = false;
            return;
        }

        if (ballPrefabs == null || ballPrefabs.Count == 0)
        {
            Debug.LogError("ChainController: ballPrefabs list is empty.");
            enabled = false;
            return;
        }

        // Auto spacing once from the first prefab (all prefabs should be same size)
        if (spacing <= 0f)
        {
            var r0 = ballPrefabs[0].GetComponentInChildren<Renderer>();
            if (r0 != null)
            {
                float diameter = Mathf.Max(r0.bounds.size.x, r0.bounds.size.z);
                spacing = diameter * 0.98f;
            }
            else
            {
                spacing = 0.6f;
                Debug.LogWarning("ChainController: Could not auto-calc spacing. Using 0.6.");
            }
        }

        headDist = startHeadDist;

        // Spawn initial chain
        balls.Clear();
        for (int i = 0; i < ballCount; i++)
        {
            int colorId;
            GameObject prefab = PickBallPrefab(out colorId);

            GameObject go = Instantiate(prefab, transform);
            Renderer r = go.GetComponentInChildren<Renderer>();

            Ball b = new Ball
            {
                tr = go.transform,
                dist = headDist - i * spacing,
                rend = r,
                colorId = colorId
            };

            balls.Add(b);
        }

        ApplyVisuals();
    }

    bool TryGetMatchAtIndex(int index, out MatchRange range)
    {
        range = default;

        if (balls == null || balls.Count == 0) return false;
        if (index < 0 || index >= balls.Count) return false;

        int color = balls[index].colorId;
        if (color < 0) return false;

        int left = index;
        while (left - 1 >= 0 && balls[left - 1].colorId == color)
            left--;

        int right = index;
        while (right + 1 < balls.Count && balls[right + 1].colorId == color)
            right++;

        range.start = left;
        range.end = right;

        return range.Count >= matchCount;
    }

    GameObject PickBallPrefab(out int colorId)
    {
        colorId = Random.Range(0, ballPrefabs.Count);
        return ballPrefabs[colorId];
    }

    // Called by OrbShooter when you "hit" a ball (index decided in OrbShooter)
    public void InsertBallAtHitIndex(int hitIndex, Vector3 worldAimPos)
    {
        if (balls == null || balls.Count == 0) return;
        if (hitIndex < 0 || hitIndex >= balls.Count) return;

        float baseDist = balls[hitIndex].dist;

        // Two valid slots around the hit ball
        float distBefore = Mathf.Clamp(baseDist + spacing, 0f, path.TotalLength); // toward head
        float distAfter = Mathf.Clamp(baseDist - spacing, 0f, path.TotalLength); // toward tail

        Vector3 posBefore = path.GetPos(distBefore);
        Vector3 posAfter = path.GetPos(distAfter);

        // Pick the closer slot to the click/projectile position
        bool insertBefore = (worldAimPos - posBefore).sqrMagnitude <= (worldAimPos - posAfter).sqrMagnitude;

        float insertDist = insertBefore ? distBefore : distAfter;
        int insertIndex = insertBefore ? hitIndex : hitIndex + 1;
        insertIndex = Mathf.Clamp(insertIndex, 0, balls.Count);

        // Spawn new ball from prefab list
        int colorId;
        GameObject prefab = PickBallPrefab(out colorId);

        GameObject go = Instantiate(prefab, transform);
        Renderer r = go.GetComponentInChildren<Renderer>();

        Ball newBall = new Ball
        {
            tr = go.transform,
            dist = insertDist,
            rend = r,
            colorId = colorId
        };

        balls.Insert(insertIndex, newBall);

        ResolveSpacingLocal(insertIndex);

        headDist = balls[0].dist;
        ApplyVisuals();

        // MATCH CHECK
        if (TryGetMatchAtIndex(insertIndex, out var match))
        {
            Debug.Log($"MATCH! color={balls[insertIndex].colorId} range={match.start}-{match.end} count={match.Count}");
            debugHasMatch = true;
            debugMatchStart = match.start;
            debugMatchEnd = match.end;
        }
        else
        {
            Debug.Log("No match.");
            debugHasMatch = false;
        }

        // Keep headDist aligned with the actual head after insertion
        headDist = balls[0].dist;

        ApplyVisuals();
    }

    void ResolveSpacingLocal(int pivot)
    {
        // Push toward tail (ensure no overlap)
        for (int i = pivot + 1; i < balls.Count; i++)
        {
            float maxDist = balls[i - 1].dist - spacing;
            if (balls[i].dist > maxDist)
                balls[i].dist = maxDist;
        }

        // Push toward head (ensure no overlap)
        for (int i = pivot - 1; i >= 0; i--)
        {
            float minDist = balls[i + 1].dist + spacing;
            if (balls[i].dist < minDist)
                balls[i].dist = minDist;
        }
    }

    void ApplyVisuals()
    {
        for (int i = 0; i < balls.Count; i++)
        {
            Ball b = balls[i];

            if (b.rend != null)
                b.rend.enabled = (b.dist >= 0f);

            if (b.dist >= 0f)
                b.tr.position = path.GetPos(b.dist);
        }
    }

    void Update()
    {
        if (balls == null || balls.Count == 0) return;

        float dt = Time.deltaTime;

        // Sync then advance head
        headDist = balls[0].dist;
        headDist += speed * dt;
        balls[0].dist = headDist;

        // End of path
        if (headDist >= path.TotalLength - endPadding)
        {
            Debug.Log("Reached end of path!");

            if (loopForTesting)
            {
                headDist = 0f;
                balls[0].dist = headDist;
            }
            else
            {
                enabled = false;
                return;
            }
        }

        // Catch-up drift
        for (int i = 1; i < balls.Count; i++)
            balls[i].dist += catchUpSpeed * dt;

        // Spacing constraint (head -> tail)
        for (int i = 1; i < balls.Count; i++)
        {
            float maxDist = balls[i - 1].dist - spacing;
            if (balls[i].dist > maxDist)
                balls[i].dist = maxDist;
        }

        ApplyVisuals();
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (!debugHasMatch) return;
        if (balls == null) return;

        Gizmos.color = Color.magenta;
        for (int i = debugMatchStart; i <= debugMatchEnd; i++)
        {
            if (i < 0 || i >= balls.Count) continue;
            if (balls[i].tr == null) continue;
            Gizmos.DrawWireSphere(balls[i].tr.position, 0.35f);
        }
    }
}
