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
public class Network_Login : MonoBehaviour
{
    #region SOCKET_SET
    byte[] recvByte = new byte[10000];
    Thread ServerCheck_thread;
    Queue<string> Buffer = new Queue<string>();
    string strIP = "127.0.0.1";

    int port = 9000;
    int bindPort = 8500;
    Socket sock;
    IPAddress ip;
    IPEndPoint endPoint;
    EndPoint remoteEP;
    IPEndPoint serverEP;
    IPEndPoint bindEP;
    object buffer_lock = new object(); //queue충돌 방지용 lock
    #endregion

    #region Variable
    private class Players
    {
        string nickname;
        float x;
        float y;
        float z;
        float angle_x;
        float angle_y;
        float angle_z;
        int map;
        int exp;
        double currentTime;
    };
    private double latency;

    #endregion
    // Start is called before the first frame update
    void Start()
    {
        serverOn();
        StartCoroutine(buffer_update());
    }

    void serverOn(){
        sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        ip = IPAddress.Parse(strIP);
        endPoint = new IPEndPoint(IPAddress.Any, 0);
        serverEP = new IPEndPoint(ip, port);
        bindEP = new IPEndPoint(ip, bindPort);
        sock.Bind(bindEP);
        remoteEP = (EndPoint)endPoint;
        ServerCheck_thread = new Thread(ServerCheck);
        ServerCheck_thread.Start();
    }

    void ServerCheck()
    {
        while(true)
        {
            sock.Receive(recvByte, 0, recvByte.Length, SocketFlags.None);//서버에서 온 패킷을 버퍼에 담기
            string t = Encoding.Default.GetString(recvByte); //큐에 버퍼를 넣을 준비
            t = t.Replace("\0", string.Empty); //버퍼 마지막에 공백이 있는지 검색하고 공백을 삭제
            lock(buffer_lock){ //큐 충돌방지
               Buffer.Enqueue(t); //큐에 버퍼 저장
            }
            System.Array.Clear(recvByte, 0, recvByte.Length); //버퍼를 사용후 초기화
        }
    }

    IEnumerator buffer_update()
    {
        while(true)
        {
            yield return null; //코루틴에서 반복문 쓸수잇게해줌
            BufferSystem();
        }
    }

    void BufferSystem()
    {
        while(Buffer.Count != 0){ //큐의 크기가 0이 아니면 작동, 만약 while을 안하면 프레임마다 버퍼를 처리하는데
        //많은 패킷을 처리할 땐 처리되는 양보다 쌓이는 양이 많아져 작동이 제대로 이루어지지않음
            string b = null;
            lock(buffer_lock)
            {
                b = Buffer.Dequeue();
            }
            Debug.Log(b); //버퍼를 사용
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SendPacket2Server(object obj)
    {
        byte[] userByte = ObjToByte(obj);
        //sock.Send(userByte, 0, userByte.Length, SocketFlags.None);
        sock.SendTo(userByte, serverEP);
    }

    private byte[] ObjToByte(object obj)
    {
        string json = JsonUtility.ToJson(obj);
        byte[] returnValue = Encoding.UTF8.GetBytes(json);
        return returnValue;
    }
}
