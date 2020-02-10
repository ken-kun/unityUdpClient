﻿    using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

public class NetworkMan : MonoBehaviour
{
    public UdpClient udp;
    public GameObject playerGO;

    public string myAddress;
    public Dictionary<string,GameObject> currentPlayers;
    public List<string> newPlayers, droppedPlayers;
    public GameState lastestGameState;
    public ListOfPlayers initialSetofPlayers;
    
    public MessageType latestMessage;

    [Serializable]
    public class ConnectMessage
    {
        public string cmd;
    }
    // Start is called before the first frame update
    void Start()
    {
        newPlayers = new List<string>();
        droppedPlayers = new List<string>();
        currentPlayers = new Dictionary<string, GameObject>();
        initialSetofPlayers = new ListOfPlayers();

        udp = new UdpClient();
        udp.Connect("54.152.99.54",12345);

        ConnectMessage msg = new ConnectMessage();
        msg.cmd = "connect";
        Byte[] sendBytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(msg));
        udp.Send(sendBytes, sendBytes.Length);
        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        


        InvokeRepeating("HeartBeat", 1, 1);
        InvokeRepeating("Position", 0.5f, 0.5f);
    }

    void OnDestroy(){
        udp.Dispose();
    }

    [Serializable]
        public struct receivedPosition{
            public float x;
            public float y;
            public float z;
        }
    [Serializable]
    public class Player{
        public string id;
        public receivedPosition position;        
    }

    [Serializable]
    public class ListOfPlayers{
        public Player[] players;

        public ListOfPlayers(){
            players = new Player[0];
        }
    }
    [Serializable]
    public class ListOfDroppedPlayers{
        public string[] droppedPlayers;
    }
    [Serializable]
    public class GameState
    {
        public int pktID;
        public Player[] players;
    }
    

    [Serializable]
    public class MessageType{
        public commands cmd;
    }
    public enum commands{
        PLAYER_CONNECTED,
        GAME_UPDATE,
        PLAYER_DISCONNECTED,
        CONNECTION_APPROVED,
        LIST_OF_PLAYERS,
    };
    
    void OnReceived(IAsyncResult result){
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;
        
        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);
        
        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        // Debug.Log("Got this: " + returnData);
        
        latestMessage = JsonUtility.FromJson<MessageType>(returnData);
        
        Debug.Log(returnData);
        try{
            switch(latestMessage.cmd){
                case commands.PLAYER_CONNECTED:
                    ListOfPlayers latestPlayer = JsonUtility.FromJson<ListOfPlayers>(returnData);
                    Debug.Log(returnData);
                    foreach (Player player in latestPlayer.players){
                        newPlayers.Add(player.id);
                    }
                    break;
                case commands.GAME_UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                case commands.PLAYER_DISCONNECTED:
                    ListOfDroppedPlayers latestDroppedPlayer = JsonUtility.FromJson<ListOfDroppedPlayers>(returnData);
                    foreach (string player in latestDroppedPlayer.droppedPlayers){
                        droppedPlayers.Add(player);
                    }
                    break;
                case commands.CONNECTION_APPROVED:
                    ListOfPlayers myPlayer = JsonUtility.FromJson<ListOfPlayers>(returnData);
                    Debug.Log(returnData);
                    foreach (Player player in myPlayer.players){
                        newPlayers.Add(player.id);
                        myAddress = player.id;
                    }
                    break;
                case commands.LIST_OF_PLAYERS:
                    initialSetofPlayers = JsonUtility.FromJson<ListOfPlayers>(returnData);
                    break; 
                default:
                    Debug.Log("Error: " + returnData);
                    break;
            }
        }
        catch (Exception e){
            Debug.Log(e.ToString());
        }
        
        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    void SpawnPlayers(){
        if (newPlayers.Count > 0){
            foreach (string playerID in newPlayers){
                currentPlayers.Add(playerID,Instantiate(playerGO, new Vector3(0,0,0),Quaternion.identity));
                currentPlayers[playerID].name = playerID;
            }
            newPlayers.Clear();
        }
        if (initialSetofPlayers.players.Length > 0){
            Debug.Log(initialSetofPlayers);
            foreach (Player player in initialSetofPlayers.players){
                if (player.id == myAddress)
                    continue;
                currentPlayers.Add(player.id, Instantiate(playerGO, new Vector3(0,0,0), Quaternion.identity));
                //currentPlayers[player.id].GetComponent<Renderer>().material.color = new Color(player.color.R, player.color.G, player.color.B);
                currentPlayers[player.id].GetComponent<Transform>().position = new Vector3(player.position.x, player.position.y, player.position.z);
                currentPlayers[player.id].name = player.id;
            }
            initialSetofPlayers.players = new Player[0];
        }
    }

    void UpdatePlayers(){
        if (lastestGameState.players.Length >0){
            foreach (NetworkMan.Player player in lastestGameState.players){
                string playerID = player.id;
                //currentPlayers[player.id].GetComponent<Renderer>().material.color = new Color(player.color.R,player.color.G,player.color.B);
                currentPlayers[player.id].GetComponent<Transform>().position = new Vector3(player.position.x, player.position.y, player.position.z);
            }
            lastestGameState.players = new Player[0];
        }
    }

    void DestroyPlayers(){
        if (droppedPlayers.Count > 0){
            foreach (string playerID in droppedPlayers){
                Debug.Log(playerID);
                Debug.Log(currentPlayers[playerID]);
                Destroy(currentPlayers[playerID].gameObject);
                currentPlayers.Remove(playerID);
            }
            droppedPlayers.Clear();
        }
    }

    [Serializable]
    public class HeartBeatMessage
    {
        public string cmd;
    }

    void HeartBeat(){
        HeartBeatMessage msg = new HeartBeatMessage();
        msg.cmd = "heartbeat";
        Byte[] sendBytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(msg));
        udp.Send(sendBytes, sendBytes.Length);
    }

    [Serializable]
    public class PositionMessage
    {
        public string cmd;
        public Vector3 position;
    }

    void Position()
    {
        PositionMessage msg = new PositionMessage();
        msg.cmd = "posUpdate";
        msg.position.x = currentPlayers[myAddress].GetComponent<Transform>().position.x;
        msg.position.y = currentPlayers[myAddress].GetComponent<Transform>().position.y;
        msg.position.z = currentPlayers[myAddress].GetComponent<Transform>().position.z;
        string json = JsonUtility.ToJson(msg);
        Byte[] sendBytes = Encoding.ASCII.GetBytes(json);
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update(){
        
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}