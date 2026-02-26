using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ProjectileOrb : MonoBehaviour
{
    [HideInInspector] public OrbShooter shooter;
    [HideInInspector] public int colorId;
    [HideInInspector] public PowerUpType shotType;

    public float speed = 25f;
    public float lifeTime = 3f;
    public float armDelay = 0.05f;

    Rigidbody2D rb;
    Collider2D col;

    float life;
    float armTimer;

    Transform visualRoot;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        col.isTrigger = true;
    }

    void Start()
    {
        rb.linearVelocity = (Vector2)transform.up * speed;
    }

    void FixedUpdate()
    {
        armTimer += Time.fixedDeltaTime;

        life += Time.fixedDeltaTime;
        if (life >= lifeTime)
            Destroy(gameObject);
    }

    public void SetVisual(GameObject visualPrefab)
    {
        if (visualRoot) Destroy(visualRoot.gameObject);

        if (!visualPrefab) return;

        var go = Instantiate(visualPrefab, transform);
        visualRoot = go.transform;

        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = Vector3.one;

        foreach (var c in go.GetComponentsInChildren<Collider2D>(true))
            c.enabled = false;

        foreach (var r in go.GetComponentsInChildren<Rigidbody2D>(true))
        {
            r.simulated = false;
            r.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (armTimer < armDelay) return;
        if (!shooter) return;

        var hit = other.GetComponentInParent<ChainBallHit>();
        if (!hit) return;

        shooter.OnProjectileHitChain(hit.index, transform.position, colorId, shotType);
        Destroy(gameObject);
    }
}