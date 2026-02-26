using UnityEngine;

/// <summary>
/// Place this on any obstacle that should kill the ball on contact.
/// The obstacle's collider should be set to "Is Trigger" in the Unity editor.
///
/// Tag your ball GameObject with "Player".
/// </summary>
public class ObstacleCollision : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        BallController ball = other.GetComponent<BallController>();
        if (ball != null)
            Debug.Log("Hit obstcle");
            ball.OnHitObstacle();
    }
}
