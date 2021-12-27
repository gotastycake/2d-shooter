using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class NetworkController : MonoBehaviour
{
    public static string serverIP = "127.0.0.1";
    public static int port = 8080;
    public static Socket udp;
    public static IPEndPoint endPoint;

    public static List<EndPoint> clients;
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (udp == null || udp.Available == 0) { return; }

        byte[] rawPacket = new byte[2048];
        EndPoint sender = new IPEndPoint(IPAddress.Any, port);
        int rec = udp.ReceiveFrom(rawPacket, ref sender);
        Debug.Log(rec);
        byte[] packet = new byte[rec];
        for (int i = 0; i < rec; i++) { packet[i] = rawPacket[i]; }
        Message msg = Message.FromBytes(packet);
        
        string type = msg.type;
        if (type != "render_move" && type != "make_move")
        {
            LogMessage(Shared.networkType.ToString(), "Got message:", type);
        }
        GameController gc = GameObject.FindObjectOfType<GameController>();
        switch (Shared.networkType)
        {
            case Shared.NetworkType.host:
                switch (type)
                {
                    case "connect":
                        LogMessage("Clients connected", (clients.Count + 1).ToString());
                        if (clients.Count >= 3) { return; }

                        int id = clients.Count + 2;
                        clients.Add(sender);
                        Message msgToSend = new Message();
                        msgToSend.type = "accept";
                        msgToSend.data["id"] = id.ToString();
                        SendPacket(sender, msgToSend);
                        GameObject.Find("ConnectedAmount").GetComponent<Text>().text = "Connected: " + id + "/4";
                        break;
                    case "disconnect":
                        clients.RemoveAt(msg.id - 2);
                        GameObject.Find("ConnectedAmount").GetComponent<Text>().text = "Connected: " + (msg.id - 1) + "/4";
                        break;
                    case "make_move":
                        GameController.HandleMove((Move)msg);
                        break;
                    case "shot_pacman":
                        GameObject p = gc.CreatePacman((Move)msg);
                        GameController.BroadcastPacman(p);
                        break;
                    case "me_hit":
                        GameController.HandleMeHit(msg);
                        break;
                    case "heal":
                        GameController.HandleBooster(msg);
                        break;
                    case "damage":
                        GameController.HandleBooster(msg);
                        break;
                    default:
                        Debug.Log("[" + type + "]");
                        break;
                }
                break;
            case Shared.NetworkType.client:
                switch (type)
                {
                    case "accept":
                        MenuController.IncConnectedAmount(msg);
                        break;
                    case "start":
                        MenuController.StartGame();
                        break;
                    case "render_move":
                        GameController.RenderMove((Move)msg);
                        break;
                    case "initgame":
                        GameController.InitGame(msg);
                        break;
                    case "create_packman":
                        gc.CreatePacman((Move)msg);
                        break;
                    case "hit":
                        GameController.HandleHit(msg);
                        break;
                    case "kill":
                        gc.HandleKill(msg);
                        break;
                    case "wall":
                        gc.SpawnWall((Move)msg);
                        break;
                    case "heal_up":
                        GameController.HealthUp(msg);
                        break;
                    case "damage_up":
                        GameController.DamageUp(msg);
                        break;
                    case "spawn_heal":
                        gc.SpawnHeal((Move)msg);
                        break;
                    case "spawn_damage":
                        gc.SpawnDamage((Move)msg);
                        break;
                    default:
                        LogMessage("[", type, "]");
                        break;
                }
                break;
            default:
                LogMessage("Default NetworkType");
                break;
        }
    }

    static void SendPacket(EndPoint addr, string type)
    {
        Message msg = new Message();
        msg.type = type;
        byte[] arr = msg.ToBytes();
        udp.SendTo(arr, addr);
    }

    static void SendPacket(EndPoint addr, Message msg)
    {
        if (msg.type != "render_move" && msg.type != "make_move") {
            Debug.Log("Sending a message, type: " + msg.type + " id: " + msg.id);
        }
        
        byte[] arr = msg.ToBytes();
        udp.SendTo(arr, addr);
    }

    public static void BroadcastPacket(Message msg)
    {
        foreach (EndPoint p in clients)
        {
            SendPacket(p, msg);
        }
    }

    public static IPAddress GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip;
            }
        }
        return null;
    }

    public static void CreateServer()
    {
        if (udp != null)
        {
            Debug.Log("Server is already created!");
        }
        clients = new List<EndPoint>();
        IPAddress ip = IPAddress.Parse(serverIP);
        IPEndPoint endPoint = new IPEndPoint(ip, port);

        udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udp.Bind(endPoint);
        udp.Blocking = false;
        Debug.Log("Server has been created successfully. IP Address: " + ip + ", port: " + port);
    }

    public static void ConnectToServer()
    {
        udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udp.Blocking = false;

        Message msg = new Message();
        msg.type = "connect";
        SendPacketToServer(msg);
        LogMessage("Sent to server:", msg.type);
    }

    public static void SendPacketToServer(Message msg) {
        endPoint = new IPEndPoint(IPAddress.Parse(serverIP), port);
        SendPacket(endPoint, msg);
    }

    public static void StartGame()
    {
        Message msg = new Message();
        msg.type = "start";
        BroadcastPacket(msg);
    }

    public static void LogMessage(params string[] msg)
    {
        Text logText = GameObject.Find("LogLine").GetComponent<Text>();
        if (logText.text.Split('\n').Length > 50) {
            logText.text = "";
        }
        string totalMsg = "";
        foreach (string msgElem in msg)
        {
            totalMsg += msgElem + " ";
        }
        logText.text += '\n' + totalMsg;
    }
}
