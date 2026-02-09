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
    public float normalSpeed = 2.5f;
    public float spawnBoostSpeed = 10f;
    public float speedBlend = 6f;
    private float speed;

    [Header("Chain Reactions")]
    public float gapEps = 0.02f;                 // tolerance for "touching"
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

    [Header("Stars")]
    public int star1Score = 500;
    public int star2Score = 1500;
    public int star3Score = 3000;

    public int GetStars()
    {
        if (score >= star3Score) return 3;
        if (score >= star2Score) return 2;
        if (score >= star1Score) return 1;
        return 0;
    }

    private bool debugHasMatch;
    private int debugMatchStart, debugMatchEnd;

    public System.Action OnLevelWon;
    public System.Action OnLevelLost;

    bool levelEnded;

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
        levelEnded = false;
        comboLevel = 0;
        debugHasMatch = false;

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
            // ignore hidden balls
            if (balls[i].rend != null && !balls[i].rend.enabled) continue;

            int c = balls[i].colorId;
            if (first == -1) first = c;
            else if (c != first) return false;
        }

        if (first == -1) return false;
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

        if (gapPrev.Count != balls.Count - 1)
            RebuildGapPrev();

        for (int i = 0; i < balls.Count - 1; i++)
        {
            float gap = balls[i].dist - (balls[i + 1].dist + spacing);
            bool isGappedNow = gap > gapEps;

            bool wasGapped = gapPrev[i];
            bool justClosed = wasGapped && !isGappedNow;

            gapPrev[i] = isGappedNow;

            if (!justClosed) continue;

            if (balls[i].colorId != balls[i + 1].colorId) continue;

            if (TryGetMatchAtIndex(i, out var match))
            {
                debugHasMatch = true;
                debugMatchStart = match.start;
                debugMatchEnd = match.end;

                comboLevel++; // reaction step

                RemoveRange(match.start, match.end);
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

    // Called by OrbShooter when you hit a ball
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

        comboLevel = 0; // player-triggered pop starts combo chain

        ApplyVisuals();

        if (TryGetMatchAtIndex(insertIndex, out var match))
        {
            debugHasMatch = true;
            debugMatchStart = match.start;
            debugMatchEnd = match.end;

            RemoveRange(match.start, match.end);
            ApplyVisuals();

            chainReactionArmed = true;
            RebuildGapPrev();
        }
        else
        {
            debugHasMatch = false;
        }

        headDist = balls.Count > 0 ? balls[0].dist : headDist;
    }

    void ResolveSpacingLocal(int pivot)
    {
        for (int i = pivot + 1; i < balls.Count; i++)
        {
            float maxDist = balls[i - 1].dist - spacing;
            if (balls[i].dist > maxDist)
                balls[i].dist = maxDist;
        }

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

            if (b.tr != null)
            {
                var hit = b.tr.GetComponentInChildren<ChainBallHit>();
                if (hit != null) hit.index = i;
            }
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

        int mult = 1 + comboLevel * comboStep;
        AddScore(removed * pointsPerOrb * mult);

        if (!levelEnded && (balls == null || balls.Count == 0))
        {
            levelEnded = true;
            OnLevelWon?.Invoke();
        }

        return removed;
    }

    void Update()
    {
        if (levelEnded) return;
        if (balls == null || balls.Count == 0) return;

        float dt = Time.deltaTime;

        int tail = balls.Count - 1;

        bool stillSpawning = balls[tail].dist < 0f;
        float targetSpeed = stillSpawning ? spawnBoostSpeed : normalSpeed;
        speed = Mathf.Lerp(speed, targetSpeed, 1f - Mathf.Exp(-speedBlend * dt));

        balls[tail].dist += speed * dt;

        for (int i = tail - 1; i >= 0; i--)
        {
            float minDist = balls[i + 1].dist + spacing;
            if (balls[i].dist < minDist)
                balls[i].dist = minDist;
        }

        if (balls[0].dist >= path.TotalLength - endPadding)
        {
            Debug.Log("Reached end of path!");

            if (loopForTesting)
            {
                float shift = balls[0].dist;
                for (int i = 0; i < balls.Count; i++)
                    balls[i].dist -= shift;

                RebuildGapPrev();
                chainReactionArmed = false;
                comboLevel = 0;
            }
            else
            {
                if (!levelEnded)
                {
                    levelEnded = true;
                    OnLevelLost?.Invoke();
                }
                enabled = false;
                return;
            }
        }

        if (chainReactionArmed)
        {
            bool removed = TryChainReaction();

            // ✅ FIXED: braces so combo reset only happens when disarming
            if (!removed && !AnyGapNow())
            {
                chainReactionArmed = false;
                comboLevel = 0;
            }
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