using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class ProjectileOrb : MonoBehaviour
{
    [HideInInspector] public OrbShooter shooter;
    [HideInInspector] public int colorId;
    [HideInInspector] public PowerUpType shotType;

    [Header("Movement")]
    [SerializeField] private float speed = 25f;

    [Header("Lifetime")]
    [SerializeField] private float lifeTime = 3f;

    [Header("Hit safety")]
    [SerializeField] private float armDelay = 0.05f;

    private Rigidbody2D rb;
    private Collider2D col;

    private float life;
    private float armTimer;

    private Transform visualRoot;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        col.isTrigger = true;
    }

    private void Start()
    {
        // We shoot in the up direction of the projectile
        rb.linearVelocity = (Vector2)transform.up * speed;
    }

    private void FixedUpdate()
    {
        armTimer += Time.fixedDeltaTime;

        life += Time.fixedDeltaTime;
        if (life >= lifeTime)
        {
            Destroy(gameObject);
        }
    }

    public void SetVisual(GameObject visualPrefab)
    {
        // This deletes the old visual so we only have one child visual
        if (visualRoot != null)
        {
            Destroy(visualRoot.gameObject);
            visualRoot = null;
        }

        if (visualPrefab == null) return;

        GameObject go = Instantiate(visualPrefab, transform);
        visualRoot = go.transform;

        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = Vector3.one;

        DisablePhysicsOnVisual(go);
    }

    private void DisablePhysicsOnVisual(GameObject root)
    {
        // This makes sure the visual can never collide with the chain
        // Only the projectile collider should do hits
        Collider2D[] cols = root.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i].enabled = false;
        }

        Rigidbody2D[] rbs = root.GetComponentsInChildren<Rigidbody2D>(true);
        for (int i = 0; i < rbs.Length; i++)
        {
            rbs[i].simulated = false;
            rbs[i].bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (armTimer < armDelay) return;
        if (shooter == null) return;

        // We only care about hitting chain balls
        ChainBallHit hit = other.GetComponentInParent<ChainBallHit>();
        if (hit == null) return;

        shooter.OnProjectileHitChain(hit.index, transform.position, colorId, shotType);
        Destroy(gameObject);
    }
}