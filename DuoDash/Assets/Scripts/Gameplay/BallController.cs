using UnityEngine;

/// <summary>
/// Controls the rolling ball — constant forward movement, 3-lane sliding,
/// and death detection.
///
/// Lane indices: 0 = left, 1 = center, 2 = right
/// Lane X positions: (-laneWidth, 0, +laneWidth)
///
/// No Photon code here — all networking is handled by GameManager via RPCs.
/// BallController is a pure local simulation that both clients run identically.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    [Header("Forward Movement")]
    public float forwardSpeed = 10f;

    [Header("Lane Settings")]
    [Tooltip("Distance between lane centers.")]
    public float laneWidth = 2.5f;

    [Tooltip("How quickly the ball slides to the target lane. Higher = snappier.")]
    public float laneSlideSpeed = 12f;

    // -------------------------------------------------------------------------
    // Internal State
    // -------------------------------------------------------------------------

    private int targetLane = 1;         // Lane we are sliding toward
    private int queuedDirection = 0;    // -1 or +1: input received while mid-slide

    private Rigidbody rb;
    private bool isDead = false;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.State != GameState.Playing) return;

        // Both clients run this — the shared seed + synced lane inputs keep them in sync
        DriveForward();
        SlideLateral();
        FlushQueuedInput();
    }

    // -------------------------------------------------------------------------
    // Movement
    // -------------------------------------------------------------------------

    void DriveForward()
    {
        // Preserve Y velocity so gravity still works; override X and Z
        rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y, forwardSpeed);
    }

    void SlideLateral()
    {
        float targetX = LaneToX(targetLane);
        float newX = Mathf.Lerp(transform.position.x, targetX, Time.fixedDeltaTime * laneSlideSpeed);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }

    void FlushQueuedInput()
    {
        if (queuedDirection == 0) return;

        // Only apply queued input once we are close enough to the current target lane
        if (IsNearTargetLane())
        {
            ApplyLaneChange(queuedDirection);
            queuedDirection = 0;
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Request a lane change. Called by GameManager when RPC_ChangeLane fires on this client.
    /// If mid-slide, the input is queued and applied on arrival.
    /// </summary>
    public void RequestLaneChange(int direction) // -1 left, +1 right
    {
        if (IsNearTargetLane())
            ApplyLaneChange(direction);
        else
            queuedDirection = direction; // queue — overwrite any older queued input
    }

    /// <summary>Full reset for a new level/round.</summary>
    public void ResetBall()
    {
        isDead = false;
        targetLane = 1;
        queuedDirection = 0;

        transform.position = Vector3.zero;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    /// <summary>Clears any queued input — called after handoff so stale input doesn't fire.</summary>
    public void ResetInputState()
    {
        queuedDirection = 0;
    }

    /// <summary>
    /// Sets position and velocity from the dying player's state (handoff).
    /// Both clients call this so both simulations are corrected to the same point.
    /// </summary>
    public void SetState(Vector3 position, Vector3 velocity)
    {
        isDead = false;
        transform.position = position;
        rb.velocity = velocity;
        rb.angularVelocity = Vector3.zero;

        targetLane = GetNearestLane(position.x);
        queuedDirection = 0;
    }

    /// <summary>Called by ObstacleCollision when the ball hits an obstacle.</summary>
    public void OnHitObstacle()
    {
        if (isDead) return;
        isDead = true;
        GameManager.Instance.OnBallDied(transform.position, rb.velocity);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void ApplyLaneChange(int direction)
    {
        targetLane = Mathf.Clamp(targetLane + direction, 0, 2);
    }

    private bool IsNearTargetLane()
    {
        return Mathf.Abs(transform.position.x - LaneToX(targetLane)) < 0.15f;
    }

    private float LaneToX(int lane)
    {
        return (lane - 1) * laneWidth; // lane 0 = -laneWidth, 1 = 0, 2 = +laneWidth
    }

    private int GetNearestLane(float x)
    {
        int best = 1;
        float bestDist = float.MaxValue;
        for (int i = 0; i < 3; i++)
        {
            float dist = Mathf.Abs(x - LaneToX(i));
            if (dist < bestDist) { bestDist = dist; best = i; }
        }
        return best;
    }

    // -------------------------------------------------------------------------
    // Death zone — optional fallback for falling off the course
    // -------------------------------------------------------------------------

    void OnTriggerEnter(Collider other)
    {
        // Tag a death-zone trigger (e.g. a collider below the track) as "DeathZone"
        if (other.CompareTag("DeathZone"))
            OnHitObstacle();
    }
}
