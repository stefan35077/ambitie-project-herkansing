using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ProjectileOrb : MonoBehaviour
{
    [HideInInspector] public OrbShooter shooter;
    [HideInInspector] public int colorId;

    public PowerUpType powerUp = PowerUpType.None;

    public float speed = 25f;
    public float lifeTime = 3f;
    public float armDelay = 0.05f;

    Rigidbody2D rb;
    float life;
    float armTimer;

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
        rb.linearVelocity = (Vector2)transform.up * speed;
    }

    void FixedUpdate()
    {
        armTimer += Time.fixedDeltaTime;

        life += Time.fixedDeltaTime;
        if (life >= lifeTime)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (armTimer < armDelay) return; // ignore collisions right after spawn
        if (!shooter) return;

        var hit = other.GetComponentInParent<ChainBallHit>();
        if (!hit) return;

        shooter.OnProjectileHitChain(hit.index, transform.position, colorId, powerUp);
        Destroy(gameObject);
    }
}