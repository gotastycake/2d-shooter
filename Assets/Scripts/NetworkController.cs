using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class NetworkController : MonoBehaviour
{
    // Start is called before the first frame update
    string serverIP = "192.168.0.102";
    int port = 8080;
    Socket udp;
    IPEndPoint endPoint;

    enum NetworkType { host, client, none };
    NetworkType networkType = NetworkType.none;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        switch (networkType)
        {
            case NetworkType.host:
                if (Time.frameCount % 60 == 0 && udp.Available != 0)
                {
                    byte[] packet = new byte[64];
                    EndPoint sender = new IPEndPoint(IPAddress.Any, port);

                    int rec = udp.ReceiveFrom(packet, ref sender);
                    string info = Encoding.Default.GetString(packet);

                    Debug.Log("Server received: " + info);
                }
                break;
            case NetworkType.client:
                if (Time.frameCount % 60 == 0 && udp.Available != 0)
                {
                    SendPacket();
                }
                break;
            default:
                break;
        }
    }

    public void SendPacket()
    {
        byte[] arr = Encoding.ASCII.GetBytes("Haha " + Time.frameCount);
        udp.SendTo(arr, endPoint);
    }

    public void HostButton()
    {
        HideUI();
        networkType = NetworkType.host;
        
        IPAddress ip = GetLocalIPAddress();
        IPEndPoint endPoint = new IPEndPoint(ip, port);

        Debug.Log("Server IP Address: " + ip);
        Debug.Log("Port: " + port);
        udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udp.Bind(endPoint);
        udp.Blocking = false;
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

    public void ConnectButton()
    {
        HideUI();
        networkType = NetworkType.client;

        endPoint = new IPEndPoint(IPAddress.Parse(serverIP), port);
        udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udp.Blocking = false;
        SendInitialReqToServer();
    }

    void SendInitialReqToServer()
    {
        string p = "n " + transform.position.x + " " + transform.position.y
                + " " + transform.position.z;

        byte[] packet = Encoding.ASCII.GetBytes(p);
        udp.SendTo(packet, endPoint);
    }
    void HideUI()
    {
        foreach (Button btn in FindObjectsOfType<Button>())
        {
            btn.gameObject.SetActive(false);
        }
    }
}
