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
    public float startHeadDist = 3f; // IMPORTANT: prevents everything being < 0 at start

    private float headDist;

    private class Ball
    {
        public Transform tr;
        public float dist;
        public Renderer rend;
    }

    private readonly List<Ball> balls = new();

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
            var b = new Ball
            {
                tr = go.transform,
                b.dist += speed * dt;

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
        headDist += speed * Time.deltaTime;

        for (int i = 0; i < balls.Count; i++)
        {
            var b = balls[i];
            b.dist = headDist - i * spacing;

            if (b.rend != null)
                b.rend.enabled = (b.dist >= 0f);

            if (b.dist >= 0f)
                b.tr.position = path.GetPos(b.dist);
        }
    }
}
