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
        private static ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(("127.0.0.1:6379,password=osm980811"));

        private const string SessionChannel = "Session"; // Can be anything we want.
        public static bool OverLogin = false;
        
        static void Main(string[] args)
        {
            //Console.Write("Enter your name: ");
            //userName = Console.ReadLine();
            //Sql_Insert();
            NetWork useSocket = new NetWork();
            Thread socketThread = new Thread(new ThreadStart(useSocket.Start));
            socketThread.Start();
            Thread BufferThread = new Thread(new ThreadStart(useSocket.buffer_update));
            BufferThread.Start();
            List<Address> players = new List<Address>();
            // Create pub/sub
            var pubsub = connection.GetSubscriber();

            // Subscriber subscribes to a channel
            pubsub.Subscribe(SessionChannel, (channel, message) => MessageAction(message,ref players));

            // Notify subscriber(s) if you're joining
            //pubsub.Publish(SessionChannel, $"'{userName}' joined the chat room.");

            // Messaging here
            while (true)
            {
                
            }
        }

        private static void MessageAction(RedisValue message, ref List<Address> players)
        {
            JObject userInfo = JObject.Parse(message);
            AddPlayerInWorld(userInfo, ref players);
        }

        public static void AddPlayerInWorld(JObject userInfo, ref List<Address> players)
        {
            Address address = new Address();
            NetWork networkClass = new NetWork();
            
            address.address = userInfo["address"].ToString();
            address.port = Int32.Parse(userInfo["port"].ToString());
            address.nickname = userInfo["nickname"].ToString();
            Sql_CheckOverLogin(address.nickname);
            if (OverLogin == false)
            {
                string findOnMapQuery = "SELECT * FROM Player WHERE onLogin=true";
                SendOtherUsersPacket(address, findOnMapQuery); //현재 접속중인 모든 플레이어를 방금 접속한 플레이어한테만 보냄
                players.Add(address); //새로 접속한 플레이어를 월드 접속중에 추가
                networkClass.setPlayers(players);
                string query = "SELECT * FROM Player WHERE nickname='" + userInfo["nickname"].ToString() + "'";
                Sql_Read(players, address, query); // 새로 접속한 플레이어의 정보만을 db에서 찾아내고 모든 클라에 보냄
                Sql_ToOnLine(address.nickname);//접속해서 온라인상태로 변경
            }
            else
            {
                //Console.WriteLine("중복로그인입니다!!!!");
                Sql_LogOut(address.nickname, address.address, address.port);
            }
        }

        private static void Sql_LogOut(string nickname, string address, int port)
        {
            NetWork useSocket = new NetWork();
            using (MySqlConnection connection = new MySqlConnection("Server=localhost;Port=3306;Database=MORPG;Uid=root;Pwd=osm980811"))
            {
                try
                {
                    connection.Open();
                    MySqlCommand command = new MySqlCommand("UPDATE Player SET onLoing=false WHERE nickname='" + nickname + "'", connection);
                    JObject message = new JObject();
                    message.Add("message", "OverLogin");
                    useSocket.SendPacket2Server(message, address, port);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        private static void Sql_CheckOverLogin(string nickname)
        {
            using (MySqlConnection connection = new MySqlConnection("Server=localhost;Port=3306;Database=MORPG;Uid=root;Pwd=osm980811"))
            {
                try
                {
                    connection.Open();
                    MySqlCommand command = new MySqlCommand("SELECT COUNT(*) FROM Player WHERE onLogin=true AND nickname='" + nickname + "'", connection);
                    //int check = (int)command.ExecuteScalar();
                    var check = command.ExecuteScalar();
                    if(check.ToString() == "1")
                    {
                        OverLogin = true;
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
            NetWork useSocket = new NetWork();
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
                        Console.WriteLine(table);
                        JObject json = util.CreateJson("OtherPlayers", table);
                        useSocket.SendPacket2Server(json, address.address, address.port);
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
            using (MySqlConnection connection = new MySqlConnection("Server=localhost;Port=3306;Database=MORPG;Uid=root;Pwd=osm980811"))
            {
                string query = "UPDATE Player SET onLogin=true WHERE nickname='" + nickname + "'";
                try
                {
                    connection.Open();
                    MySqlCommand command = new MySqlCommand(query, connection);
                    if (command.ExecuteNonQuery() == 1)
                    {
                        Console.WriteLine("수정 성공");
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

        private static void Sql_Read(List<Address> players, Address address, string query)
        {
            using (MySqlConnection connection = new MySqlConnection("Server=localhost;Port=3306;Database=MORPG;Uid=root;Pwd=osm980811"))
            {
                try//예외 처리
                {
                    connection.Open();
                    
                    MySqlCommand command = new MySqlCommand(query, connection);
                    MySqlDataReader table = command.ExecuteReader();

                    // 만약에 내가처리한 Mysql에 정상적으로 들어갔다면 메세지를 보여주라는 뜻이다
                    while (table.Read())
                    {
                        //소켓으로 데이터 전송
                        //접속되어있는 모든 인원한테.
                        NetWork useSocket = new NetWork();

                        players.ForEach(ConnectNewUser);
                        void ConnectNewUser(Address s)
                        {
                            if(s.address == address.address && s.port == address.port)
                            {
                                Util util = new Util();
                                JObject packet = util.CreateJson("Connect", table);
                                useSocket.SendPacket2Server(packet, s.address, s.port);
                            }
                            else
                            {
                                //otherjson.Add("message", "OtherPlayers");
                                Util util = new Util();
                                JObject packet = util.CreateJson("OtherPlayers", table);
                                useSocket.SendPacket2Server(packet, s.address, s.port);
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
        public JObject CreateJson(string message, MySqlDataReader table)
        {
            var otherjson = new JObject();
            otherjson.Add("nickname", table["nickname"].ToString());
            otherjson.Add("x", Int32.Parse(table["x"].ToString()));
            otherjson.Add("y", Int32.Parse(table["y"].ToString()));
            otherjson.Add("z", Int32.Parse(table["z"].ToString()));
            otherjson.Add("angle_x", Int32.Parse(table["angle_x"].ToString()));
            otherjson.Add("angle_y", Int32.Parse(table["angle_y"].ToString()));
            otherjson.Add("angle_z", Int32.Parse(table["angle_z"].ToString()));
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
    }
    public class NetWork
    {
        byte[] recvByte = new byte[1024];
        string strIP = "127.0.0.1";
        int port = 8000;
        public static Socket sock;
        IPAddress ip;
        IPEndPoint endPoint;
        Queue<string> Buffer = new Queue<string>();
        object buffer_lock = new object(); //queue충돌 방지용 lock
        public Thread moveThread;
        public static List<Address> players;

        public void Start()
        {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            ip = IPAddress.Parse(strIP);
            endPoint = new IPEndPoint(ip, port);
            sock.Bind(endPoint);
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
            }
            //sock.Connect(endPoint);
        }

        public void buffer_update()
        {
            while (true)
            {
                while (Buffer.Count != 0)
                {
                    string b = null;
                    lock (buffer_lock)
                    {
                        b = Buffer.Dequeue();
                    }
                    JObject player = JObject.Parse(b);
                    //Console.WriteLine(player);
                    moveThread = new Thread(() => PlayerMove(player));
                    moveThread.Start();
                }
            }
        }

        public void setPlayers(List<Address> playersArr)
        {
            players = playersArr;
        }

        public void PlayerMove(JObject player)
        {
            //디비에 저장?
            //클라에 값 전달
            //SendPacket2Server(player, )
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
