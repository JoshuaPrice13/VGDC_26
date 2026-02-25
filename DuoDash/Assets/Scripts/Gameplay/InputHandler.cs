using UnityEngine;

/// <summary>
/// Detects screen taps and translates them into lane change requests.
///
/// Tap left half of screen  → move one lane left
/// Tap right half of screen → move one lane right
///
/// Only active when GameManager identifies the local player as the current runner.
/// Works with touch (mobile) and mouse click (editor testing).
/// </summary>
public class InputHandler : MonoBehaviour
{
    // No inspector references needed — GameManager and BallController are singletons/scene refs.
    // If you prefer explicit wiring, add public fields here.

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.State != GameState.Playing) return;
        if (!GameManager.Instance.IsLocalPlayerActive()) return;

        DetectTap();
    }

    void DetectTap()
    {
        // ---- Touch (mobile) ----
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
                SendLaneChange(touch.position.x);
            return;
        }

        // ---- Mouse (editor / PC testing) ----
        if (Input.GetMouseButtonDown(0))
            SendLaneChange(Input.mousePosition.x);
    }

    void SendLaneChange(float screenX)
    {
        int direction = screenX < Screen.width * 0.5f ? -1 : 1;
        GameManager.Instance.LocalRequestLaneChange(direction);
    }
}
