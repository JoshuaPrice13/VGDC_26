using UnityEngine;

/// <summary>
/// Chase camera that follows the ball from behind and above.
/// Both players (active and spectating) see the same camera — there is only one ball.
///
/// Attach to your Main Camera.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // Assign the ball transform in the inspector

    [Header("Follow Settings")]
    [Tooltip("Offset from the ball: (0, 4, -7) gives a nice behind-and-above view.")]
    public Vector3 offset = new Vector3(0f, 4f, -7f);

    [Tooltip("How smoothly the camera catches up to the ball. Higher = tighter follow.")]
    public float smoothSpeed = 8f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * smoothSpeed);

        // Always look at the ball
        transform.LookAt(target);
    }
}
