using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class FishingRodTool : PlayerTool
{
    [Header("Удочка")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private float ropeLength       = 10f;
    [SerializeField] private float hookSpeed        = 25f;
    [SerializeField] private float pullSpeed        = 14f;
    [SerializeField] private float arrivalDistance  = 1f;
    [SerializeField] private float groundedCooldown = 1f;

    [Header("Звуки")]
    [SerializeField] private AudioClip shootSound;

    [Header("Верёвка (визуал)")]
    [SerializeField] private int   ropeSegments            = 24;
    [SerializeField] private float ropeWidth               = 0.015f;
    [SerializeField] private float ropeGravityScale        = 0.4f;
    [SerializeField] private int   ropeConstraintIter      = 12;

    // ── Внутренние ────────────────────────────────────────────────────────────
    private LineRenderer lineRenderer;
    private GameObject   hookObject;
    private Rigidbody    hookRb;
    private FishingHook  hookScript;

    private enum RopeState { Idle, Flying, Attached }
    private RopeState state = RopeState.Idle;

    private Vector3    hookLandedPos;
    private float      cooldownTimer;

    private Vector3[]  ropePoints;
    private Vector3[]  ropePointsPrev;

    private Quaternion baseRodLocalRot;
    private Quaternion targetRodLocalRot;

    // ── Init ──────────────────────────────────────────────────────────────────
    void Awake()
    {
        lineRenderer                = GetComponent<LineRenderer>();
        lineRenderer.positionCount  = ropeSegments;
        lineRenderer.startWidth     = ropeWidth;
        lineRenderer.endWidth       = ropeWidth * 0.5f;
        lineRenderer.useWorldSpace  = true;
        lineRenderer.enabled        = false;
    }

    protected override void OnEquipped()
    {
        baseRodLocalRot   = transform.localRotation;
        targetRodLocalRot = transform.localRotation;
        cooldownTimer     = 0f;
    }

    protected override void OnUnequipped() => Retract();

    // ── Update ────────────────────────────────────────────────────────────────
    void Update()
    {
        if (!IsEquipped || player == null) return;

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        bool held    = Keyboard.current[ActionKey].isPressed;
        bool pressed = Keyboard.current[ActionKey].wasPressedThisFrame;

        switch (state)
        {
            case RopeState.Idle:
                if (pressed) Shoot();
                break;

            case RopeState.Flying:
                if (!held) { Retract(); break; }
                if (hookObject != null &&
                    Vector3.Distance(startPoint.position, hookObject.transform.position) >= ropeLength)
                    Retract();
                break;

            case RopeState.Attached:
                if (!held) { Retract(); break; }
                // Плавно повернуть удочку к точке крюка
                transform.localRotation = Quaternion.Slerp(
                    transform.localRotation, targetRodLocalRot, Time.deltaTime * 8f);
                PullPlayer();
                break;
        }
    }

    void LateUpdate()
    {
        if (state != RopeState.Idle && lineRenderer.enabled)
            UpdateRopeVisual();
    }

    // ── Выстрел ───────────────────────────────────────────────────────────────
    void Shoot()
    {
        hookObject                      = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hookObject.transform.localScale = Vector3.one * 0.07f;
        hookObject.transform.position   = startPoint.position;
        hookObject.name                 = "FishingHook_Runtime";

        hookRb                          = hookObject.AddComponent<Rigidbody>();
        hookRb.useGravity               = true;
        hookRb.interpolation            = RigidbodyInterpolation.Interpolate;
        hookRb.collisionDetectionMode   = CollisionDetectionMode.Continuous;

        hookScript            = hookObject.AddComponent<FishingHook>();
        hookScript.onLanded   = OnHookLanded;
        hookScript.maxLength  = ropeLength;
        hookScript.startPoint = startPoint;

        // Направление — горизонтально или вверх, но НЕ вниз
        Vector3 shootDir = player.cameraTransform.forward;
        shootDir.y = Mathf.Max(shootDir.y, 0f);
        if (shootDir.sqrMagnitude < 0.001f) shootDir = player.transform.forward;
        shootDir.Normalize();

        hookRb.linearVelocity = shootDir * hookSpeed;
        AudioManager.Instance?.PlaySFX(shootSound);

        InitRopePoints();
        lineRenderer.enabled = true;
        state = RopeState.Flying;
    }

    void OnHookLanded(Vector3 pos)
    {
        hookLandedPos = pos;
        state         = RopeState.Attached;

        // Считаем локальный поворот удочки в направлении крюка
        Vector3 worldDir = (pos - startPoint.position).normalized;
        if (worldDir != Vector3.zero && transform.parent != null)
            targetRodLocalRot = Quaternion.Inverse(transform.parent.rotation)
                                * Quaternion.LookRotation(worldDir);
    }

    // ── Притяжение ────────────────────────────────────────────────────────────
    void PullPlayer()
    {
        Vector3 dir  = hookLandedPos - player.transform.position;
        float   dist = dir.magnitude;

        if (dist < arrivalDistance) { Retract(); return; }

        dir.Normalize();

        // Если тянет горизонтально и игрок на земле — он тащится по полу → отпустить
        float upFactor = Vector3.Dot(dir, Vector3.up);
        if (player.IsGrounded && upFactor < 0.3f)
        {
            Retract();
            cooldownTimer = groundedCooldown;
            return;
        }

        player.SetExternalVelocity(dir * pullSpeed);
    }

    // ── Убрать крюк ───────────────────────────────────────────────────────────
    void Retract()
    {
        state                = RopeState.Idle;
        lineRenderer.enabled = false;

        if (hookObject != null) { Destroy(hookObject); hookObject = null; }

        StopAllCoroutines();
        StartCoroutine(RestoreRodRotation());
    }

    IEnumerator RestoreRodRotation()
    {
        Quaternion from = transform.localRotation;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            transform.localRotation = Quaternion.Slerp(from, baseRodLocalRot, t);
            yield return null;
        }
        transform.localRotation = baseRodLocalRot;
    }

    // ── Верёвка (Verlet) ──────────────────────────────────────────────────────
    void InitRopePoints()
    {
        ropePoints     = new Vector3[ropeSegments];
        ropePointsPrev = new Vector3[ropeSegments];
        for (int i = 0; i < ropeSegments; i++)
        {
            ropePoints[i]     = startPoint.position;
            ropePointsPrev[i] = startPoint.position;
        }
    }

    void UpdateRopeVisual()
    {
        if (ropePoints == null) return;

        Vector3 endPos = (state == RopeState.Flying && hookObject != null)
            ? hookObject.transform.position
            : hookLandedPos;

        float segLen = ropeLength / (ropeSegments - 1);
        float dt     = Time.deltaTime;

        // Verlet — применяем гравитацию к внутренним точкам
        for (int i = 1; i < ropeSegments - 1; i++)
        {
            Vector3 vel      = ropePoints[i] - ropePointsPrev[i];
            ropePointsPrev[i] = ropePoints[i];
            ropePoints[i]    += vel + Physics.gravity * ropeGravityScale * dt * dt;
        }

        // Ограничения длины сегментов
        for (int iter = 0; iter < ropeConstraintIter; iter++)
        {
            ropePoints[0]                = startPoint.position;
            ropePoints[ropeSegments - 1] = endPos;

            for (int i = 0; i < ropeSegments - 1; i++)
            {
                float   d    = Vector3.Distance(ropePoints[i], ropePoints[i + 1]);
                Vector3 corr = (ropePoints[i + 1] - ropePoints[i]).normalized
                               * ((d - segLen) * 0.5f);
                if (i != 0)                    ropePoints[i]     += corr;
                if (i + 1 != ropeSegments - 1) ropePoints[i + 1] -= corr;
            }
        }

        lineRenderer.SetPositions(ropePoints);
    }
}
