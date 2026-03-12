using System;
using UnityEngine;

/// <summary>
/// Прикрепляется к крюку удочки (создаётся в FishingRodTool во время выстрела).
/// При столкновении фиксируется и сообщает FishingRodTool о точке приземления.
/// </summary>
public class FishingHook : MonoBehaviour
{
    [HideInInspector] public Action<Vector3> onLanded;
    [HideInInspector] public float           maxLength;
    [HideInInspector] public Transform       startPoint;

    private bool landed = false;

    void Update()
    {
        if (landed || startPoint == null) return;

        // Если верёвка натянута до предела — остановить на месте
        if (Vector3.Distance(transform.position, startPoint.position) >= maxLength)
            Land(transform.position);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (landed) return;
        if (collision.gameObject.CompareTag("Player")) return;

        Land(collision.contacts[0].point);
    }

    void Land(Vector3 pos)
    {
        if (landed) return;
        landed = true;

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic    = true;
        }

        onLanded?.Invoke(pos);
    }
}
