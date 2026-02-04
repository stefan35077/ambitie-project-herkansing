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
