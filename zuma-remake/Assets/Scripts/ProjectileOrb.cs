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
    float life;
    float armTimer;

    GameObject visualInstance;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Start()
    {
        // fire once
        rb.linearVelocity = (Vector2)transform.up * speed;
    }

    void FixedUpdate()
    {
        armTimer += Time.fixedDeltaTime;

        life += Time.fixedDeltaTime;
        if (life >= lifeTime)
            Destroy(gameObject);
    }

    /// <summary>
    /// Assigns a visual prefab to this projectile.
    /// Visual is a CHILD and has NO colliders.
    /// </summary>
    public void SetVisual(GameObject visualPrefab)
    {
        if (!visualPrefab) return;

        // remove old visual if any
        if (visualInstance)
            Destroy(visualInstance);

        visualInstance = Instantiate(visualPrefab, transform);
        visualInstance.transform.localPosition = Vector3.zero;
        visualInstance.transform.localRotation = Quaternion.identity;
        visualInstance.transform.localScale = Vector3.one;

        // IMPORTANT: visuals must not collide
        foreach (var c in visualInstance.GetComponentsInChildren<Collider2D>())
            c.enabled = false;

        foreach (var c in visualInstance.GetComponentsInChildren<Collider>())
            c.enabled = false;

        var rb2d = visualInstance.GetComponentInChildren<Rigidbody2D>();
        if (rb2d) rb2d.simulated = false;

        var rb3d = visualInstance.GetComponentInChildren<Rigidbody>();
        if (rb3d) rb3d.isKinematic = true;
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