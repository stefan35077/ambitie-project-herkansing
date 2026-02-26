using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ChainController : MonoBehaviour
{
    [Header("Setup")]
    public PathSystem path;
    public List<GameObject> ballPrefabs = new List<GameObject>();

    [Header("Chain")]
    [SerializeField] private int ballCount = 20;
    [SerializeField] private float startHeadDist = 3f;

    [Header("Movement")]
    public float spacing = 0f;
    [SerializeField] private float normalSpeed = 2.5f;
    [SerializeField] private float spawnBoostSpeed = 10f;
    [SerializeField] private float speedBlend = 6f;
    [SerializeField] private float endPadding = 0.3f;
    [SerializeField] private bool loopForTesting;

    [Header("Match")]
    [SerializeField] private int matchCount = 3;

    [Header("Score")]
    [SerializeField] private int pointsPerOrb = 10;
    [SerializeField] private int comboStep = 1;

    [Header("Stars")]
    [SerializeField] private int star1Score = 500;
    [SerializeField] private int star2Score = 1500;
    [SerializeField] private int star3Score = 3000;

    [Header("Freeze")]
    [SerializeField] private bool frozen;
    private float frozenTimer;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip matchClip;
    [SerializeField] private float matchPitchJitter = 0.05f;

    [Header("Chain reaction")]
    [SerializeField] private float gapEps = 0.02f;

    public Action<int> OnScoreChanged;
    public Action OnLevelWon;
    public Action OnLevelLost;

    public List<Ball> balls = new List<Ball>();

    private float speed;
    public int score;
    private int comboLevel;
    private bool levelEnded;

    private bool chainReactionArmed;
    private readonly List<bool> gapPrev = new List<bool>();

    private bool debugHasMatch;
    private int debugMatchStart;
    private int debugMatchEnd;

    private struct MatchRange
    {
        public int start;
        public int end;

        public int Count
        {
            get { return end - start + 1; }
        }
    }

    [Serializable]
    public sealed class Ball
    {
        public Transform tr;
        public float dist;
        public Renderer rend;
        public int colorId;
    }

    private void Start()
    {
        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }

        if (spacing <= 0f)
        {
            spacing = CalcSpacingFromPrefab(ballPrefabs[0]);
        }

        levelEnded = false;
        comboLevel = 0;
        debugHasMatch = false;

        BuildStartChain();

        // Start fast so the chain comes in quickly
        speed = spawnBoostSpeed;

        ApplyVisuals();
        RebuildGapPrev();
        chainReactionArmed = false;
    }

    private bool ValidateSetup()
    {
        if (path == null)
        {
            Debug.LogError("ChainController missing path");
            return false;
        }

        if (ballPrefabs == null || ballPrefabs.Count == 0)
        {
            Debug.LogError("ChainController has no ball prefabs");
            return false;
        }

        return true;
    }

    private float CalcSpacingFromPrefab(GameObject prefab)
    {
        if (prefab == null) return 0.6f;

        Renderer r = prefab.GetComponentInChildren<Renderer>();
        if (r == null) return 0.6f;

        float diameter = Mathf.Max(r.bounds.size.x, r.bounds.size.z);
        return diameter * 0.98f;
    }

    private void BuildStartChain()
    {
        balls.Clear();

        for (int i = 0; i < ballCount; i++)
        {
            int colorId;
            GameObject prefab = PickBallPrefab(out colorId);

            GameObject go = Instantiate(prefab, transform);
            Renderer renderer = go.GetComponentInChildren<Renderer>();

            Ball ball = new Ball();
            ball.tr = go.transform;
            ball.rend = renderer;
            ball.colorId = colorId;

            // dist is how far the ball is on the path
            // negative means it is still hidden and coming in
            ball.dist = startHeadDist - (i * spacing);

            balls.Add(ball);
        }
    }

    public void Freeze(float seconds)
    {
        frozen = true;
        frozenTimer = Mathf.Max(frozenTimer, seconds);
    }

    public int GetStars()
    {
        if (score >= star3Score) return 3;
        if (score >= star2Score) return 2;
        if (score >= star1Score) return 1;
        return 0;
    }

    private void AddScore(int amount)
    {
        score += amount;

        Action<int> handler = OnScoreChanged;
        if (handler != null) handler.Invoke(score);
    }

    public void BlackHoleHit(int hitIndex)
    {
        if (balls == null || balls.Count == 0) return;

        int index = Mathf.Clamp(hitIndex, 0, balls.Count - 1);

        RemoveRange(index, index);
        ApplyVisuals();

        ArmChainReaction();
    }

    public void DestroyBallAtHitIndex(int hitIndex)
    {
        if (levelEnded) return;
        if (balls == null || balls.Count == 0) return;
        if (hitIndex < 0 || hitIndex >= balls.Count) return;

        RemoveRange(hitIndex, hitIndex);
        ApplyVisuals();

        ArmChainReaction();
    }

    public void InsertBallAtHitIndex(int hitIndex, Vector3 worldAimPos, int colorId)
    {
        if (levelEnded) return;
        if (balls == null || balls.Count == 0) return;
        if (ballPrefabs == null || ballPrefabs.Count == 0) return;
        if (hitIndex < 0 || hitIndex >= balls.Count) return;

        bool isRainbow = (colorId == -1);

        if (!isRainbow)
        {
            colorId = Mathf.Clamp(colorId, 0, ballPrefabs.Count - 1);
        }

        // We decide before or after based on which side is closer to the aim point
        float baseDist = balls[hitIndex].dist;

        float beforeDist = Mathf.Clamp(baseDist + spacing, 0f, path.TotalLength);
        float afterDist = Mathf.Clamp(baseDist - spacing, 0f, path.TotalLength);

        Vector3 beforePos = path.GetPos(beforeDist);
        Vector3 afterPos = path.GetPos(afterDist);

        float beforeSq = (worldAimPos - beforePos).sqrMagnitude;
        float afterSq = (worldAimPos - afterPos).sqrMagnitude;

        bool insertBefore = beforeSq <= afterSq;

        float insertDist = insertBefore ? beforeDist : afterDist;
        int insertIndex = insertBefore ? hitIndex : (hitIndex + 1);
        insertIndex = Mathf.Clamp(insertIndex, 0, balls.Count);

        // Rainbow means we pick a real color that helps create a match
        int finalColorId = isRainbow ? ChooseRainbowColorForInsert(insertIndex) : colorId;
        finalColorId = Mathf.Clamp(finalColorId, 0, ballPrefabs.Count - 1);

        GameObject go = Instantiate(ballPrefabs[finalColorId], transform);
        Renderer renderer = go.GetComponentInChildren<Renderer>();

        Ball newBall = new Ball();
        newBall.tr = go.transform;
        newBall.rend = renderer;
        newBall.colorId = finalColorId;
        newBall.dist = insertDist;

        balls.Insert(insertIndex, newBall);

        // This pushes balls apart near the insert so nothing overlaps
        ResolveSpacingLocal(insertIndex);

        // Shooting a ball resets the combo chain
        comboLevel = 0;

        ApplyVisuals();

        // If the insert instantly makes a match remove it right away
        MatchRange match;
        if (TryGetMatchAtIndex(insertIndex, out match))
        {
            debugHasMatch = true;
            debugMatchStart = match.start;
            debugMatchEnd = match.end;

            RemoveRange(match.start, match.end);
            ApplyVisuals();

            ArmChainReaction();
        }
        else
        {
            debugHasMatch = false;
        }
    }

    private int ChooseRainbowColorForInsert(int insertIndex)
    {
        // We look left and right and pick the color with the bigger group
        int leftColor = (insertIndex - 1 >= 0) ? balls[insertIndex - 1].colorId : -1;
        int rightColor = (insertIndex < balls.Count) ? balls[insertIndex].colorId : -1;

        if (leftColor < 0) return rightColor;
        if (rightColor < 0) return leftColor;

        int leftRun = CountRun(insertIndex, leftColor);
        int rightRun = CountRun(insertIndex - 1, rightColor);

        if (rightRun > leftRun) return rightColor;
        return leftColor;
    }

    private int CountRun(int insertIndex, int colorId)
    {
        int count = 1;

        for (int i = insertIndex - 1; i >= 0; i--)
        {
            if (balls[i].colorId != colorId) break;
            count++;
        }

        for (int i = insertIndex; i < balls.Count; i++)
        {
            if (balls[i].colorId != colorId) break;
            count++;
        }

        return count;
    }

    private void ResolveSpacingLocal(int pivot)
    {
        // Every ball must stay spacing away from the next one
        for (int i = pivot + 1; i < balls.Count; i++)
        {
            float maxDist = balls[i - 1].dist - spacing;
            if (balls[i].dist > maxDist) balls[i].dist = maxDist;
        }

        for (int i = pivot - 1; i >= 0; i--)
        {
            float minDist = balls[i + 1].dist + spacing;
            if (balls[i].dist < minDist) balls[i].dist = minDist;
        }
    }

    private void ApplyVisuals()
    {
        // If dist is negative the ball is still hidden
        for (int i = 0; i < balls.Count; i++)
        {
            Ball b = balls[i];
            if (b == null) continue;

            bool visible = b.dist >= 0f;

            if (b.rend != null) b.rend.enabled = visible;
            if (visible && b.tr != null) b.tr.position = path.GetPos(b.dist);

            if (b.tr != null)
            {
                ChainBallHit hit = b.tr.GetComponentInChildren<ChainBallHit>();
                if (hit != null) hit.index = i;
            }
        }
    }

    private bool TryGetMatchAtIndex(int index, out MatchRange range)
    {
        range = default(MatchRange);

        if (index < 0 || index >= balls.Count) return false;

        // Rainbow uses the nearest real color so it can match
        int baseColor = GetBaseColorForMatch(index);
        if (baseColor < 0) return false;

        int left = index;
        while (left - 1 >= 0 && IsMatchColor(balls[left - 1].colorId, baseColor))
        {
            left--;
        }

        int right = index;
        while (right + 1 < balls.Count && IsMatchColor(balls[right + 1].colorId, baseColor))
        {
            right++;
        }

        range.start = left;
        range.end = right;

        return range.Count >= matchCount;
    }

    private int GetBaseColorForMatch(int index)
    {
        if (balls[index].colorId >= 0) return balls[index].colorId;

        for (int r = 1; r < balls.Count; r++)
        {
            int left = index - r;
            int right = index + r;

            if (left >= 0 && balls[left].colorId >= 0) return balls[left].colorId;
            if (right < balls.Count && balls[right].colorId >= 0) return balls[right].colorId;
        }

        return -1;
    }

    private bool IsMatchColor(int c, int baseColor)
    {
        // Rainbow counts as the same
        return c == baseColor || c == -1;
    }

    private void ArmChainReaction()
    {
        // This turns on chain reaction checking after we remove something
        chainReactionArmed = true;
        RebuildGapPrev();
    }

    private void RebuildGapPrev()
    {
        gapPrev.Clear();

        for (int i = 0; i < balls.Count - 1; i++)
        {
            float gap = balls[i].dist - (balls[i + 1].dist + spacing);
            gapPrev.Add(gap > gapEps);
        }
    }

    private bool AnyGapNow()
    {
        for (int i = 0; i < balls.Count - 1; i++)
        {
            float gap = balls[i].dist - (balls[i + 1].dist + spacing);
            if (gap > gapEps) return true;
        }
        return false;
    }

    private bool TryChainReaction()
    {
        if (balls.Count < matchCount) return false;

        if (gapPrev.Count != balls.Count - 1)
        {
            RebuildGapPrev();
        }

        // We detect when a gap was there before and is gone now
        // That means two parts of the chain touched again
        // When they touch we check if it made a match
        for (int i = 0; i < balls.Count - 1; i++)
        {
            float gap = balls[i].dist - (balls[i + 1].dist + spacing);

            bool gappedNow = gap > gapEps;
            bool gappedBefore = gapPrev[i];

            bool justClosed = gappedBefore && !gappedNow;

            gapPrev[i] = gappedNow;

            if (!justClosed) continue;

            if (balls[i].colorId != balls[i + 1].colorId) continue;

            MatchRange match;
            if (TryGetMatchAtIndex(i, out match))
            {
                debugHasMatch = true;
                debugMatchStart = match.start;
                debugMatchEnd = match.end;

                comboLevel++;

                RemoveRange(match.start, match.end);
                ApplyVisuals();
                RebuildGapPrev();
                return true;
            }
        }

        return false;
    }

    private int RemoveRange(int start, int end)
    {
        if (balls == null || balls.Count == 0) return 0;

        start = Mathf.Clamp(start, 0, balls.Count - 1);
        end = Mathf.Clamp(end, 0, balls.Count - 1);
        if (start > end) return 0;

        int removed = (end - start + 1);

        Vector3 popupPos;
        bool hasPopupPos = TryGetPopupPosition(start, end, out popupPos);

        for (int i = end; i >= start; i--)
        {
            Transform tr = balls[i].tr;
            if (tr != null) Destroy(tr.gameObject);
            balls.RemoveAt(i);
        }

        int mult = 1 + (comboLevel * comboStep);
        int gainedPoints = removed * pointsPerOrb * mult;
        AddScore(gainedPoints);

        PlayMatchSound();

        if (hasPopupPos)
        {
            ScorePopup.Spawn(popupPos, gainedPoints);
        }

        if (!levelEnded && balls.Count == 0)
        {
            levelEnded = true;
            Action won = OnLevelWon;
            if (won != null) won.Invoke();
        }

        return removed;
    }

    private bool TryGetPopupPosition(int start, int end, out Vector3 popupPos)
    {
        // We try to use the middle ball so the popup looks centered
        popupPos = transform.position;

        int mid = (start + end) / 2;

        if (balls[mid].tr != null)
        {
            popupPos = balls[mid].tr.position;
            return true;
        }

        for (int offset = 1; (mid - offset) >= start || (mid + offset) <= end; offset++)
        {
            int a = mid - offset;
            int b = mid + offset;

            if (a >= start && balls[a].tr != null)
            {
                popupPos = balls[a].tr.position;
                return true;
            }

            if (b <= end && balls[b].tr != null)
            {
                popupPos = balls[b].tr.position;
                return true;
            }
        }

        return false;
    }

    private void PlayMatchSound()
    {
        if (audioSource == null) return;
        if (matchClip == null) return;

        float oldPitch = audioSource.pitch;

        float jitter = UnityEngine.Random.Range(-matchPitchJitter, matchPitchJitter);
        audioSource.pitch = 1f + jitter;

        audioSource.PlayOneShot(matchClip);

        audioSource.pitch = oldPitch;
    }

    private GameObject PickBallPrefab(out int colorId)
    {
        colorId = UnityEngine.Random.Range(0, ballPrefabs.Count);
        return ballPrefabs[colorId];
    }

    private void Update()
    {
        if (levelEnded) return;
        if (balls == null || balls.Count == 0) return;

        float dt = Time.deltaTime;

        UpdateFreeze(dt);

        if (!frozen)
        {
            MoveChainForward(dt);
        }

        if (ReachedEnd())
        {
            return;
        }

        if (chainReactionArmed)
        {
            bool removed = TryChainReaction();

            if (!removed && !AnyGapNow())
            {
                chainReactionArmed = false;
                comboLevel = 0;
            }
        }

        ApplyVisuals();
    }

    private void UpdateFreeze(float dt)
    {
        if (!frozen) return;

        frozenTimer -= dt;

        if (frozenTimer <= 0f)
        {
            frozenTimer = 0f;
            frozen = false;
        }
    }

    private void MoveChainForward(float dt)
    {
        int tail = balls.Count - 1;

        // If tail is still hidden we move faster so it comes in quickly
        bool stillSpawning = balls[tail].dist < 0f;

        float targetSpeed = stillSpawning ? spawnBoostSpeed : normalSpeed;
        float t = 1f - Mathf.Exp(-speedBlend * dt);
        speed = Mathf.Lerp(speed, targetSpeed, t);

        balls[tail].dist += speed * dt;

        // This keeps the chain packed so balls do not separate
        for (int i = tail - 1; i >= 0; i--)
        {
            float minDist = balls[i + 1].dist + spacing;
            if (balls[i].dist < minDist) balls[i].dist = minDist;
        }
    }

    private bool ReachedEnd()
    {
        if (balls[0].dist < path.TotalLength - endPadding) return false;

        if (loopForTesting)
        {
            float shift = balls[0].dist;

            for (int i = 0; i < balls.Count; i++)
            {
                balls[i].dist -= shift;
            }

            RebuildGapPrev();
            chainReactionArmed = false;
            comboLevel = 0;
            return false;
        }

        levelEnded = true;

        Action lost = OnLevelLost;
        if (lost != null) lost.Invoke();

        enabled = false;
        return true;
    }

    private void OnDrawGizmos()
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