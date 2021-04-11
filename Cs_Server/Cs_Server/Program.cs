using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Cs_Server
{
    class Program
    {
        private static ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(("127.0.0.1:6379,password=osm980811"));

        private const string SessionChannel = "Session"; // Can be anything we want.
        private static string userName = string.Empty;
        static void Main(string[] args)
        {
            //Console.Write("Enter your name: ");
            //userName = Console.ReadLine();

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
            //int initialCursorTop = Console.CursorTop;
            //int initialCursorLeft = Console.CursorLeft;

            //Console.MoveBufferArea(0, initialCursorTop, Console.WindowWidth,
            //                       1, 0, initialCursorTop + 1);
            //Console.CursorTop = initialCursorTop;
            //Console.CursorLeft = 0;

            // Print the message here
            Console.WriteLine(message);

            //Console.CursorTop = initialCursorTop + 1;
            //Console.CursorLeft = initialCursorLeft;
        }
    }
}
