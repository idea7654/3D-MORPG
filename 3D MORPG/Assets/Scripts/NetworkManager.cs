using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System;
using System.Runtime.InteropServices;
using System.IO;
[Serializable]
public class NetworkManager : MonoBehaviour
{   
    #region SOCKET_SET
    byte[] recvByte = new byte[1024];
    Thread ServerCheck_thread;
    Queue<string> Buffer = new Queue<string>();
    string strIP = "127.0.0.1";

    int port = 8000;
    Socket sock;
    IPAddress ip;
    IPEndPoint endPoint;
    #endregion

    #region Variable
    private class Players
    {
        int id;
        float x;
        float y;
        float z;
        double currentTime;
    };
    private double latency;

    #endregion
    // Start is called before the first frame update
    void Start()
    {
        serverOn();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void serverOn()
    {
        sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        ip = IPAddress.Parse(strIP);
        endPoint = new IPEndPoint(ip, port);
        sock.Connect(endPoint);
    }

    public void SendPacket2Server(object obj)
    {
        byte[] userByte = ObjToByte(obj);
        sock.Send(userByte, 0, userByte.Length, SocketFlags.None);
    }

    private byte[] ObjToByte(object obj)
    {
        string json = JsonUtility.ToJson(obj);
        byte[] returnValue = Encoding.UTF8.GetBytes(json);
        return returnValue;
    }
}
