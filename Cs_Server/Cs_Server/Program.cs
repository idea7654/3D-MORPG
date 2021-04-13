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
    class Program
    {
        private static ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(("127.0.0.1:6379,password=osm980811"));

        private const string SessionChannel = "Session"; // Can be anything we want.

        static void Main(string[] args)
        {
            //Console.Write("Enter your name: ");
            //userName = Console.ReadLine();
            //Sql_Insert();
            NetWork useSocket = new NetWork();
            useSocket.Start();
            // Create pub/sub
            var pubsub = connection.GetSubscriber();

            // Subscriber subscribes to a channel
            pubsub.Subscribe(SessionChannel, (channel, message) => MessageAction(message));

            // Notify subscriber(s) if you're joining
            //pubsub.Publish(SessionChannel, $"'{userName}' joined the chat room.");

            // Messaging here
            while (true)
            {
                //pubsub.Publish(SessionChannel, "보낼 내용");
            }
        }

        private static void MessageAction(RedisValue message)
        {
            JObject userInfo = JObject.Parse(message);
            AddPlayerInWorld(userInfo);
        }

        private static void AddPlayerInWorld(JObject userInfo)
        {
            List<Address> players = new List<Address>();
            Address address = new Address();
            address.address = userInfo["address"].ToString();
            address.port = Int32.Parse(userInfo["port"].ToString());
            address.nickname = userInfo["nickname"].ToString();
            string findOnMapQuery = "SELECT * FROM Player WHERE onLogin=true";
            SendOtherUsersPacket(address, findOnMapQuery); //현재 접속중인 모든 플레이어를 방금 접속한 플레이어한테만 보냄
            players.Add(address); //새로 접속한 플레이어를 월드 접속중에 추가
            string query = "SELECT * FROM Player WHERE nickname='" + userInfo["nickname"].ToString() + "'";
            Sql_Read(players, address, query); // 새로 접속한 플레이어의 정보만을 db에서 찾아내고 모든 클라에 보냄
            Sql_ToOnLine(address.nickname);//접속해서 온라인상태로 변경
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
                    while (table.Read())
                    {
                        var json = new JObject();
                        json.Add("nickname", table["nickname"].ToString());
                        json.Add("x", Int32.Parse(table["x"].ToString()));
                        json.Add("y", Int32.Parse(table["y"].ToString()));
                        json.Add("z", Int32.Parse(table["z"].ToString()));
                        json.Add("angle_x", Int32.Parse(table["angle_x"].ToString()));
                        json.Add("angle_y", Int32.Parse(table["angle_y"].ToString()));
                        json.Add("angle_z", Int32.Parse(table["angle_z"].ToString()));
                        json.Add("exp", Int32.Parse(table["exp"].ToString()));
                        json.Add("map", Int32.Parse(table["map"].ToString()));
                        json.Add("message", "OtherPlayers");
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
                        var json = new JObject();
                        json.Add("nickname", table["nickname"].ToString());
                        json.Add("x", Int32.Parse(table["x"].ToString()));
                        json.Add("y", Int32.Parse(table["y"].ToString()));
                        json.Add("z", Int32.Parse(table["z"].ToString()));
                        json.Add("angle_x", Int32.Parse(table["angle_x"].ToString()));
                        json.Add("angle_y", Int32.Parse(table["angle_y"].ToString()));
                        json.Add("angle_z", Int32.Parse(table["angle_z"].ToString()));
                        json.Add("exp", Int32.Parse(table["exp"].ToString()));
                        json.Add("map", Int32.Parse(table["map"].ToString()));
                        //json.Add("message", "Connect");

                        players.ForEach(ConnectNewUser);
                        void ConnectNewUser(Address s)
                        {
                            if(s.address == address.address && s.port == address.port)
                            {
                                json.Add("message", "Connect");
                                useSocket.SendPacket2Server(json, s.address, s.port);
                            }
                            else
                            {
                                json.Add("message", "OtherPlayers");
                                useSocket.SendPacket2Server(json, s.address, s.port);
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
    public class Address
    {
        public string address;
        public int port;
        public string nickname;
    }
    public class NetWork
    {
        byte[] recvByte = new byte[1024];
        Thread ServerCheck_thread;
        Queue<string> Buffer = new Queue<string>();
        string strIP = "127.0.0.1";
        int port = 8000;
        public static Socket sock;
        IPAddress ip;
        IPEndPoint endPoint;
        object buffer_lock = new object();

        public void Start()
        {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            ip = IPAddress.Parse(strIP);
            endPoint = new IPEndPoint(ip, port);
            sock.Bind(endPoint);
            //sock.Connect(endPoint);
        }

        public void SendPacket2Server(JObject obj, string targetIP, int targetPort)
        {
            byte[] userByte = ObjToByte(obj);
            IPEndPoint sender = new IPEndPoint(IPAddress.Parse(targetIP), targetPort);
            EndPoint remote = (EndPoint)sender;
            //sock.Connect(remote);
            Console.WriteLine(port);
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
