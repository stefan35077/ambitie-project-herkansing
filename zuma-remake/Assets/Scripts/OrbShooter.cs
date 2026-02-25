using UnityEngine;
using UnityEngine.InputSystem;

public class OrbShooter : MonoBehaviour
{
    public ChainController chain;

    [Header("Hit Settings (Zuma-style)")]
    public float hitRadius = 0.8f;

    [Header("Preview")]
    public Transform previewSocket;
    private GameObject previewInstance;
    public int currentColorId = -1;

    [Header("Projectile")]
    public ProjectileOrb projectilePrefab;
    public Transform muzzle;

    [Header("Projectile Visuals")]
    public GameObject normalProjectileVisual;
    public GameObject rainbowProjectileVisual;
    public GameObject freezeProjectileVisual;
    public GameObject blackHoleProjectileVisual;

    [Header("Powerups")]
    public PowerUpType nextShotType = PowerUpType.None;
    public float freezeSeconds = 2.5f;

    // Mouse
    private Vector3 mouseWorldPos;
    private bool hasMousePos;

    // Debug (path projection + gizmos)
    private float mouseDistOnPath;
    private Vector3 mouseClosestPoint;

    private int debugHitIndex = -1;
    private float debugInsertDist;
    private Vector3 debugInsertWorldPos;

    void Start()
    {
        RollNextBall();
    }

    void Update()
    {
        if (!chain || !chain.path) return;

        GetMousePos();
        if (!hasMousePos) return;

        mouseDistOnPath = chain.path.GetClosestDistanceOnPath(mouseWorldPos, out mouseClosestPoint);

        debugHitIndex = FindHitBallIndex(mouseWorldPos, hitRadius);
        ComputeDebugInsertFromHit();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            Shoot();

        transform.rotation = Quaternion.LookRotation(Vector3.forward, mouseWorldPos - transform.position);
    }

    GameObject GetProjectileVisualPrefab(int colorId)
    {
        switch (nextShotType)
        {
            case PowerUpType.BlackHole: return blackHoleProjectileVisual;
            case PowerUpType.Rainbow: return rainbowProjectileVisual;
            case PowerUpType.Freeze: return freezeProjectileVisual;
            default:
                if (normalProjectileVisual) return normalProjectileVisual;
                if (chain && chain.ballPrefabs != null && chain.ballPrefabs.Count > 0)
                {
                    int id = Mathf.Clamp(colorId, 0, chain.ballPrefabs.Count - 1);
                    return chain.ballPrefabs[id];
                }
                return null;
        }
    }

    void RollNextBall()
    {
        if (!chain || chain.ballPrefabs == null || chain.ballPrefabs.Count == 0)
        {
            Debug.LogError("OrbShooter: chain or ballPrefabs missing.");
            return;
        }

        if (chain.TryGetOnlyColor(out int only))
            currentColorId = only;
        else
            currentColorId = chain.GetRandomExistingColorId();

        if (previewInstance) Destroy(previewInstance);

        if (!previewSocket)
        {
            Debug.LogWarning("OrbShooter: previewSocket not assigned.");
            return;
        }

        GameObject prefab = chain.ballPrefabs[currentColorId];
        previewInstance = Instantiate(prefab, previewSocket);

        previewInstance.transform.localPosition = Vector3.zero;
        previewInstance.transform.localRotation = Quaternion.identity;
        previewInstance.transform.localScale = Vector3.one;

        // 2D project: disable 2D colliders on preview
        foreach (var col in previewInstance.GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        var rb2d = previewInstance.GetComponentInChildren<Rigidbody2D>();
        if (rb2d) rb2d.simulated = false;
    }

    void Shoot()
    {
        if (!projectilePrefab || !muzzle) return;

        // Snapshot shot data NOW (so it doesn't change after RollNextBall)
        int shotColor = currentColorId;
        PowerUpType shotType = nextShotType;

        var proj = Instantiate(projectilePrefab, muzzle.position, transform.rotation);
        proj.shooter = this;
        proj.colorId = shotColor;
        proj.shotType = shotType;

        proj.SetVisual(GetProjectileVisualPrefab(shotColor));

        // Like Zuma: immediately show next ball after firing
        RollNextBall();
    }

    int FindHitBallIndex(Vector3 worldPos, float radius)
    {
        if (chain.balls == null || chain.balls.Count == 0) return -1;

        int bestIndex = -1;
        float bestSqr = radius * radius;

        for (int i = 0; i < chain.balls.Count; i++)
        {
            var b = chain.balls[i];
            if (b.rend != null && !b.rend.enabled) continue;

            float sqr = (worldPos - b.tr.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    void ComputeDebugInsertFromHit()
    {
        debugInsertDist = 0f;
        debugInsertWorldPos = Vector3.zero;

        if (debugHitIndex < 0) return;
        if (debugHitIndex >= chain.balls.Count) return;

        float baseDist = chain.balls[debugHitIndex].dist;

        float distBefore = Mathf.Clamp(baseDist + chain.spacing, 0f, chain.path.TotalLength);
        float distAfter = Mathf.Clamp(baseDist - chain.spacing, 0f, chain.path.TotalLength);

        Vector3 posBefore = chain.path.GetPos(distBefore);
        Vector3 posAfter = chain.path.GetPos(distAfter);

        bool insertBefore = (mouseWorldPos - posBefore).sqrMagnitude <= (mouseWorldPos - posAfter).sqrMagnitude;

        debugInsertDist = insertBefore ? distBefore : distAfter;
        debugInsertWorldPos = chain.path.GetPos(debugInsertDist);
    }

    void GetMousePos()
    {
        var cam = Camera.main;
        if (!cam) { hasMousePos = false; return; }

        Vector2 screenPos = (Mouse.current != null)
            ? Mouse.current.position.ReadValue()
            : Pointer.current.position.ReadValue();

        Ray ray = cam.ScreenPointToRay(screenPos);

        float zPlane = chain ? chain.transform.position.z : 0f;
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, zPlane));

        if (plane.Raycast(ray, out float enter))
        {
            mouseWorldPos = ray.GetPoint(enter);
            hasMousePos = true;
        }
        else hasMousePos = false;
    }

    // Called by ProjectileOrb on collision
    public void OnProjectileHitChain(int hitIndex, Vector3 hitWorldPos, int colorId, PowerUpType shotType)
    {
        if (!chain) return;

        switch (shotType)
        {
            case PowerUpType.BlackHole:
                chain.DestroyBallAtHitIndex(hitIndex);
                break;

            case PowerUpType.Freeze:
                chain.InsertBallAtHitIndex(hitIndex, hitWorldPos, colorId);
                chain.Freeze(freezeSeconds);
                break;

            case PowerUpType.Rainbow:
                chain.InsertBallAtHitIndex(hitIndex, hitWorldPos, -1); // -1 => rainbow
                break;

            default:
                chain.InsertBallAtHitIndex(hitIndex, hitWorldPos, colorId);
                break;
        }
    }

    void OnDrawGizmos()
    {
        if (!hasMousePos) return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(mouseWorldPos, 0.20f);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(mouseClosestPoint, 0.22f);
        Gizmos.DrawLine(mouseWorldPos, mouseClosestPoint);

        if (!chain || chain.balls == null || chain.balls.Count == 0) return;

        Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(mouseWorldPos, hitRadius);

        if (debugHitIndex >= 0 && debugHitIndex < chain.balls.Count)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(chain.balls[debugHitIndex].tr.position, 0.24f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(debugInsertWorldPos, 0.26f);
            Gizmos.DrawLine(chain.balls[debugHitIndex].tr.position, debugInsertWorldPos);
        }
    }
}