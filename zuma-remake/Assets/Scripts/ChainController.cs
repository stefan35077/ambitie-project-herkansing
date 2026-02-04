using System.Collections.Generic;
using UnityEngine;

public class ChainController : MonoBehaviour
{
    [Header("Refs")]
    public PathSystem path;
    public GameObject ballPrefab;

    [Header("Chain")]
    public int ballCount = 20;
    public float speed = 3f;
    public float spacing = 0.6f;
    public float startHeadDist = 3f;
    public float catchUpSpeed = 3f;

    private float headDist;

    [Header("End of Path")]
    public float endPadding = 0.3f;
    public bool loopForTesting = false;

    public class Ball
    {
        public Transform tr;
        public float dist;
        public Renderer rend;
    }

    public List<Ball> balls = new();

    void Start()
    {
        if (!path)
        {
            Debug.LogError("ChainController: 'path' is not assigned.");
            enabled = false;
            return;
        }
        if (!ballPrefab)
        {
            Debug.LogError("ChainController: 'ballPrefab' is not assigned.");
            enabled = false;
            return;
        }

        headDist = startHeadDist;

        for (int i = 0; i < ballCount; i++)
        {
            GameObject go = Instantiate(ballPrefab, transform);

            var r = ballPrefab.GetComponentInChildren<Renderer>();
            float diameter = r.bounds.size.x;   
            spacing = diameter * 0.98f;               

            var b = new Ball
            {
                tr = go.transform,
                dist = headDist - i * spacing,
                rend = go.GetComponentInChildren<Renderer>()
            };

            if (b.rend == null)
                Debug.LogWarning($"Ball prefab has no Renderer (ball index {i}). It may be invisible.");

            balls.Add(b);
        }
    }

    public void InsertBall(float insertDist)
    {
        Debug.Log($"InsertBall called. insertDist={insertDist}");

        if (!path)
        {
            Debug.LogError("InsertBall: path is null");
            return;
        }
        if (!ballPrefab)
        {
            Debug.LogError("InsertBall: ballPrefab is null");
            return;
        }

        // 1) Find nearest ball index
        int nearestIndex = 0;
        float best = float.PositiveInfinity;

        for (int i = 0; i < balls.Count; i++)
        {
            float d = Mathf.Abs(balls[i].dist - insertDist);
            if (d < best)
            {
                best = d;
                nearestIndex = i;
            }
        }

        // 2) Decide insert side
        int insertIndex = balls.Count; // default = end (tail)
        for (int i = 0; i < balls.Count; i++)
        {
            if (insertDist > balls[i].dist) // bigger dist = more toward head
            {
                insertIndex = i;
                break;
            }
        }

        // 3) Instantiate
        GameObject go = Instantiate(ballPrefab, transform);
        go.name = $"InsertedBall_{insertIndex}";
        Debug.Log($"Instantiated: {go.name} at {go.transform.position}");

        Renderer r = go.GetComponentInChildren<Renderer>();
        if (!r) Debug.LogWarning("Inserted ball has no Renderer in prefab (it may be invisible).");

        var newBall = new Ball
        {
            tr = go.transform,
            dist = Mathf.Clamp(insertDist, 0f, path.TotalLength), // safety
            rend = r
        };

        // Force visible + force position RIGHT NOW (so you see it immediately)
        if (newBall.rend != null)
            newBall.rend.enabled = true;

        newBall.tr.position = path.GetPos(newBall.dist);

        // Debug color (temporary)
        if (newBall.rend != null)
            newBall.rend.material.color = Color.red;

        // 4) Insert & resolve
        balls.Insert(insertIndex, newBall);
        ResolveSpacing();

        // Apply positions once after resolving (so it snaps correctly)
        ApplyVisuals();
    }

    void ResolveSpacing()
    {
        for (int i = 1; i < balls.Count; i++)
        {
            float maxDist = balls[i - 1].dist - spacing;
            if (balls[i].dist > maxDist)
                balls[i].dist = maxDist;
        }

        for (int i = balls.Count - 2; i >= 0; i--)
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
            var b = balls[i];

            if (b.rend != null)
                b.rend.enabled = (b.dist >= 0f);

            if (b.dist >= 0f)
                b.tr.position = path.GetPos(b.dist);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // 1) Drive the head
        headDist += speed * dt;
        balls[0].dist = headDist;

        if (headDist >= path.TotalLength - endPadding)
        {
            Debug.Log("Reached end of path!");

            if (loopForTesting)
            {
                headDist = 0f; // quick test reset
                balls[0].dist = headDist;
            }
            else
            {
                enabled = false; // stops the chain (replace with your lose logic)
            }
        }

        // 2) Let the rest try to move forward (catch up)
        for (int i = 1; i < balls.Count; i++)
            balls[i].dist += catchUpSpeed * dt;

        // 3) Enforce spacing (no overlaps)
        for (int i = 1; i < balls.Count; i++)
        {
            float maxDist = balls[i - 1].dist - spacing;
            if (balls[i].dist > maxDist)
                balls[i].dist = maxDist;
        }

        // 4) Apply visuals
        for (int i = 0; i < balls.Count; i++)
        {
            var b = balls[i];

            if (b.rend != null)
                b.rend.enabled = (b.dist >= 0f);

            if (b.dist >= 0f)
                b.tr.position = path.GetPos(b.dist);
        }
    }

}
