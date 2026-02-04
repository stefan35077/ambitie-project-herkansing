using System.Collections.Generic;
using UnityEngine;

public class ChainController : MonoBehaviour
{
    [Header("References")]
    public PathSystem pathSystem;

    [Header("Ball Settings")]
    public GameObject ballPrefab;
    public int ballCount;
    public float Spacing;
    public float Speed;

    private float headDist;
    private List<GameObject> balls = new List<GameObject>();
    private Transform tr;
    private float dist;

    void Start()
    {
        headDist = 0f;

        for (int i = 0; i < ballCount; i++)
        {
            GameObject ball = Instantiate(ballPrefab, transform);
            dist = headDist - i * Spacing;
            balls.Add(ball);
        }
    }

    void Update()
    {
        
    }
}
