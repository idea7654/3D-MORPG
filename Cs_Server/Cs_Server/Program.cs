using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;

namespace Cs_Server
{
    public struct Player
    {
        public string message;
        public float x;
        public float y;
        public float z;
        public float angle_x;
        public float angle_y;
        public float angle_z;
        public double currentTime;
        public PlayerMove playerMove;
        public string nickname;
    };

    public enum PlayerMove
    {
        stop,
        turn_left,
        turn_right,
        moveFront,
        moveBack
    };
    
    public class Program
    {
        public static List<Address> players;
        private static ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(("127.0.0.1:6379,password=osm980811"));
        private const string SessionChannel = "Session"; // Can be anything we want.
        public static bool OverLogin = false;
        public static NetWork networkClass = new NetWork();//네트워크 클래스 사용할 객체 생성

        static void Main(string[] args)
        {
            NetWork useSocket = new NetWork(); //네트워크 클래스의 소켓을 사용하기 위함
            players = new List<Address>(); //서버에서 접속중인 유저들 정보를 담아두는 리스트

            Thread socketThread = new Thread(new ThreadStart(useSocket.Start)); //네트워크 클래스의 소켓설정하는 스레드(static함수로 한번실행 후 종료)
            socketThread.Start();
            Thread CheckConnection = new Thread(() => ConnectCheck()); //유저 접속중을 판단하는 스레드.
            CheckConnection.Start();

            var pubsub = connection.GetSubscriber(); //redis의 pub/sub설정
            pubsub.Subscribe(SessionChannel, (channel, message) => MessageAction(message,ref players)); //redis에서 pub가 날라오면(로그인이 수행되서 nodejs로부터) MessageAction함수실행
        }
        public static async Task SetInterval(Action action, TimeSpan timeout) //SetInterval 함수 만듬
        {
            await Task.Delay(timeout).ConfigureAwait(false);

            action();

            SetInterval(action, timeout);
        }

        private static void ConnectCheck()
        {
            SetInterval(() => players.ForEach(ConnectionCount), TimeSpan.FromSeconds(1)); //1초마다 접속해있는 모든 유저의 connectCheck값 1씩 감소, 0이 되면 접속해제
        }

        private static void ConnectionCount(Address address)
        {;
            address.connectCheck--;
            if(address.connectCheck < 0)
            {
                var find = players.FirstOrDefault(p => p.nickname == address.nickname);
                if(find != null)
                {
                    players.Remove(find);
                    networkClass.setPlayers(ref players);
                    Sql_LogOut(address.nickname);
                    players.ForEach(i => LogoutPacketToClient(i, address));
                    ConnectCheck();
                }
            }
            //연결끊김처리
        }

        private static void LogoutPacketToClient(Address address, Address targetAddress)
        {
            JObject packet = new JObject();
            packet.Add("message", "Logout");
            packet.Add("nickname", targetAddress.nickname);
            networkClass.SendPacket2Server(packet, address.address, address.port);
        }

        private static void Sql_LogOut(string nickname)
        {
            using (MySqlConnection connection = new MySqlConnection("Server=localhost;Port=3306;Database=MORPG;Uid=root;Pwd=osm980811"))
            {
                try
                {
                    connection.Open();
                    MySqlCommand command = new MySqlCommand("UPDATE Player SET onLogin=false WHERE nickname='" + nickname + "'", connection);
                    //로그아웃 시킬 유저를 DB에서 로그아웃
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        private static void MessageAction(RedisValue message, ref List<Address> players)
        {
            JObject userInfo = JObject.Parse(message);
            AddPlayerInWorld(userInfo, ref players);
        } //Node.js에서 유저 정보(nickname, IPAddress, Port)를 전송받아 JObject로 파싱 후 AddPlayerInWorld실행

        public static void AddPlayerInWorld(JObject userInfo, ref List<Address> players)
        {
            Address address = new Address(); //리스트에 넣을 Address객체 생성
            
            address.address = userInfo["address"].ToString();
            address.port = Int32.Parse(userInfo["port"].ToString());
            address.nickname = userInfo["nickname"].ToString();
            //Address객체의 속성값을 Node.js에서 넘어온 값으로 설정.
            Sql_CheckOverLogin(address.nickname); //이미 로그인이 되어있는지 체크(중복로그인 방지)
            if (OverLogin == false) //중복로그인이 아닐경우
            {
                string findOnMapQuery = "SELECT * FROM Player WHERE onLogin=true";
                SendOtherUsersPacket(address, findOnMapQuery); //현재 접속중인 모든 플레이어를 방금 접속한 플레이어한테만 보냄
                players.Add(address); //새로 접속한 플레이어를 월드 접속중에 추가
                networkClass.setPlayers(ref players); //네트워크 클래스의 players와 연동
                string query = "SELECT * FROM Player WHERE nickname='" + userInfo["nickname"].ToString() + "'";
                Sql_Read(players, address, query); // 새로 접속한 플레이어의 정보만을 db에서 찾아내고 모든 클라에 보냄
                Sql_ToOnLine(address.nickname);//접속해서 온라인상태로 변경
            }
            else //중복로그인일경우
            {
                Sql_LogOut_OverLogin(address.nickname, address.address, address.port);
            }
        }

        private static void Sql_LogOut_OverLogin(string nickname, string address, int port)
        {
            //중복로그인일경우에 실행됨
            using (MySqlConnection connection = new MySqlConnection("Server=localhost;Port=3306;Database=MORPG;Uid=root;Pwd=osm980811"))
            {
                try
                {
                    connection.Open();
                    MySqlCommand command = new MySqlCommand("UPDATE Player SET onLogin=false WHERE nickname='" + nickname + "'", connection);
                    //중복로그인 유저를 DB에서 onLogin을 false로 변경
                    command.ExecuteNonQuery();
                    var item = players.SingleOrDefault(x => x.nickname == nickname); //서버에 저장되어있는 유저목록에서 중복로그인에 해당하는 유저를 찾음
                    if(item != null)
                    {
                        players.Remove(item); // 그리고 리스트에서 삭제
                    }
                    JObject message = new JObject();
                    message.Add("message", "OverLogin");
                    message.Add("nickname", nickname);
                    players.ForEach(i =>
                    {
                        networkClass.SendPacket2Server(message, i.address, i.port);
                    });
                    //networkClass.SendPacket2Server(message, address, port); //클라에 중복로그인이었다는 메시지를 보냄
                    OverLogin = false; //중복로그인 해제
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        private static void Sql_CheckOverLogin(string nickname)
        {
            //중복로그인 체크
            using (MySqlConnection connection = new MySqlConnection("Server=localhost;Port=3306;Database=MORPG;Uid=root;Pwd=osm980811"))
            {
                try
                {
                    connection.Open();
                    MySqlCommand command = new MySqlCommand("SELECT COUNT(*) FROM Player WHERE onLogin=true AND nickname='" + nickname + "'", connection);
                    //방금 접속을 시도한 유저의 onLogin값이 true인지 DB에서 검색
                    var check = command.ExecuteScalar();
                    if(check.ToString() == "1")
                    {
                        OverLogin = true; //true일경우에 OverLogin을 true로 변경.
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
                
            }
        }
        
        private static void SendOtherUsersPacket(Address address, string query)
        {
            //처음 접속한 유저를 모든 유저에게 현재 접속되어있는 모든 유저들의 데이터를 보냄
            using (MySqlConnection connection = new MySqlConnection("Server=localhost;Port=3306;Database=MORPG;Uid=root;Pwd=osm980811"))
            {
                try
                {
                    connection.Open();

                    MySqlCommand command = new MySqlCommand(query, connection);
                    MySqlDataReader table = command.ExecuteReader();
                    Util util = new Util();
                    while (table.Read())
                    {
                        //Console.WriteLine(table);
                        JObject json = util.CreateJson("OtherPlayers", table);
                        networkClass.SendPacket2Server(json, address.address, address.port);
                    }
                    table.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("실패");
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        private static void Sql_ToOnLine(string nickname)
        {
            //처음 접속한 유저를 온라인상태로 변경(DB에서)
            using (MySqlConnection connection = new MySqlConnection("Server=localhost;Port=3306;Database=MORPG;Uid=root;Pwd=osm980811"))
            {
                string query = "UPDATE Player SET onLogin=true WHERE nickname='" + nickname + "'";
                try
                {
                    connection.Open();
                    MySqlCommand command = new MySqlCommand(query, connection);
                    if (command.ExecuteNonQuery() == 1)
                    {
                        //Console.WriteLine("수정 성공");
                    }
                    else
                    {
                        Console.WriteLine("인서트 실패");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("실패");
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        public static void Sql_SaveUser(JObject player)
        {
            using (MySqlConnection connection = new MySqlConnection("Server=localhost;Port=3306;Database=MORPG;Uid=root;Pwd=osm980811"))
            {
                try
                {
                    connection.Open();
                    float x = (float)player["x"];
                    MySqlCommand command = new MySqlCommand("UPDATE Player SET x='" + x + "',y='" + player["y"] + "',z='" + player["z"] + "',angle_y='" + player["angle_y"]
                        + "' WHERE nickname='"+ player["nickname"] + "'", connection);
                    command.ExecuteNonQuery();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        private static void Sql_Read(List<Address> players, Address address, string query)
        {
            //처음 접속한 유저의 정보를 접속되어있는 모든 플레이어에게 보냄
            using (MySqlConnection connection = new MySqlConnection("Server=localhost;Port=3306;Database=MORPG;Uid=root;Pwd=osm980811"))
            {
                try
                {
                    connection.Open();
                    
                    MySqlCommand command = new MySqlCommand(query, connection);
                    MySqlDataReader table = command.ExecuteReader();
                    
                    while (table.Read())
                    {
                        //소켓으로 데이터 전송

                        players.ForEach(ConnectNewUser);
                        void ConnectNewUser(Address s)
                        {
                            if(s.address == address.address && s.port == address.port) //서버에 접속중인 플레이어들 중 자신이라면
                            {
                                Util util = new Util();
                                JObject packet = util.CreateJson("Connect", table); //Connect메시지로 보내고
                                networkClass.SendPacket2Server(packet, s.address, s.port);
                            }
                            else //서버에 접속중인 플레이어들 중 자신이 아니라면
                            {
                                //otherjson.Add("message", "OtherPlayers");
                                Util util = new Util();
                                JObject packet = util.CreateJson("OtherPlayers", table); //OtherPlayers메시지로 보냄
                                networkClass.SendPacket2Server(packet, s.address, s.port);
                            }
                        }
                    }
                    table.Close(); 
                }
                catch (Exception ex)
                {
                    Console.WriteLine("실패");
                    Console.WriteLine(ex.ToString());
                }

            }
        }
    }

    public class Util
    {
        public JObject CreateJson(string message, MySqlDataReader table) //이동, 접속 등에 사용되며 공통된 Player정보를 유틸화함.
        {
            var otherjson = new JObject();
            otherjson.Add("nickname", table["nickname"].ToString());
            otherjson.Add("x", Single.Parse(table["x"].ToString()));
            otherjson.Add("y", Single.Parse(table["y"].ToString()));
            otherjson.Add("z", Single.Parse(table["z"].ToString()));
            otherjson.Add("angle_x", Single.Parse(table["angle_x"].ToString()));
            otherjson.Add("angle_y", Single.Parse(table["angle_y"].ToString()));
            otherjson.Add("angle_z", Single.Parse(table["angle_z"].ToString()));
            otherjson.Add("exp", Int32.Parse(table["exp"].ToString()));
            otherjson.Add("map", Int32.Parse(table["map"].ToString()));
            otherjson.Add("message", message);
            return otherjson;
        }
    }
    
    public class Address
    {
        public string address;
        public int port;
        public string nickname;
        public int connectCheck = 5;
    } //서버에 접속중인 리스트 클래스
    public class NetWork
    {
        byte[] recvByte = new byte[512];
        string strIP = "127.0.0.1";
        int port = 8000;
        public static Socket sock;
        IPAddress ip;
        IPEndPoint endPoint;
        Queue<string> Buffer = new Queue<string>();
        object buffer_lock = new object(); //queue충돌 방지용 lock
        public Thread moveThread;
        public static List<Address> players;
        EndPoint bindPoint;
        //기본 소켓 구조

        public void Start()
        {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            ip = IPAddress.Parse(strIP);
            endPoint = new IPEndPoint(ip, port);
            bindPoint = (EndPoint)endPoint;
            sock.Bind(endPoint);
            /*
            while (true)
            {
                try
                {
                    sock.Receive(recvByte, 0, recvByte.Length, SocketFlags.None);
                    string recvString = Encoding.UTF8.GetString(recvByte);
                    recvString = recvString.Replace("\0", string.Empty);

                    lock (buffer_lock)
                    { //큐 충돌방지
                        Buffer.Enqueue(recvString); //큐에 버퍼 저장
                    }
                    System.Array.Clear(recvByte, 0, recvByte.Length); //버퍼를 사용후 초기화
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            } //기존의 동기방식
            */
            //sock.Connect(endPoint);
            sock.BeginReceiveFrom(recvByte, 0, recvByte.Length, SocketFlags.None, ref bindPoint, new AsyncCallback(RecvCallBack), recvByte);
            try
            {
                while(true)
                {

                }
            }
            catch(SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void RecvCallBack(IAsyncResult result)
        {
            try
            {
                int size = sock.EndReceiveFrom(result, ref bindPoint);

                if(size > 0)
                {
                    byte[] recvBuffer = new byte[512];
                    recvBuffer = (byte[])result.AsyncState;

                    //로직 처리
                    string recvString = Encoding.UTF8.GetString(recvBuffer);
                    recvString = recvString.Replace("\0", string.Empty);

                    //lock (buffer_lock)
                    //{ //큐 충돌방지
                    //    Buffer.Enqueue(recvString); //큐에 버퍼 저장
                    //}
                    buffer_update(recvString);
                    System.Array.Clear(recvByte, 0, recvByte.Length); //버퍼를 사용후 초기화
                }
                sock.BeginReceiveFrom(recvByte, 0, recvByte.Length, SocketFlags.None, ref bindPoint, new AsyncCallback(RecvCallBack), recvByte);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void buffer_update(string recvString)
        {
            try
            {
                string b = recvString;
                //lock (buffer_lock)
                //{
                    //b = Buffer.Dequeue();
                //}
                JObject player = JObject.Parse(b);
                if(player["message"].ToString() == "connected")
                {
                    //Console.WriteLine(player["nickname"].ToString());
                    Address result = players.Find(x => x.nickname == player["nickname"].ToString());
                    result.connectCheck = 5;
                    //검사하면서 유저위치 디비에 업데이트
                    Program.Sql_SaveUser(player);
                }else if(player["message"].ToString() == "Chatting")
                {
                    players.ForEach(i =>
                    {
                        SendPacket2Server(player, i.address, i.port);
                    });
                }
                else
                {
                    moveThread = new Thread(() => PlayerMove(player));
                    moveThread.Start();
                }
            }
            catch(Exception ex)
            {
            Console.WriteLine(ex);
            }
        }

        public void setPlayers(ref List<Address> playersArr)
        {
            players = playersArr;
        }

        public void PlayerMove(JObject player)
        {
            //클라에 값 전달
            players.ForEach((address) => SendToAllClient(address, player));
            moveThread.Interrupt();
        }

        private void SendToAllClient(Address address, JObject player)
        {
            SendPacket2Server(player, address.address, address.port);
        }

        public void SendPacket2Server(JObject obj, string targetIP, int targetPort)
        {
            byte[] userByte = ObjToByte(obj);
            IPEndPoint sender = new IPEndPoint(IPAddress.Parse(targetIP), targetPort);
            EndPoint remote = (EndPoint)sender;
            sock.SendTo(userByte, remote);
        }

        private byte[] ObjToByte(JObject obj)
        {
            string json = obj.ToString();
            byte[] returnValue = Encoding.UTF8.GetBytes(json);
            return returnValue;
        }
    }
}
