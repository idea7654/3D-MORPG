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
    byte[] recvByte = new byte[512];
    Thread ServerCheck_thread;
    Queue<string> Buffer = new Queue<string>();
    Queue<string> Buffer_Connection = new Queue<string>();
    string strIP = "127.0.0.1";

    int port = 9000;
    Socket sock;
    IPAddress ip;
    IPEndPoint endPoint;
    EndPoint remoteEP;
    IPEndPoint serverEP;
    IPEndPoint bindEP;
    IPEndPoint CsServerEP;
    object buffer_lock = new object(); //queue충돌 방지용 lock
    
    public float speed = 3.0f;
    //private bool ConnectionFlag = false;
    #endregion

    #region Variable
    public class Enemy{
        public string id;
        public float damage;
    }
    public class Player : Enemy
    {
        public string nickname;
        public float x;
        public float y;
        public float z;
        public float angle_x;
        public float angle_y;
        public float angle_z;
        public PlayerMove playerMove;
        public int map;
        public int exp;
        //double currentTime;
        public string message;
        public long currentTime;
        public string chat;
        public PlayerStateAttack playerStateAttack;
    };
    public class ConnectionPacket : Player{
        //public string message;
    }
    public enum PlayerMove{
        stop = 0,
        turn_left = 1,
        turn_right = 2,
        moveFront = 3,
        moveBack = 4,
        moveFrontLeft,
        moveFrontRight,
        moveBackLeft,
        moveBackRight
    };
    private long latency;
    public GameObject PlayerPrefab;
    public GameObject PlayerPrefab2;
    public GameObject EnemyPrefab;

    #endregion
    // Start is called before the first frame update
    public string PlayerName;
    void Awake() {
        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        serverOn();
        StartCoroutine(buffer_update());
        
        GameObject.Find("OverLogin").SetActive(false);
    }

    void serverOn(){
        sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        ip = IPAddress.Parse(strIP);
        endPoint = new IPEndPoint(IPAddress.Any, 0);
        serverEP = new IPEndPoint(ip, port);
        bindEP = new IPEndPoint(IPAddress.Any, 0);
        CsServerEP = new IPEndPoint(ip, 8000);
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
            BufferSystem();
            yield return null; //코루틴에서 반복문 쓸수잇게해줌
        }
    }

    void ConnectPacket()
    {
        //while(true)
        //{
            ConnectionPacket packet = new ConnectionPacket();
            packet.message = "connected";
            packet.nickname = PlayerName;
            var player = GameObject.Find(PlayerName);
            packet.x = player.transform.position.x;
            packet.y = player.transform.position.y;
            packet.z = player.transform.position.z;
            packet.angle_y = player.transform.eulerAngles.y;
            SendPacket2CsServer(packet);
            //yield return new WaitForSecondsRealtime(.5f);
        //}
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
            //Debug.Log(DateTime.Now.TimeOfDay.TotalMilliseconds - connectPlayer.currentTime);
            switch(connectPlayer.message){
                case "Connect":
                    ConnectNewPlayer(connectPlayer);
                    //StartCoroutine(ConnectPacket());
                    InvokeRepeating("ConnectPacket", 1f, 1f);
                    //ConnectionFlag = true;
                    break;
                case "OtherPlayers":
                    ConnectOtherPlayer(connectPlayer);
                    break;
                case "PlayerMove":
                    MovePlayer(connectPlayer);
                    break;
                case "PlayerAction":
                    ActionPlayer(connectPlayer);
                    break;
                case "OverLogin":
                    OverLogin(connectPlayer);
                    break;
                case "Logout":
                    Logout(connectPlayer);
                    break;
                case "Chatting":
                    GameObject.Find("Canvas").GetComponent<Chatting>().AddChat(connectPlayer.nickname, connectPlayer.chat);
                    break;
                case "Respawn":
                    EnemyRespawn(connectPlayer);
                    break;
                default:
                    break;
            }
        }
    }
    //1. 대각이동 만들기
    //2. 데드 레커닝 적용하기.

    void MovePlayer(Player player)
    {
        latency = DateTimeOffset.Now.ToUnixTimeMilliseconds() - player.currentTime;
        //레이턴시 대강 6ms정도(솔플, 로컬기준)
        //Debug.Log(latency);
        GameObject target = GameObject.Find(player.nickname);
        //target.transform.position = new Vector3(player.x, 0, player.z);
        if(player.playerMove == PlayerMove.stop)
        {
            target.transform.position = new Vector3(player.x, player.y, player.z);
            target.transform.rotation = Quaternion.Euler(new Vector3(0, player.angle_y, 0));
        }else{
            switchMove(player, target);
        }
        //target.transform.rotation = Quaternion.Euler(new Vector3(0, player.angle_y, 0));
        DeadReckoning(player);
    }

    void EnemyRespawn(Player player)
    {
        GameObject obj = Instantiate(EnemyPrefab, new Vector3(player.x, player.y, player.z), Quaternion.Euler(new Vector3(0, 0, 0)));
        EnemyFSM enemyInfo = obj.GetComponent<EnemyFSM>();
        enemyInfo.enemyInfo.id = Convert.ToInt32(player.id);
        enemyInfo.enemyInfo.damage = player.damage;
        obj.transform.Rotate(new Vector3(0, player.angle_y, 0));
        obj.name = player.id;
        DontDestroyOnLoad(obj);
    }

    void switchMove(Player player, GameObject target)
    {
        float x = Mathf.Cos(player.angle_y * Mathf.PI / 180) * speed * Convert.ToSingle(latency) / 1000 * Time.deltaTime;
        float z = Mathf.Sin(player.angle_y * Mathf.PI / 180) * speed * Convert.ToSingle(latency) / 1000 * Time.deltaTime;
        target.transform.position = new Vector3(player.x, player.y, player.z) + new Vector3(x, 0, z);
        target.transform.rotation = Quaternion.Euler(new Vector3(0, player.angle_y, 0));
    }

    void ActionPlayer(Player player)
    {
        Debug.Log(player.playerStateAttack);
        OtherPlayerController otherObject = GameObject.Find(player.nickname).GetComponent<OtherPlayerController>();
        if(otherObject)
        {
            otherObject.playerAction = (int)player.playerStateAttack;
        }
    }

    void OverLogin(Player player)
    {
        if(player.nickname == PlayerName)
        {
            GameObject.Find("Canvas").transform.FindChild("OverLogin").gameObject.SetActive(true);
        }
        else{
            Logout(player);
        }
    }

    void Logout(Player player)
    {
        Destroy(GameObject.Find(player.nickname));
    }

    void DeadReckoning(Player player)
    {
        OtherPlayerController otherObject = GameObject.Find(player.nickname).GetComponent<OtherPlayerController>();
        //otherObject.SetPlayerMove(player.playerMove);
        // otherObject.playerMove = (int)player.playerMove;
        if(otherObject)
        {
            otherObject.playerMove = (int)player.playerMove;
        }
    }

    void ConnectNewPlayer(Player player)
    {
        //씬전환, 데이터 남기기.
        SceneManager.LoadScene("SampleScene");
        Vector3 position = new Vector3(player.x, player.y, player.z);
        //Vector3 angle = new Vector3(player.angle_x, player.angle_y, player.angle_z);
        CreatePlayer(player.nickname, position, player.angle_y);
    }

    void ConnectOtherPlayer(Player player)
    {
        Vector3 position = new Vector3(player.x, player.y, player.z);
        //Vector3 angle = new Vector3(player.angle_x, player.angle_y, player.angle_z);
        CreateOtherPlayer(player.nickname, position, player.angle_y);

    }

    // Update is called once per frame
    void Update()
    {
        // if(ConnectionFlag)
        // {
        //     if(Time.time > nextTime){
        //         nextTime = Time.time + 3.0f;
        //         ConnectionPacket packet = new ConnectionPacket();
        //         packet.message = "connected";
        //         packet.nickname = PlayerName;
        //         var player = GameObject.Find(PlayerName);
        //         packet.x = player.transform.position.x;
        //         //packet.y = player.transform.position.y;
        //         packet.z = player.transform.position.z;
        //         packet.angle_y = player.transform.eulerAngles.y;
        //         SendPacket2CsServer(packet);
        //     }
        // }
    }

    public void SendPacket2Server(object obj)
    {
        byte[] userByte = ObjToByte(obj);
        //sock.Send(userByte, 0, userByte.Length, SocketFlags.None);
        sock.SendTo(userByte, serverEP);
    }

    public void SendPacket2CsServer(object obj)
    {
        byte[] userByte = ObjToByte(obj);
        sock.SendTo(userByte, userByte.Length, SocketFlags.None, CsServerEP);
        //Debug.Log(DateTimeOffset.Now.ToUnixTimeMilliseconds());
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
    public void CreatePlayer(string nickname, Vector3 position, float angle_y){
        GameObject obj = Instantiate(PlayerPrefab, position, Quaternion.Euler(new Vector3(0, 0, 0)));
        obj.transform.Rotate(new Vector3(0, angle_y, 0));
        obj.name = nickname;
        PlayerName = nickname;
        DontDestroyOnLoad(obj);
    }
    public void CreateOtherPlayer(string nickname, Vector3 position, float angle_y){
        GameObject obj = Instantiate(PlayerPrefab2, position, Quaternion.Euler(new Vector3(0, 0, 0)));
        obj.transform.Rotate(new Vector3(0, angle_y, 0));
        obj.name = nickname;
        DontDestroyOnLoad(obj);
    }
}