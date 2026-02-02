using UnityEngine;

public class PathTester : MonoBehaviour
{
    public PathSystem path;
    public Transform follower;
    public float speed = 3f;

    float d;

    void Update()
    {
        if (!path || !follower) return;

        d += speed * Time.deltaTime;
        follower.position = path.GetPos(d);
    }
}
