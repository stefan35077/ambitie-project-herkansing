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
    public float startHeadDist = 3f;

    [Header("Speed")]
    public float normalSpeed;     
    public float spawnBoostSpeed;   
    public float speedBlend;
    private float speed;

    [Header("Chain Reactions")]
    public float gapEps = 0.02f;          // tolerance for "touching"
    private bool chainReactionArmed = false;
    private readonly List<bool> gapPrev = new(); // tracks which gaps existed last frame

    [Tooltip("If 0, spacing is auto-calculated from prefab diameter.")]
    public float spacing = 0f;

    [Header("End of Path")]
    public float endPadding = 0.3f;
    public bool loopForTesting = false;

    float headDist;

    [Header("Match Rules")]
    public int matchCount = 3;

    [Header("Score")]
    public int score;
    public int pointsPerOrb = 10;

    [Tooltip("Extra multiplier per chain reaction. 0 = no combo system.")]
    public int comboStep = 1;

    private int comboLevel = 0;

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

    public System.Action<int> OnScoreChanged;
    void AddScore(int amount)
    {
        score += amount;
        OnScoreChanged?.Invoke(score);
    }

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

        speed = spawnBoostSpeed;

        ApplyVisuals();

        RebuildGapPrev();
        chainReactionArmed = false;
    }

    public bool TryGetOnlyColor(out int onlyColorId)
    {
        onlyColorId = -1;
        if (balls == null || balls.Count == 0) return false;

        int first = -1;

        for (int i = 0; i < balls.Count; i++)
        {
            // ignore hidden balls (if you ever have dist < 0)
            if (balls[i].rend != null && !balls[i].rend.enabled) continue;

            int c = balls[i].colorId;
            if (first == -1) first = c;
            else if (c != first) return false; // found a second color -> not only one
        }

        if (first == -1) return false; // all were hidden
        onlyColorId = first;
        return true;
    }
    bool TryGetMatchAtIndex(int index, out MatchRange range)
    {
        range = default;
        if (balls == null || balls.Count == 0) return false;
        if (index < 0 || index >= balls.Count) return false;

        int color = balls[index].colorId;

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

    void RebuildGapPrev()
    {
        gapPrev.Clear();
        if (balls == null) return;

        for (int i = 0; i < balls.Count - 1; i++)
        {
            float gap = balls[i].dist - (balls[i + 1].dist + spacing);
            gapPrev.Add(gap > gapEps);
        }
    }

    bool AnyGapNow()
    {
        for (int i = 0; i < balls.Count - 1; i++)
        {
            float gap = balls[i].dist - (balls[i + 1].dist + spacing);
            if (gap > gapEps) return true;
        }
        return false;
    }

    bool TryChainReaction()
    {
        if (balls == null || balls.Count < matchCount) return false;

        // Ensure gapPrev length matches current chain
        if (gapPrev.Count != balls.Count - 1)
            RebuildGapPrev();

        for (int i = 0; i < balls.Count - 1; i++)
        {
            float gap = balls[i].dist - (balls[i + 1].dist + spacing);
            bool isGappedNow = gap > gapEps;

            bool wasGapped = gapPrev[i];
            bool justClosed = wasGapped && !isGappedNow;

            // update tracking for next frame
            gapPrev[i] = isGappedNow;

            if (!justClosed) continue;

            // Gap just closed between i and i+1. Check boundary match
            if (balls[i].colorId != balls[i + 1].colorId) continue;

            if (TryGetMatchAtIndex(i, out var match))
            {
                // optional debug outline
                debugHasMatch = true;
                debugMatchStart = match.start;
                debugMatchEnd = match.end;

                comboLevel++;

                int removed = RemoveRange(match.start, match.end);
                ApplyVisuals();
                RebuildGapPrev();
                return true;
            }
        }

        return false;
    }


    GameObject PickBallPrefab(out int colorId)
    {
        colorId = Random.Range(0, ballPrefabs.Count);
        return ballPrefabs[colorId];
    }

    // Called by OrbShooter when you "hit" a ball (index decided in OrbShooter)
    public void InsertBallAtHitIndex(int hitIndex, Vector3 worldAimPos, int colorId)
    {
        if (balls == null || balls.Count == 0) return;
        if (hitIndex < 0 || hitIndex >= balls.Count) return;
        if (ballPrefabs == null || ballPrefabs.Count == 0) return;

        colorId = Mathf.Clamp(colorId, 0, ballPrefabs.Count - 1);

        float baseDist = balls[hitIndex].dist;

        float distBefore = Mathf.Clamp(baseDist + spacing, 0f, path.TotalLength);
        float distAfter = Mathf.Clamp(baseDist - spacing, 0f, path.TotalLength);

        Vector3 posBefore = path.GetPos(distBefore);
        Vector3 posAfter = path.GetPos(distAfter);

        bool insertBefore = (worldAimPos - posBefore).sqrMagnitude <= (worldAimPos - posAfter).sqrMagnitude;

        float insertDist = insertBefore ? distBefore : distAfter;
        int insertIndex = insertBefore ? hitIndex : hitIndex + 1;
        insertIndex = Mathf.Clamp(insertIndex, 0, balls.Count);

        // Spawn chosen color prefab
        GameObject go = Instantiate(ballPrefabs[colorId], transform);
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

        comboLevel = 0;

        // MATCH CHECK + REMOVE
        if (TryGetMatchAtIndex(insertIndex, out var match))
        {
            debugHasMatch = true;
            debugMatchStart = match.start;
            debugMatchEnd = match.end;

            RemoveRange(match.start, match.end);
            ApplyVisuals();

            // ARM chain reactions (gap will close over time)
            chainReactionArmed = true;
            RebuildGapPrev();
        }
        else
        {
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

    int RemoveRange(int start, int end)
    {
        start = Mathf.Clamp(start, 0, balls.Count - 1);
        end = Mathf.Clamp(end, 0, balls.Count - 1);
        if (start > end) return 0;

        int removed = (end - start + 1);

        for (int i = end; i >= start; i--)
        {
            if (balls[i].tr != null)
                Destroy(balls[i].tr.gameObject);

            balls.RemoveAt(i);
        }

        // score calculation
        int mult = 1 + comboLevel * comboStep;
        AddScore(removed * pointsPerOrb * mult);

        return removed;
    }

    void Update()
    {
        if (balls == null || balls.Count == 0) return;

        float dt = Time.deltaTime;

        // Tail-driven push (Zuma-style)
        int tail = balls.Count - 1;

        // If the tail is still < 0, the chain is still "spawning in" -> go fast.
        bool stillSpawning = balls[tail].dist < 0f;

        // Target speed depending on spawn phase
        float targetSpeed = stillSpawning ? spawnBoostSpeed : normalSpeed;

        // Smooth it so it doesn't snap instantly
        speed = Mathf.Lerp(speed, targetSpeed, 1f - Mathf.Exp(-speedBlend * dt));

        // Apply movement
        balls[tail].dist += speed * dt;

        // push forward only when overlapping (no gap pushing)
        for (int i = tail - 1; i >= 0; i--)
        {
            float minDist = balls[i + 1].dist + spacing; // must be at least spacing ahead of the ball behind it
            if (balls[i].dist < minDist)
                balls[i].dist = minDist;
        }

        // End-of-path check is based on the head reaching the end
        if (balls[0].dist >= path.TotalLength - endPadding)
        {
            Debug.Log("Reached end of path!");

            if (loopForTesting)
            {
                // reset the whole chain by shifting all dists back by head amount
                float shift = balls[0].dist;
                for (int i = 0; i < balls.Count; i++)
                    balls[i].dist -= shift;
            }
            else
            {
                enabled = false;
                return;
            }
        }

        if (chainReactionArmed)
        {
            // Try one reaction per frame (clean + stable)
            bool removed = TryChainReaction();

            // If no gaps exist anymore and nothing removed, disarm
            if (!removed && !AnyGapNow())
                chainReactionArmed = false;
                comboLevel = 0;
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
