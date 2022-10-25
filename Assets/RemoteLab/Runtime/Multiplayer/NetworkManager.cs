using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using RemoteLab;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    // Variables to set up for Photon
    [SerializeField] private string gameVersion;
    [SerializeField] private string playerName;
    [SerializeField] private string roomName;
    [SerializeField] private bool isParticipant; // TODO: should this always be set to true on build?
    [SerializeField] private List<GameObject> researcherObjs;
    [SerializeField] private List<GameObject> participantObjs;

    public Dictionary<int, Player> ConnectedPlayers { get; private set; }

    private Stack<int> displayIds;
    private int displayIdLimit = 50;

    private static NetworkManager _instance;

    public static NetworkManager Instance { get { return _instance; } }

    private void Awake()
    {
        // Automatically sync scene
        PhotonNetwork.AutomaticallySyncScene = true;

        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }

        ConnectedPlayers = new Dictionary<int, Player>();

        // TODO: doublecheck before commit
//#if UNITY_EDITOR
//        isParticipant = false;
//#else
//        isParticipant = true;
//#endif
    }

    private void Start()
    {
        print("Connecting to network...");

        displayIds = new Stack<int>();

        for (int i = displayIdLimit + 2; i >= 2; i--)
        {
            displayIds.Push(i);
        }

        // Connect to Photon servers
        ConnectToPhoton();
    }

    // Set player name
    public void SetPlayerName(string name)
    {
        playerName = name;
    }

    // Set room name
    public void SetRoomName(string name)
    {
        roomName = name;
    }

    // Connect to Photon
    void ConnectToPhoton()
    {
        print("Attempting connection...");
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
    }

    // Joining a room
    public void JoinRoom()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.LocalPlayer.NickName = playerName;
            print("Attempting to join room...");
            RoomOptions roomOptions = new RoomOptions();
            TypedLobby typedLobby = new TypedLobby(roomName, LobbyType.Default);
            PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, typedLobby);
        }
    }

    // Override for OnConnected
    public override void OnConnected()
    {
        base.OnConnected();
    }

    // Load room after connecting to master server
    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        JoinRoom();
    }

    // Override on OnDisconnected
    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);
        print("Disconnected...");
    }

    // TODO: Add network connectivity UI to display connection status
    // Override for OnJoinedRoom
    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        if (PhotonNetwork.IsMasterClient)
        {
            print("You are the host...");

            if (isParticipant) ConnectedPlayers.Add(PhotonNetwork.LocalPlayer.ActorNumber, PhotonNetwork.LocalPlayer);
        }
        else
        {
            print("Connected to host");
        }

        if (isParticipant)
        {
            foreach (GameObject go in participantObjs)
            {
                go.SetActive(true);
            }
        }
        else
        {
            foreach (GameObject go in researcherObjs)
            {
                go.SetActive(true);
            }
        }        
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        Debug.Log("new player entered");

        ConnectedPlayers.Add(newPlayer.ActorNumber, newPlayer);
    }

    public bool IsParticipant()
    {
        return isParticipant;
    }

    public string GetPlayerName()
    {
        return PhotonNetwork.LocalPlayer.NickName;
    }

    public int AllocateDisplayId()
    {
        if (displayIds.Count <= 0)
            return -1;

        return displayIds.Pop();
    }

    public void DeallocateDisplayId(int id)
    {
        if (!displayIds.Contains(id))
            displayIds.Push(id);
    }
}
