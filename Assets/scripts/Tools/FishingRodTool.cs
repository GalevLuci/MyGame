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
    [SerializeField] private float pullSpeed        = 14f;      // скорость притяжения с земли
    [SerializeField] private float arrivalDistance  = 1f;
    [SerializeField] private float groundedCooldown = 1f;
    [SerializeField] private float minHookHeight    = 1.5f;     // мин. высота крюка над игроком чтобы не считалось волочением

    [Header("Качание (маятник)")]
    [SerializeField] private float swingInputForce  = 6f;       // сила управления при качании
    [SerializeField] private float swingLaunchMult  = 1f;       // множитель инерции при отпускании

    [Header("Звуки")]
    [SerializeField] private AudioClip shootSound;

    [Header("Верёвка (визуал)")]
    [SerializeField] private int   ropeSegments       = 24;
    [SerializeField] private float ropeWidth          = 0.015f;
    [SerializeField] private float ropeGravityScale   = 0.4f;
    [SerializeField] private int   ropeConstraintIter = 12;

    // ── Состояние ─────────────────────────────────────────────────────────────
    private LineRenderer lineRenderer;
    private GameObject   hookObject;
    private Rigidbody    hookRb;
    private FishingHook  hookScript;

    private enum RopeState { Idle, Flying, Attached }
    private RopeState state = RopeState.Idle;

    private Vector3 hookLandedPos;
    private float   cooldownTimer;

    // Маятник
    private bool    isSwinging      = false;
    private Vector3 swingVelocity;
    private float   swingRopeLength;            // длина верёвки в момент зацепа

    // Поворот удочки
    private Quaternion baseRodLocalRot;
    private Quaternion targetRodLocalRot;

    // Верёвка
    private Vector3[] ropePoints;
    private Vector3[] ropePointsPrev;

    // ── Init ──────────────────────────────────────────────────────────────────
    void Awake()
    {
        lineRenderer               = GetComponent<LineRenderer>();
        lineRenderer.positionCount = ropeSegments;
        lineRenderer.startWidth    = ropeWidth;
        lineRenderer.endWidth      = ropeWidth * 0.5f;
        lineRenderer.useWorldSpace = true;
        lineRenderer.enabled       = false;
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

        if (cooldownTimer > 0f) { cooldownTimer -= Time.deltaTime; return; }

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
                if (!held) { ReleaseWithMomentum(); break; }
                transform.localRotation = Quaternion.Slerp(
                    transform.localRotation, targetRodLocalRot, Time.deltaTime * 8f);
                UpdateAttached();
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

        hookRb                        = hookObject.AddComponent<Rigidbody>();
        hookRb.useGravity             = true;
        hookRb.interpolation          = RigidbodyInterpolation.Interpolate;
        hookRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

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
        hookLandedPos   = pos;
        state           = RopeState.Attached;
        isSwinging      = false;
        swingRopeLength = Vector3.Distance(player.transform.position, pos);

        // Поворот удочки к крюку
        Vector3 worldDir = (pos - startPoint.position).normalized;
        if (worldDir != Vector3.zero && transform.parent != null)
            targetRodLocalRot = Quaternion.Inverse(transform.parent.rotation)
                                * Quaternion.LookRotation(worldDir);
    }

    // ── Логика зацепления ─────────────────────────────────────────────────────
    void UpdateAttached()
    {
        Vector3 toHook = hookLandedPos - player.transform.position;
        float   dist   = toHook.magnitude;

        if (dist < arrivalDistance) { Retract(); return; }

        if (player.IsGrounded)
        {
            // Если крюк недостаточно высоко — игрок тащится по полу → отпустить
            float heightAbovePlayer = hookLandedPos.y - player.transform.position.y;
            if (heightAbovePlayer < minHookHeight)
            {
                Retract();
                cooldownTimer = groundedCooldown;
                return;
            }

            // Крюк высоко → тянуть вверх с земли
            isSwinging = false;
            player.SetExternalVelocity(toHook.normalized * pullSpeed);
        }
        else
        {
            // В воздухе — маятник
            if (!isSwinging)
            {
                // Первый кадр в воздухе: инициализируем скорость маятника
                isSwinging      = true;
                swingRopeLength = dist;
                swingVelocity   = player.CurrentVelocity;
            }

            UpdateSwing(toHook, dist);
        }
    }

    void UpdateSwing(Vector3 toHook, float dist)
    {
        Vector3 hookDir = toHook.normalized; // направление к крюку

        // Гравитация
        swingVelocity += Physics.gravity * Time.deltaTime;

        // Ограничение верёвки: убираем компонент "от крюка" когда верёвка натянута
        if (dist >= swingRopeLength * 0.98f)
        {
            float outward = Vector3.Dot(swingVelocity, -hookDir); // скорость прочь от крюка
            if (outward > 0f)
                swingVelocity += hookDir * outward;               // гасим расширение
        }

        // Управление WASD: добавляем силу перпендикулярно верёвке
        Vector3 input = GetSwingInput();
        if (input.sqrMagnitude > 0.01f)
        {
            // Убираем компоненту вдоль верёвки — только перпендикулярное качание
            input -= Vector3.Dot(input, hookDir) * hookDir;
            swingVelocity += input * swingInputForce * Time.deltaTime;
        }

        player.SetVelocityOverride(swingVelocity);
    }

    Vector3 GetSwingInput()
    {
        Vector2 inp = Vector2.zero;
        if (Keyboard.current[player.keyForward].isPressed) inp.y += 1f;
        if (Keyboard.current[player.keyBack].isPressed)    inp.y -= 1f;
        if (Keyboard.current[player.keyLeft].isPressed)    inp.x -= 1f;
        if (Keyboard.current[player.keyRight].isPressed)   inp.x += 1f;
        return (player.transform.right * inp.x + player.transform.forward * inp.y);
    }

    // ── Отпустить с инерцией ──────────────────────────────────────────────────
    void ReleaseWithMomentum()
    {
        if (isSwinging)
        {
            // Передаём вертикальную скорость маятника гравитационной системе игрока
            player.SetYVelocity(swingVelocity.y * swingLaunchMult);
            // Горизонтальный импульс — один кадр внешней скорости
            Vector3 horizontal = new Vector3(swingVelocity.x, 0f, swingVelocity.z);
            player.SetExternalVelocity(horizontal * swingLaunchMult);
        }
        Retract();
    }

    // ── Убрать крюк ───────────────────────────────────────────────────────────
    void Retract()
    {
        state                = RopeState.Idle;
        isSwinging           = false;
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

        for (int i = 1; i < ropeSegments - 1; i++)
        {
            Vector3 vel      = ropePoints[i] - ropePointsPrev[i];
            ropePointsPrev[i] = ropePoints[i];
            ropePoints[i]    += vel + Physics.gravity * ropeGravityScale * dt * dt;
        }

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
