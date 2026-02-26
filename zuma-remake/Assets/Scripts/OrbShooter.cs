using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class OrbShooter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ChainController chain;

    [Header("Hit")]
    [SerializeField] private float hitRadius = 0.8f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootClip;
    [SerializeField] private AudioClip powerupRollClip;

    [Header("Preview")]
    [SerializeField] private Transform previewSocket;
    private GameObject previewInstance;

    [Header("Projectile")]
    [SerializeField] private ProjectileOrb projectilePrefab;
    [SerializeField] private Transform muzzle;

    [Header("Projectile visuals")]
    [SerializeField] private GameObject normalProjectileVisual;
    [SerializeField] private GameObject rainbowProjectileVisual;
    [SerializeField] private GameObject freezeProjectileVisual;
    [SerializeField] private GameObject blackHoleProjectileVisual;

    [Header("Powerups")]
    [SerializeField] private PowerUpType nextShotType = PowerUpType.None;
    [SerializeField] private float freezeSeconds = 2.5f;

    [Header("Powerup chances")]
    [SerializeField, Range(0f, 1f)] private float powerupChance = 0.12f;
    [SerializeField, Range(0f, 1f)] private float freezeWeight = 0.45f;
    [SerializeField, Range(0f, 1f)] private float rainbowWeight = 0.35f;
    [SerializeField, Range(0f, 1f)] private float blackHoleWeight = 0.20f;

    [Header("Fire rate")]
    [SerializeField] private float shootCooldown = 0.15f;
    private float shootTimer;

    [Header("Runtime")]
    [SerializeField] private int currentColorId = -1;

    private Vector3 mouseWorldPos;
    private bool hasMousePos;

    private Vector3 mouseClosestPoint;

    private int debugHitIndex = -1;
    private Vector3 debugInsertWorldPos;

    private void Start()
    {
        RollNextBall();
    }

    private void Update()
    {
        if (chain == null) return;
        if (chain.path == null) return;

        UpdateMouseWorldPos();
        if (!hasMousePos) return;

        chain.path.GetClosestDistanceOnPath(mouseWorldPos, out mouseClosestPoint);

        debugHitIndex = FindHitBallIndex(mouseWorldPos, hitRadius);
        UpdateDebugInsertPoint();

        shootTimer -= Time.deltaTime;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (shootTimer <= 0f)
            {
                Shoot();
                shootTimer = shootCooldown;
            }
        }

        Vector3 aimDir = mouseWorldPos - transform.position;
        if (aimDir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(Vector3.forward, aimDir);
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null) return;
        if (muzzle == null) return;
        if (chain == null) return;

        if (audioSource != null && shootClip != null)
        {
            audioSource.PlayOneShot(shootClip);
        }

        ProjectileOrb proj = Instantiate(projectilePrefab, muzzle.position, transform.rotation);

        proj.shooter = this;
        proj.colorId = currentColorId;
        proj.shotType = nextShotType;

        GameObject visualPrefab = GetVisualPrefabForShot(currentColorId, nextShotType);

        // This puts the visual prefab inside the projectile
        // So we do not need any SetVisual function on ProjectileOrb
        SpawnVisualIntoTarget(proj.transform, visualPrefab);

        nextShotType = PowerUpType.None;

        RollNextBall();
    }

    private void RollNextBall()
    {
        if (chain == null) return;
        if (chain.ballPrefabs == null) return;
        if (chain.ballPrefabs.Count == 0) return;

        RollNextShotType();

        int onlyColorId;
        if (TryGetOnlyColor(chain, out onlyColorId))
        {
            currentColorId = onlyColorId;
        }
        else
        {
            currentColorId = GetRandomExistingColorIdFromChain(chain);
        }

        UpdatePreview();
    }

    private int GetRandomExistingColorIdFromChain(ChainController chain)
    {
        if (chain.balls == null || chain.balls.Count == 0)
            return -1;

        HashSet<int> existingColors = new HashSet<int>();
        foreach (var ball in chain.balls)
        {
            if (ball != null)
            {
                existingColors.Add(ball.colorId);
            }
        }

        if (existingColors.Count == 0)
            return -1;

        int randomIndex = UnityEngine.Random.Range(0, existingColors.Count);
        return existingColors.ElementAt(randomIndex);
    }

    private bool TryGetOnlyColor(ChainController chain, out int onlyColorId)
    {
        onlyColorId = -1;
        if (chain.balls == null || chain.balls.Count == 0) return false;

        int firstColorId = chain.balls[0].colorId;
        for (int i = 1; i < chain.balls.Count; i++)
        {
            if (chain.balls[i].colorId != firstColorId)
            {
                return false;
            }
        }

        onlyColorId = firstColorId;
        return true;
    }

    private void RollNextShotType()
    {
        float roll = UnityEngine.Random.value;
        if (roll > powerupChance)
        {
            nextShotType = PowerUpType.None;
            return;
        }

        float total = freezeWeight + rainbowWeight + blackHoleWeight;
        if (total <= 0f)
        {
            nextShotType = PowerUpType.None;
            return;
        }

        float r = UnityEngine.Random.value * total;

        if (r < freezeWeight) nextShotType = PowerUpType.Freeze;
        else if (r < freezeWeight + rainbowWeight) nextShotType = PowerUpType.Rainbow;
        else nextShotType = PowerUpType.BlackHole;

        if (audioSource != null && powerupRollClip != null)
        {
            audioSource.PlayOneShot(powerupRollClip);
        }
    }

    private void UpdatePreview()
    {
        if (previewSocket == null) return;

        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }

        GameObject previewPrefab = GetVisualPrefabForShot(currentColorId, nextShotType);
        if (previewPrefab == null) return;

        previewInstance = Instantiate(previewPrefab, previewSocket);
        ResetLocal(previewInstance.transform);

        DisablePhysicsOnPreview(previewInstance);
    }

    private GameObject GetVisualPrefabForShot(int colorId, PowerUpType shotType)
    {
        if (shotType == PowerUpType.BlackHole) return blackHoleProjectileVisual;
        if (shotType == PowerUpType.Rainbow) return rainbowProjectileVisual;
        if (shotType == PowerUpType.Freeze) return freezeProjectileVisual;

        if (chain != null && chain.ballPrefabs != null && colorId >= 0 && colorId < chain.ballPrefabs.Count)
        {
            return chain.ballPrefabs[colorId];
        }

        return normalProjectileVisual;
    }

    private void SpawnVisualIntoTarget(Transform targetRoot, GameObject visualPrefab)
    {
        if (targetRoot == null) return;
        if (visualPrefab == null) return;

        // If the projectile has a child named Visual we use it
        // Otherwise we use the projectile root
        Transform socket = targetRoot.Find("Visual");
        if (socket == null) socket = targetRoot;

        for (int i = socket.childCount - 1; i >= 0; i--)
        {
            Destroy(socket.GetChild(i).gameObject);
        }

        GameObject instance = Instantiate(visualPrefab, socket);
        ResetLocal(instance.transform);
    }

    private void ResetLocal(Transform t)
    {
        if (t == null) return;
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }

    private void DisablePhysicsOnPreview(GameObject root)
    {
        if (root == null) return;

        Collider[] cols3D = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols3D.Length; i++)
        {
            cols3D[i].enabled = false;
        }

        Rigidbody[] rbs3D = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rbs3D.Length; i++)
        {
            rbs3D[i].isKinematic = true;
            rbs3D[i].detectCollisions = false;
        }

        Collider2D[] cols2D = root.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < cols2D.Length; i++)
        {
            cols2D[i].enabled = false;
        }

        Rigidbody2D[] rbs2D = root.GetComponentsInChildren<Rigidbody2D>(true);
        for (int i = 0; i < rbs2D.Length; i++)
        {
            rbs2D[i].simulated = false;
            rbs2D[i].bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private int FindHitBallIndex(Vector3 worldPos, float radius)
    {
        if (chain == null) return -1;
        if (chain.balls == null) return -1;
        if (chain.balls.Count == 0) return -1;

        int bestIndex = -1;
        float bestSqr = radius * radius;

        for (int i = 0; i < chain.balls.Count; i++)
        {
            ChainController.Ball b = chain.balls[i];
            if (b == null) continue;

            if (b.rend != null && !b.rend.enabled) continue;
            if (b.tr == null) continue;

            float sqr = (worldPos - b.tr.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void UpdateDebugInsertPoint()
    {
        debugInsertWorldPos = Vector3.zero;

        if (debugHitIndex < 0) return;
        if (chain == null) return;
        if (chain.balls == null) return;
        if (debugHitIndex >= chain.balls.Count) return;

        float baseDist = chain.balls[debugHitIndex].dist;

        float distBefore = Mathf.Clamp(baseDist + chain.spacing, 0f, chain.path.TotalLength);
        float distAfter = Mathf.Clamp(baseDist - chain.spacing, 0f, chain.path.TotalLength);

        Vector3 posBefore = chain.path.GetPos(distBefore);
        Vector3 posAfter = chain.path.GetPos(distAfter);

        bool insertBefore = (mouseWorldPos - posBefore).sqrMagnitude <= (mouseWorldPos - posAfter).sqrMagnitude;

        float insertDist = insertBefore ? distBefore : distAfter;
        debugInsertWorldPos = chain.path.GetPos(insertDist);
    }

    private void UpdateMouseWorldPos()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            hasMousePos = false;
            return;
        }

        Vector2 screenPos = ReadPointerScreenPos(out bool ok);
        if (!ok)
        {
            hasMousePos = false;
            return;
        }

        Ray ray = cam.ScreenPointToRay(screenPos);

        float zPlane = chain != null ? chain.transform.position.z : 0f;
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, zPlane));

        float enter;
        if (plane.Raycast(ray, out enter))
        {
            mouseWorldPos = ray.GetPoint(enter);
            hasMousePos = true;
        }
        else
        {
            hasMousePos = false;
        }
    }

    private Vector2 ReadPointerScreenPos(out bool ok)
    {
        ok = false;

        if (Mouse.current != null)
        {
            ok = true;
            return Mouse.current.position.ReadValue();
        }

        if (Pointer.current != null)
        {
            ok = true;
            return Pointer.current.position.ReadValue();
        }

        return Vector2.zero;
    }

    public void OnProjectileHitChain(int hitIndex, Vector3 hitWorldPos, int colorId, PowerUpType shotType)
    {
        if (chain == null) return;

        if (shotType == PowerUpType.BlackHole)
        {
            chain.DestroyBallAtHitIndex(hitIndex);
            return;
        }

        if (shotType == PowerUpType.Freeze)
        {
            chain.Freeze(freezeSeconds);
            return;
        }

        if (shotType == PowerUpType.Rainbow)
        {
            chain.InsertBallAtHitIndex(hitIndex, hitWorldPos, -1);
            return;
        }

        chain.InsertBallAtHitIndex(hitIndex, hitWorldPos, colorId);
    }

    private void OnDrawGizmos()
    {
        if (!hasMousePos) return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(mouseWorldPos, 0.20f);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(mouseClosestPoint, 0.22f);
        Gizmos.DrawLine(mouseWorldPos, mouseClosestPoint);

        if (chain == null) return;
        if (chain.balls == null) return;
        if (chain.balls.Count == 0) return;

        Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(mouseWorldPos, hitRadius);

        if (debugHitIndex >= 0 && debugHitIndex < chain.balls.Count)
        {
            ChainController.Ball b = chain.balls[debugHitIndex];
            if (b != null && b.tr != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(b.tr.position, 0.24f);

                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(debugInsertWorldPos, 0.26f);
                Gizmos.DrawLine(b.tr.position, debugInsertWorldPos);
            }
        }
    }
}