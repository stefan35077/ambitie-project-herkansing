using UnityEngine;
using UnityEngine.InputSystem;

public class OrbShooter : MonoBehaviour
{
    public ChainController chain;

    [Header("Hit Settings (Zuma-style)")]
    public float hitRadius = 0.8f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shootClip;
    public AudioClip powerupRollClip;

    [Header("Preview")]
    public Transform previewSocket;
    private GameObject previewInstance;
    public int currentColorId = -1;

    [Header("Projectile")]
    public ProjectileOrb projectilePrefab;
    public Transform muzzle;

    [Header("Projectile Sprites")]
    public GameObject normalProjectileVisual;
    public GameObject rainbowProjectileVisual;
    public GameObject freezeProjectileVisual;
    public GameObject blackHoleProjectileVisual;

    [Header("Powerups")]
    public PowerUpType nextShotType = PowerUpType.None;
    public float freezeSeconds = 2.5f;

    [Header("Powerup Chances")]
    [Range(0f, 1f)] public float powerupChance = 0.12f;
    [Range(0f, 1f)] public float freezeWeight = 0.45f;
    [Range(0f, 1f)] public float rainbowWeight = 0.35f;
    [Range(0f, 1f)] public float blackHoleWeight = 0.20f;


    [Header("Fire Rate")]
    public float shootCooldown = 0.15f;
    private float shootTimer;

    private Vector3 mouseWorldPos;
    private bool hasMousePos;

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

        shootTimer -= Time.deltaTime;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (shootTimer <= 0f)
            {
                Shoot();
                shootTimer = shootCooldown;
            }
        }

        transform.rotation = Quaternion.LookRotation(Vector3.forward, mouseWorldPos - transform.position);
    }

    GameObject GetProjectileVisualPrefab(int colorId)
    {
        // powerups override visuals
        switch (nextShotType)
        {
            case PowerUpType.BlackHole: return blackHoleProjectileVisual;
            case PowerUpType.Rainbow: return rainbowProjectileVisual;
            case PowerUpType.Freeze: return freezeProjectileVisual;
        }

        // normal shot: use the actual color prefab
        if (chain != null && chain.ballPrefabs != null &&
            colorId >= 0 && colorId < chain.ballPrefabs.Count)
            return chain.ballPrefabs[colorId];

        return normalProjectileVisual;
    }

    void RollNextShotType()
    {
        if (Random.value > powerupChance)
        {
            nextShotType = PowerUpType.None;
            return;
        }

        float total = freezeWeight + rainbowWeight + blackHoleWeight;
        float r = Random.value * total;

        if (r < freezeWeight) nextShotType = PowerUpType.Freeze;
        else if (r < freezeWeight + rainbowWeight) nextShotType = PowerUpType.Rainbow;
        else nextShotType = PowerUpType.BlackHole;

        if (audioSource && powerupRollClip)
            audioSource.PlayOneShot(powerupRollClip);
    }

    void RollNextBall()
    {
        if (!chain || chain.ballPrefabs == null || chain.ballPrefabs.Count == 0)
        {
            Debug.LogError("OrbShooter: chain or ballPrefabs missing.");
            return;
        }

        RollNextShotType(); // ✅ THIS is why powerups start showing up

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

        // ✅ preview shows powerup visual, otherwise shows the color ball prefab
        GameObject previewPrefab = GetProjectileVisualPrefab(currentColorId);
        previewInstance = Instantiate(previewPrefab, previewSocket);

        previewInstance.transform.localPosition = Vector3.zero;
        previewInstance.transform.localRotation = Quaternion.identity;
        previewInstance.transform.localScale = Vector3.one;

        foreach (var col in previewInstance.GetComponentsInChildren<Collider2D>(true))
            col.enabled = false;

        foreach (var rb in previewInstance.GetComponentsInChildren<Rigidbody2D>(true))
        {
            rb.simulated = false;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    void Shoot()
    {
        if (!projectilePrefab || !muzzle) return;

        if (audioSource && shootClip)
            audioSource.PlayOneShot(shootClip);

        var proj = Instantiate(projectilePrefab, muzzle.position, transform.rotation);
        proj.shooter = this;
        proj.colorId = currentColorId;
        proj.shotType = nextShotType;

        proj.SetVisual(GetProjectileVisualPrefab(currentColorId));

        nextShotType = PowerUpType.None;

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
                chain.InsertBallAtHitIndex(hitIndex, hitWorldPos, -1);
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