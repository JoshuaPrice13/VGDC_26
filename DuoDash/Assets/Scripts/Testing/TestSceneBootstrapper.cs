using UnityEngine;

/// <summary>
/// Bootstraps the test scene with no network connection.
/// GameManager detects PhotonNetwork.IsConnected == false and routes all
/// game calls (lane change, death) directly without RPCs.
///
/// Setup:
///   1. Duplicate your main game scene and name it TestScene.
///   2. Remove NetworkLobbyManager from the scene (it's lobby-only).
///   3. Keep GameManager in the scene (PhotonView on it is harmless when not connected).
///   4. Attach this script to any GameObject in the test scene.
/// </summary>
public class TestSceneBootstrapper : MonoBehaviour
{
    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[TestBootstrapper] GameManager.Instance is null — make sure GameManager is in this scene.");
            return;
        }

        GameManager.Instance.StartLocalGame();
    }
}
