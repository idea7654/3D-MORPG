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
using UnityEngine.SceneManagement;
[Serializable]
public class Network_Login : MonoBehaviour
{
    #region SOCKET_SET
    byte[] recvByte = new byte[10000];
    Thread ServerCheck_thread;
    Queue<string> Buffer = new Queue<string>();
    string strIP = "127.0.0.1";

    int port = 9000;
    int bindPort = 8400;
    Socket sock;
    IPAddress ip;
    IPEndPoint endPoint;
    EndPoint remoteEP;
    IPEndPoint serverEP;
    IPEndPoint bindEP;
    object buffer_lock = new object(); //queue충돌 방지용 lock
    #endregion

    #region Variable
    public class Player
    {
        public string nickname;
        public float x;
        public float y;
        public float z;
        public float angle_x;
        public float angle_y;
        public float angle_z;
        public int map;
        public int exp;
        //double currentTime;
        public string message;
    };
    private double latency;
    public GameObject PlayerPrefab;
    public GameObject PlayerPrefab2;

    #endregion
    // Start is called before the first frame update
    void Awake() {
        DontDestroyOnLoad(gameObject);
    }
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
        bindEP = new IPEndPoint(IPAddress.Any, 0);
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
            //Debug.Log(b); //버퍼를 사용
            Player connectPlayer = StringToObj(b);
            switch(connectPlayer.message){
                case "Connect":
                    ConnectNewPlayer(connectPlayer);
                    break;
                case "OtherPlayers":
                    ConnectOtherPlayer(connectPlayer);
                    break;
                default:
                    break;
            }
        }
    }

    void ConnectNewPlayer(Player player)
    {
        //씬전환, 데이터 남기기.
        SceneManager.LoadScene("SampleScene");
        Vector3 position = new Vector3(player.x, player.y, player.z);
        Vector3 angle = new Vector3(player.angle_x, player.angle_y, player.angle_z);
        CreatePlayer(player.nickname, position, angle);
    }

    void ConnectOtherPlayer(Player player)
    {
        Vector3 position = new Vector3(player.x, player.y, player.z);
        Vector3 angle = new Vector3(player.angle_x, player.angle_y, player.angle_z);
        CreateOtherPlayer(player.nickname, position, angle);
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

    public Player StringToObj(string str)
    {
        Player newPlayer = JsonUtility.FromJson<Player>(str);
        return newPlayer;
    }
    public void CreatePlayer(string nickname, Vector3 position, Vector3 angle){
        var obj = Instantiate(PlayerPrefab, position, Quaternion.Euler(angle));
        obj.name = nickname;
        DontDestroyOnLoad(obj);
        //obj.Id = id;
    }
    public void CreateOtherPlayer(string nickname, Vector3 position, Vector3 angle){
        var obj = Instantiate(PlayerPrefab2, position, Quaternion.Euler(angle));
        obj.name = nickname;
        DontDestroyOnLoad(obj);
        //obj.Id = id;
    }
}
