using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{
	private const string LEVEL = "level";
	private const string TEAM = "team";
	private const byte MAX_PLAYERS = 2;
	//[SerializeField] private GameInitializer gameInitializer;
	public string playerLevel = "0";
	public PhotonView photonView;

	[SerializeField] private DeckManager deckManager;

	void Awake()
	{
		PhotonNetwork.AutomaticallySyncScene = true;
	}

	public void Connect()
	{

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinRandomRoom(new ExitGames.Client.Photon.Hashtable() { { LEVEL, playerLevel } }, MAX_PLAYERS);
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

	void Update()
	{
		if (PhotonNetwork.CountOfPlayers <= 1)
        {
			deckManager.SetConnectionStatusText("You can pick card when opponent joins");
		}
        else
        {
			deckManager.SetConnectionStatusText(PhotonNetwork.NetworkClientState.ToString());
		}
	
	}

	#region Photon Callbacks

	public override void OnConnectedToMaster()
	{

		Debug.LogError($"Connected to server. Looking for random room with level {playerLevel}");
		PhotonNetwork.JoinRandomRoom(new ExitGames.Client.Photon.Hashtable() { { LEVEL, playerLevel } }, MAX_PLAYERS);
		//PhotonNetwork.JoinRandomRoom();
	}

	public override void OnJoinRandomFailed(short returnCode, string message)
	{
		Debug.LogError($"Joining random room failed becuse of {message}. Creating new one with player level {playerLevel}");
		PhotonNetwork.CreateRoom(null, new RoomOptions
		{
			CustomRoomPropertiesForLobby = new string[] { LEVEL },
			MaxPlayers = MAX_PLAYERS,
			CustomRoomProperties = new ExitGames.Client.Photon.Hashtable() { { LEVEL, playerLevel } }
		});
		//PhotonNetwork.CreateRoom(null);
	}

	public override void OnJoinedRoom()
	{
		Debug.LogError($"Player {PhotonNetwork.LocalPlayer.ActorNumber} joined a room ");
		//gameInitializer.CreateMultiplayerBoard();
		//PhotonNetwork.NickName = "Hellow";
	}

	public override void OnPlayerEnteredRoom(Player newPlayer)
	{
		Debug.LogError($"Player {newPlayer.ActorNumber} entered a room");
		if (newPlayer.ActorNumber >= 2)
		{
			deckManager.afterMaxPlayersJoined();
		}
	}
	#endregion

	internal bool IsRoomFull()
	{
		return PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers;
	}


}
