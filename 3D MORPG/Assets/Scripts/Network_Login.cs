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
using UnityEngine.UI;
using System.Linq;
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

    public class EnemyPacket{
        public string target;
        public string message;
        public double x;
        public double z;
        public double angle_y;
        public string state;
        public string id;
        public float PlayerHp;
        public bool attack = false;
    };
    [Serializable]
    public class MemberList{
        public string nickname;
        public float hp;
    }
    [Serializable]
    public class PartyPacket{
        public string member;
        public string leader;
        public string message;
        public float memberHP;
        public float leaderHP;
        public MemberList[] memberList;
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
    public bool isLeader = false;
    public bool isInParty = false;

    public List<MemberList> PartyMembers = new List<MemberList>();
    #endregion
    // Start is called before the first frame update
    public string PlayerName;
    private long sampleTimer;
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
        // while(true)
        // {
        //     sock.Receive(recvByte, 0, recvByte.Length, SocketFlags.None);//서버에서 온 패킷을 버퍼에 담기
        //     string t = Encoding.Default.GetString(recvByte); //큐에 버퍼를 넣을 준비
        //     t = t.Replace("\0", string.Empty); //버퍼 마지막에 공백이 있는지 검색하고 공백을 삭제
        //     lock(buffer_lock){ //큐 충돌방지
        //        Buffer.Enqueue(t); //큐에 버퍼 저장
        //     }
        //     System.Array.Clear(recvByte, 0, recvByte.Length); //버퍼를 사용후 초기화
        // }
        sock.BeginReceiveFrom(recvByte, 0, recvByte.Length, SocketFlags.None, ref remoteEP, new AsyncCallback(RecvCallBack), recvByte);

        void RecvCallBack(IAsyncResult result)
        {
            sampleTimer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            try
            {
                int size = sock.EndReceiveFrom(result, ref remoteEP);

                if(size > 0)
                {
                    byte[] recvBuffer = new byte[512];
                    recvBuffer = (byte[])result.AsyncState;

                    //로직 처리
                    string recvString = Encoding.UTF8.GetString(recvBuffer);
                    recvString = recvString.Replace("\0", string.Empty);

                    lock (buffer_lock)
                    { //큐 충돌방지
                       Buffer.Enqueue(recvString); //큐에 버퍼 저장
                    }
                    System.Array.Clear(recvByte, 0, recvByte.Length); //버퍼를 사용후 초기화
                }
                sock.BeginReceiveFrom(recvByte, 0, recvByte.Length, SocketFlags.None, ref remoteEP, new AsyncCallback(RecvCallBack), recvByte);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
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

    public void ConnectPacket()
    {
        //while(true)
        //{
            Player packet = new Player();
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
                    //InvokeRepeating("ConnectPacket", 1f, 0.5f);
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
                case "Chase":
                    EnemyAI(b);
                    break;
                case "Attack":
                    EnemyAI_Attack(b);
                    break;
                case "EnemyIdle":
                    EnemyAI_ToIdle(b);
                    break;
                case "EnemyDie":
                    EnemyDie(b);
                    break;
                case "PlayerAttackToEnemy":
                    EnemyEffect(b);
                    break;
                case "CreateParty":
                    CreateParty(b);
                    break;
                case "AddMember":
                    AddPartyMember(b);
                    break;
                case "AddedParty":
                    AddedParty(b);
                    break;
                case "ArriveParty":
                    ArriveParty(b);
                    break;
                case "AlreadyInParty":
                    AlreadyInParty(connectPlayer);
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
        DeadReckoning(player);
    }

    void AlreadyInParty(Player player){
        GameObject.Find("AlertCanvas").transform.FindChild("InPartyPanel").gameObject.SetActive(true);
    }

    void AddedParty(string b){
        PartyPacket partyPacket = JsonUtility.FromJson<PartyPacket>(b);
        GameObject PartyCanvas = GameObject.Find("PartyCanvas");
        PartyCanvas.transform.FindChild("PartyPanel").gameObject.SetActive(true);
        for(int i = 1; i < partyPacket.memberList.Length; i++){
            PartyCanvas.transform.FindChild("PartyPanel").transform.FindChild("User" + i.ToString()).gameObject.SetActive(true);
            Transform member = GameObject.Find("User" + i.ToString()).transform;
            member.FindChild("HPBar").GetComponent<Slider>().value = partyPacket.memberList[i-1].hp / 100;
            member.FindChild("Text").GetComponent<Text>().text = partyPacket.memberList[i-1].nickname;
            PartyMembers.Add(partyPacket.memberList[i-1]);
        }
        isInParty = true;
    }

    void ArriveParty(string b){
        PartyPacket partyPacket = JsonUtility.FromJson<PartyPacket>(b);
        GameObject PartyCanvas = GameObject.Find("PartyCanvas");
        string target = partyPacket.member;
        if(target == PlayerName){
            PartyMembers = new List<MemberList>();
            PartyCanvas.transform.FindChild("PartyPanel").gameObject.SetActive(false);
            isInParty = false;
        }else{
            PartyMembers = new List<MemberList>();
            PartyMembers.AddRange(partyPacket.memberList);
            PartyMembers.RemoveAll(i => i.nickname == PlayerName);
            for(int i = 0; i < PartyMembers.Count; i++){
                Transform member = GameObject.Find("User" + (i + 1).ToString()).transform;
                member.FindChild("HPBar").GetComponent<Slider>().value = PartyMembers[i].hp / 100;
                member.FindChild("Text").GetComponent<Text>().text = PartyMembers[i].nickname;
            }
            PartyCanvas.transform.FindChild("PartyPanel").transform.FindChild("User" + (PartyMembers.Count + 1).ToString()).gameObject.SetActive(false);
        }
    }

    void CreateParty(string b){
        PartyPacket partyPacket = JsonUtility.FromJson<PartyPacket>(b);
        //여기서 파티창 보이게 하고 멤버만 추가!!!
        GameObject PartyCanvas = GameObject.Find("PartyCanvas");
        PartyCanvas.transform.FindChild("PartyPanel").gameObject.SetActive(true);
        PartyCanvas.transform.FindChild("PartyPanel").transform.FindChild("User1").gameObject.SetActive(true);
        Transform member = GameObject.Find("User1").transform;
        isInParty = true;
        if(PlayerName == partyPacket.leader){
            isLeader = true;
            member.FindChild("HPBar").GetComponent<Slider>().value = partyPacket.memberHP / 100;
            member.FindChild("Text").GetComponent<Text>().text = partyPacket.member;
            MemberList memberList = new MemberList();
            memberList.nickname = partyPacket.member;
            memberList.hp = partyPacket.memberHP;
            PartyMembers.Add(memberList);
        }else{
            member.FindChild("HPBar").GetComponent<Slider>().value = partyPacket.leaderHP / 100;
            member.FindChild("Text").GetComponent<Text>().text = partyPacket.leader;
            MemberList leaderList = new MemberList();
            leaderList.nickname = partyPacket.leader;
            leaderList.hp = partyPacket.leaderHP;
            PartyMembers.Add(leaderList);
        }
    }

    void AddPartyMember(string b){
        PartyPacket partyPacket = JsonUtility.FromJson<PartyPacket>(b);
        GameObject PartyPanel = GameObject.Find("PartyPanel");
        MemberList memberList = new MemberList();
        memberList.nickname = partyPacket.member;
        memberList.hp = partyPacket.memberHP;
        PartyMembers.Add(memberList);

        for(int i = 1; i < PartyMembers.Count + 1; i++){
            PartyPanel.transform.FindChild("User" + i.ToString()).gameObject.SetActive(true);
        }
        Transform member = GameObject.Find("User" + (PartyMembers.Count).ToString()).transform;
        member.FindChild("HPBar").GetComponent<Slider>().value = partyPacket.memberHP / 100;
        member.FindChild("Text").GetComponent<Text>().text = partyPacket.member;
    }

    void EnemyEffect(string b)
    {
        EnemyPacket enemyPacket = JsonUtility.FromJson<EnemyPacket>(b);
        GameObject Enemy = GameObject.Find(enemyPacket.id);
        
        Enemy.GetComponent<EnemyFSM>().ShowHitEffect();
    }

    void EnemyDie(string b)
    {
        EnemyPacket enemyPacket = JsonUtility.FromJson<EnemyPacket>(b);
        GameObject Enemy = GameObject.Find(enemyPacket.id);

        Destroy(Enemy);
    }

    void EnemyAI(string b)
    {
        EnemyPacket enemyPacket = JsonUtility.FromJson<EnemyPacket>(b);
        GameObject Enemy = GameObject.Find(enemyPacket.id);
        //Enemy.transform.position = new Vector3(Convert.ToSingle(enemyPacket.x), 0, Convert.ToSingle(enemyPacket.z));
        //Vector3 newPosition = new Vector3(Convert.ToSingle(enemyPacket.x), 0, Convert.ToSingle(enemyPacket.z));
        //Enemy.transform.position = Vector3.MoveTowards(newPosition, Enemy.transform.position, 1f * Time.deltaTime);
        //Enemy.transform.rotation = Quaternion.Euler(new Vector3(0, Convert.ToSingle(enemyPacket.angle_y) + 180, 0));
        // if(Vector3.Distance(Enemy.transform.position, new Vector3(Convert.ToSingle(enemyPacket.x), 0, Convert.ToSingle(enemyPacket.z))) > 1f){
        //    Debug.Log("이탈!!");
        // }
        Enemy.GetComponent<EnemyFSM>().enemyState = enemyPacket.message;
        Enemy.GetComponent<EnemyFSM>().target = enemyPacket.target;
        Enemy.GetComponent<EnemyFSM>().attack = enemyPacket.attack;
        Enemy.GetComponent<EnemyFSM>().moveAngle = Convert.ToSingle(enemyPacket.angle_y);
    }

    void EnemyAI_ToIdle(string b)
    {
        EnemyPacket enemyPacket = JsonUtility.FromJson<EnemyPacket>(b);
        GameObject Enemy = GameObject.Find(enemyPacket.id);
        //Enemy.transform.position = new Vector3(Convert.ToSingle(enemyPacket.x), 0, Convert.ToSingle(enemyPacket.z));
        Vector3 newPosition = new Vector3(Convert.ToSingle(enemyPacket.x), 0, Convert.ToSingle(enemyPacket.z));
        //Enemy.transform.position = Vector3.MoveTowards(Enemy.transform.position, newPosition, 1.5f * Time.deltaTime);
        //Enemy.transform.rotation = Quaternion.Euler(new Vector3(0, Convert.ToSingle(enemyPacket.angle_y) + 180, 0));
        //if(Vector3.Distance(Enemy.transform.position, new Vector3(Convert.ToSingle(enemyPacket.x), 0, Convert.ToSingle(enemyPacket.z))) > 0.5f){
        //   Debug.Log("이탈!!");
        //}
        Enemy.GetComponent<EnemyFSM>().targetPosition = newPosition;
        Enemy.GetComponent<EnemyFSM>().enemyState = enemyPacket.message;
        Enemy.GetComponent<EnemyFSM>().attack = enemyPacket.attack;
        Enemy.GetComponent<EnemyFSM>().moveAngle = Convert.ToSingle(enemyPacket.angle_y);
    }

    void EnemyAI_Attack(string b)
    {
        EnemyPacket enemyPacket = JsonUtility.FromJson<EnemyPacket>(b);
        GameObject Enemy = GameObject.Find(enemyPacket.id);
        if(enemyPacket.attack){
            if(enemyPacket.target == PlayerName){
                Slider slider = GameObject.Find("HPBar").GetComponent<Slider>();
                slider.value = enemyPacket.PlayerHp / 100;
            }else{
                // for(int i = 1; i < PartyMembers.Count; i++){
                //     Transform member = GameObject.Find("User" + i.ToString()).transform;
                //     if(member.FindChild("Text").GetComponent<Text>().text == enemyPacket.target){
                //         member.FindChild("HPBar").GetComponent<Slider>().value = enemyPacket.PlayerHp / 100;
                //         break;
                //     }
                // }
                if(isInParty){
                    // int index = PartyMembers.FindIndex(i => i == enemyPacket.target);
                    // Transform member = GameObject.Find("User" + (index + 1).ToString()).transform;
                    // member.FindChild("HPBar").GetComponent<Slider>().value = enemyPacket.PlayerHp / 100;
                    for(int i = 1; i < PartyMembers.Count + 1; i++){
                        Transform member = GameObject.Find("User" + i.ToString()).transform;
                        if(member.FindChild("Text").GetComponent<Text>().text == enemyPacket.target){
                            member.FindChild("HPBar").GetComponent<Slider>().value = enemyPacket.PlayerHp / 100;
                            break;
                        }
                    }
                }
            }
        }
        //Enemy.transform.position = new Vector3(Convert.ToSingle(enemyPacket.x), 0, Convert.ToSingle(enemyPacket.z));
        Vector3 newPosition = new Vector3(Convert.ToSingle(enemyPacket.x), 0, Convert.ToSingle(enemyPacket.z));
        //Enemy.transform.position = Vector3.MoveTowards(Enemy.transform.position, newPosition, 1.5f * Time.deltaTime);
        Enemy.GetComponent<EnemyFSM>().targetPosition = newPosition;
        Enemy.GetComponent<EnemyFSM>().enemyState = enemyPacket.message;
        Enemy.GetComponent<EnemyFSM>().attack = enemyPacket.attack;
        Enemy.GetComponent<EnemyFSM>().target = enemyPacket.target;
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
        Vector3 targetVector = new Vector3(player.x, player.y, player.z) + new Vector3(x, 0, z);
        target.transform.position = Vector3.Lerp(target.transform.position, targetVector, Time.deltaTime);
        target.transform.rotation = Quaternion.Euler(new Vector3(0, player.angle_y, 0));
    }

    void ActionPlayer(Player player)
    {
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

    }

    public void SendPacket2Server(object obj)
    {
        byte[] userByte = ObjToByte(obj);
        sock.SendTo(userByte, serverEP);
    }

    public void SendPacket2CsServer(object obj)
    {
        byte[] userByte = ObjToByte(obj);
        sock.SendTo(userByte, userByte.Length, SocketFlags.None, CsServerEP);
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
        //CameraController camera = GameObject.Find("Main Camera").GetComponent<CameraController>();
        //camera.player = nickname;
        DontDestroyOnLoad(obj);
    }
    public void CreateOtherPlayer(string nickname, Vector3 position, float angle_y){
        GameObject obj = Instantiate(PlayerPrefab2, position, Quaternion.Euler(new Vector3(0, 0, 0)));
        obj.transform.Rotate(new Vector3(0, angle_y, 0));
        obj.name = nickname;
        DontDestroyOnLoad(obj);
    }
}