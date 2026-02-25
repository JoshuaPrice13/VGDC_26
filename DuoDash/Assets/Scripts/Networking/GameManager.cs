using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// Central game state machine. Manages the run lifecycle, handoff between players,
/// score tracking, and level progression.
///
/// Networking approach (proof of concept):
///   - Both clients simulate the ball locally using the same deterministic seed.
///   - Lane change inputs are broadcast via RPC so both clients apply the same input.
///   - On death, the dying player broadcasts their exact position + velocity.
///     The other client uses this to correct any physics drift before their turn begins.
///   - No continuous position streaming — kept intentionally simple for jam scope.
///
/// Attach to a persistent GameObject in your Game scene alongside a PhotonView.
/// </summary>
public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Inspector References
    // -------------------------------------------------------------------------

    [Header("Scene References")]
    public BallController ball;
    public ChunkSpawner chunkSpawner;
    public CameraController cameraController;

    [Header("Handoff Settings")]
    [Tooltip("Seconds of dramatic pause between Player 1 dying and Player 2 taking over.")]
    public float handoffPauseDuration = 1.5f;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    public GameState State { get; private set; } = GameState.Lobby;

    /// <summary>Current level number. Increments each time StartNextLevel is called.</summary>
    public int CurrentLevel { get; private set; } = 1;

    // Actor number of whichever player is currently running.
    // Both clients track this so they know whose input is authoritative.
    private int activeActorNumber = -1;

    // Per-level turn flags. Reset each level.
    private bool p1UsedTurn = false;
    private bool p2UsedTurn = false;

    // Distance recorded at end of each player's turn (accumulated across levels in future).
    private float p1Distance = 0f;
    private float p2Distance = 0f;

    private PhotonView pv;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        pv = GetComponent<PhotonView>();
    }

    // -------------------------------------------------------------------------
    // Public API — called by NetworkLobbyManager or UI
    // -------------------------------------------------------------------------

    /// <summary>Called by the host's Start button. Generates seed and broadcasts RPC_StartLevel.</summary>
    public void HostStartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        int seed = Random.Range(0, int.MaxValue);
        pv.RPC(nameof(RPC_StartLevel), RpcTarget.All, seed, 1);
    }

    /// <summary>
    /// Advances to the next level. Called after the current level's run ends.
    /// LEVEL SYSTEM HOOK: call this from your Game Over / Level Complete UI.
    /// </summary>
    public void HostStartNextLevel()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        int seed = Random.Range(0, int.MaxValue);
        pv.RPC(nameof(RPC_StartLevel), RpcTarget.All, seed, CurrentLevel + 1);
    }

    /// <summary>
    /// Called by InputHandler when the local player taps.
    /// Broadcasts the lane change to both clients so both simulations stay in sync.
    /// </summary>
    public void LocalRequestLaneChange(int direction)
    {
        if (!IsLocalPlayerActive()) return;
        if (State != GameState.Playing) return;
        pv.RPC(nameof(RPC_ChangeLane), RpcTarget.All, direction);
    }

    /// <summary>Called by BallController when the ball hits an obstacle.</summary>
    public void OnBallDied(Vector3 position, Vector3 velocity)
    {
        if (!IsLocalPlayerActive()) return;
        if (State != GameState.Playing) return;

        // Record distance for whichever player just died
        if (IsLocalPlayerOne())
            p1Distance = position.z;
        else
            p2Distance = position.z;

        pv.RPC(nameof(RPC_PlayerDied), RpcTarget.All, position, velocity);
    }

    // -------------------------------------------------------------------------
    // RPCs
    // -------------------------------------------------------------------------

    /// <summary>Starts (or restarts) a level on all clients with a shared seed.</summary>
    [PunRPC]
    void RPC_StartLevel(int seed, int level)
    {
        CurrentLevel = level;

        // Reset per-level state
        p1UsedTurn = false;
        p2UsedTurn = false;

        // NOTE: p1Distance / p2Distance intentionally NOT reset here.
        // Future level system can decide whether score is cumulative or per-level.

        activeActorNumber = PhotonNetwork.MasterClient.ActorNumber; // Player 1 always goes first

        State = GameState.Playing;

        chunkSpawner.Initialize(seed);
        ball.ResetBall();
        // UIManager.Instance.ShowGameHUD();  // HOOK: show HUD when ready

        Debug.Log($"[GameManager] Level {level} started. Seed: {seed}. Active player: {activeActorNumber}");
    }

    /// <summary>Broadcasts a lane change direction to both clients.</summary>
    [PunRPC]
    void RPC_ChangeLane(int direction)
    {
        ball.RequestLaneChange(direction);
    }

    /// <summary>
    /// Called when the active player's ball dies.
    /// Passes the exact position + velocity so the next player's ball is placed correctly.
    /// </summary>
    [PunRPC]
    void RPC_PlayerDied(Vector3 position, Vector3 velocity)
    {
        bool masterClientDied = activeActorNumber == PhotonNetwork.MasterClient.ActorNumber;

        if (masterClientDied) p1UsedTurn = true;
        else p2UsedTurn = true;

        // Both players have used their turn — level is over
        if (p1UsedTurn && p2UsedTurn)
        {
            EndLevel();
            return;
        }

        // Handoff to the other player
        State = GameState.HandoffPause;

        Player otherPlayer = GetOtherPlayer();
        if (otherPlayer != null)
            activeActorNumber = otherPlayer.ActorNumber;

        ball.SetState(position, velocity);
        ball.ResetInputState();

        // UIManager.Instance.PlayHandoffEffect();  // HOOK: screen flash, audio sting

        Invoke(nameof(CompleteHandoff), handoffPauseDuration);

        Debug.Log($"[GameManager] Handoff. New active player: {activeActorNumber}");
    }

    void CompleteHandoff()
    {
        State = GameState.Playing;
        // UIManager.Instance.HideHandoffEffect();  // HOOK
        Debug.Log("[GameManager] Handoff complete.");
    }

    // -------------------------------------------------------------------------
    // Level / Run End
    // -------------------------------------------------------------------------

    void EndLevel()
    {
        State = GameState.LevelComplete;
        float totalDistance = p1Distance + p2Distance;

        Debug.Log($"[GameManager] Level {CurrentLevel} complete. Total distance: {totalDistance:F1}m");

        // LEVEL SYSTEM HOOKS:
        //   - Show level complete screen with totalDistance
        //   - UIManager.Instance.ShowLevelComplete(totalDistance, CurrentLevel);
        //   - Host can call HostStartNextLevel() to advance, or end the game entirely.
        //   - Leaderboard submission goes here.
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>Returns true if the local Photon player is the currently active runner.</summary>
    public bool IsLocalPlayerActive()
    {
        return PhotonNetwork.LocalPlayer.ActorNumber == activeActorNumber;
    }

    private bool IsLocalPlayerOne()
    {
        return PhotonNetwork.LocalPlayer.ActorNumber == PhotonNetwork.MasterClient.ActorNumber;
    }

    private Player GetOtherPlayer()
    {
        foreach (Player p in PhotonNetwork.PlayerList)
            if (p.ActorNumber != activeActorNumber) return p;
        return null;
    }

    // -------------------------------------------------------------------------
    // Photon Callbacks
    // -------------------------------------------------------------------------

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.LogWarning($"[GameManager] {otherPlayer.NickName} left mid-game.");
        // TODO: pause game and show reconnect prompt or return to lobby
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.LogWarning("[GameManager] Master client switched. Mid-game host migration not yet handled.");
        // TODO: handle if needed
    }
}

// -------------------------------------------------------------------------
// Game State Enum
// -------------------------------------------------------------------------

public enum GameState
{
    Lobby,
    Playing,
    HandoffPause,
    LevelComplete, // level ended — both turns used; show score, option to advance
    GameOver       // reserved for true end state (e.g. after final level or disconnect)
}
