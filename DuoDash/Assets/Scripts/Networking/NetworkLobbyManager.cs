using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles Photon connection, room creation/joining by code, and the pre-game waiting room.
/// Attach to a persistent GameObject in your Lobby scene.
/// Requires a PhotonView on the same GameObject.
/// </summary>
public class NetworkLobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI Panels")]
    public GameObject lobbyPanel;
    public GameObject waitingRoomPanel;

    [Header("Lobby UI")]
    public TMP_InputField joinCodeInput;

    [Header("Waiting Room UI")]
    public TextMeshProUGUI roomCodeDisplay;
    public TextMeshProUGUI waitingStatusText;
    public Button startButton; // Only visible/interactable for master client

    private const string GameVersion = "1.0";

    void Start()
    {
        PhotonNetwork.GameVersion = GameVersion;
        PhotonNetwork.AutomaticallySyncScene = true;

        lobbyPanel.SetActive(false);
        waitingRoomPanel.SetActive(false);

        PhotonNetwork.ConnectUsingSettings();
    }

    // -------------------------------------------------------------------------
    // Photon Callbacks
    // -------------------------------------------------------------------------

    public override void OnConnectedToMaster()
    {
        Debug.Log("[Lobby] Connected to Photon master server.");
        lobbyPanel.SetActive(true);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[Lobby] Joined room: {PhotonNetwork.CurrentRoom.Name}");
        lobbyPanel.SetActive(false);
        waitingRoomPanel.SetActive(true);

        roomCodeDisplay.text = "Room Code: " + PhotonNetwork.CurrentRoom.Name;
        RefreshWaitingUI();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[Lobby] Player joined: {newPlayer.NickName}");
        RefreshWaitingUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[Lobby] Player left: {otherPlayer.NickName}");
        RefreshWaitingUI();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[Lobby] Create room failed ({returnCode}): {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[Lobby] Join room failed ({returnCode}): {message}");
        // TODO: show error message in UI
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[Lobby] Disconnected: {cause}");
        lobbyPanel.SetActive(false);
        waitingRoomPanel.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // Button Handlers (wire these up in the Unity editor)
    // -------------------------------------------------------------------------

    public void OnCreateRoomPressed()
    {
        string code = GenerateRoomCode();
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = false // room is private, join by code only
        };
        PhotonNetwork.CreateRoom(code, options);
    }

    public void OnJoinRoomPressed()
    {
        string code = joinCodeInput.text.ToUpper().Trim();
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("[Lobby] No room code entered.");
            return;
        }
        PhotonNetwork.JoinRoom(code);
    }

    public void OnStartGamePressed()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            Debug.LogWarning("[Lobby] Cannot start — need 2 players.");
            return;
        }
        GameManager.Instance.HostStartGame();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void RefreshWaitingUI()
    {
        int count = PhotonNetwork.CurrentRoom.PlayerCount;
        waitingStatusText.text = count < 2 ? "Waiting for opponent..." : "Both players connected!";

        // Start button only shown to host, enabled only when both players present
        bool isHost = PhotonNetwork.IsMasterClient;
        startButton.gameObject.SetActive(isHost);
        startButton.interactable = isHost && count == 2;
    }

    private static string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no ambiguous chars (0/O, 1/I)
        char[] code = new char[6];
        for (int i = 0; i < code.Length; i++)
            code[i] = chars[Random.Range(0, chars.Length)];
        return new string(code);
    }
}
