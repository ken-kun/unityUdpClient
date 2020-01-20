using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

public class NetworkMan : MonoBehaviour
{
    public UdpClient udp;
    // Start is called before the first frame update
    void Start()
    {
        udp = new UdpClient();

        udp.Connect("ec2-54-147-162-251.compute-1.amazonaws.com", 12345);

        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");

        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1);
    }

    void OnDestroy()
    {
        udp.Dispose();
    }


    public enum commands
    {
        NEW_CLIENT,
        UPDATE
    };

    [Serializable]
    public class Message
    {
        public commands cmd;
    }

    [Serializable]
    public class Player
    {
        public string id;
        [Serializable]
        public struct receivedColor
        {
            public float R;
            public float G;
            public float B;
        }
        public receivedColor color;
    }

    [Serializable]
    public class NewPlayer
    {

    }

    [Serializable]
    public class GameState
    {
        public Player[] players;
    }

    public Message latestMessage;
    public GameState lastestGameState;
    void OnReceived(IAsyncResult result)
    {
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;

        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);

        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        Debug.Log("Got this: " + returnData);

        latestMessage = JsonUtility.FromJson<Message>(returnData);
        try
        {
            switch (latestMessage.cmd)
            {
                case commands.NEW_CLIENT:
                    GotNewPlayer = true;
                    break;
                case commands.UPDATE:
                    currentGameState = true;
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                default:
                    Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }

        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }
    GameObject cube;
    bool GotNewPlayer;
    bool currentGameState;
    void SpawnPlayers()
    {
        if (GotNewPlayer)
        {

            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = new Vector3(0, 0.5f, 0);

        }
        //GotNewPlayer = false;







    }

    void UpdatePlayers()
    {
        if (currentGameState)
        {
            var cubeRenderer = cube.GetComponent<Renderer>();
            cubeRenderer.material.color = new Color(lastestGameState.players[0].color.R, lastestGameState.players[0].color.G, lastestGameState.players[0].color.B);

        }
        //currentGameState = false;

    }

    void DestroyPlayers()
    {
        if(GotNewPlayer = false)
        {
            Destroy(cube);
            currentGameState = false;
        }
    }

    void HeartBeat()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update()
    {
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}