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
            players.Add(address);
            Sql_Read(userInfo["nickname"].ToString(), players);
        }

        private static void Sql_Insert()
        {
            using (MySqlConnection connection = new MySqlConnection("Server=localhost;Port=3306;Database=MORPG;Uid=root;Pwd=osm980811"))
            {
                string insertQuery = "INSERT INTO Player(nickname,exp,map,x,y,z,angle_x,angle_y,angle_z) VALUES('이데아',0,0,0.0,0.0,0.0,0.0,180.0,0.0)";
                try//예외 처리
                {
                    connection.Open();
                    MySqlCommand command = new MySqlCommand(insertQuery, connection);

                    // 만약에 내가처리한 Mysql에 정상적으로 들어갔다면 메세지를 보여주라는 뜻이다
                    if (command.ExecuteNonQuery() == 1)
                    {
                        Console.WriteLine("인서트 성공");
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
        private static void Sql_Read(string nickname, List<Address> players)
        {
            using (MySqlConnection connection = new MySqlConnection("Server=localhost;Port=3306;Database=MORPG;Uid=root;Pwd=osm980811"))
            {
                try//예외 처리
                {
                    connection.Open();
                    string insertQuery = "SELECT * FROM Player WHERE nickname='" + nickname + "'";
                    MySqlCommand command = new MySqlCommand(insertQuery, connection);
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
                        players.ForEach(SendPacket);
                        void SendPacket(Address s)
                        {
                            //Console.WriteLine(s.port);
                            useSocket.SendPacket2Server(json, s.address, s.port);
                        }
                        //useSocket.SendPacket2Server();
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
