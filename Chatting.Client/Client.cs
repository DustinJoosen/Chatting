using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Chatting.Client
{
    public record Chat(string User, string Message);

    public class Client
    {
        private string _name;

        private TcpClient _client;
        private List<Chat> _chats = [];

        private Task _readTask;
        private Task _writeTask;

        public bool Refreshed = false;

        public Client(string name)
        {
            this._name = name;
        }

        public void Startup()
        {
            this._client = new TcpClient();
            this._client.Connect("127.0.0.1", 8888);

            Console.WriteLine("Connected to server");

            this.SendInitialPacket();

            this._readTask = Task.Run(LoopRead);
            this._writeTask = Task.Run(LoopSend);

            Task.WaitAll(this._readTask, this._writeTask);
        }

        private void SendInitialPacket()
        {
            byte[] data = Encoding.UTF8.GetBytes(this._name);

            // Send message to server
            var stream = this._client.GetStream();

            stream.Write(data, 0, data.Length);
            stream.Flush();
        }

        private void LoopRead()
        {
            NetworkStream stream = this._client.GetStream();
            byte[] inStream = new byte[30000000];

            while (true)
            {
                if (!stream.DataAvailable)
                    continue;

                int bytesRead = stream.Read(inStream, 0, inStream.Length);
                string messageReceived = Encoding.ASCII.GetString(inStream, 0, bytesRead).ToString();
                messageReceived = messageReceived.TrimEnd('\0').ToString();
                messageReceived = Regex.Replace(messageReceived, @"\x00", "");

                this._chats = JsonSerializer.Deserialize<List<Chat>>(messageReceived);
                this.Refresh();
            }
        }

        private void LoopSend()
        {
            while (true)
            {
                string? message = Console.ReadLine();

                byte[] data = Encoding.UTF8.GetBytes(message ?? string.Empty);

                // Send message to server
                var stream = this._client.GetStream();

                stream.Write(data, 0, data.Length);
                stream.Flush();
            }
        }

        public void Refresh()
        {
            Console.SetCursorPosition(0, 2);
            _chats.ForEach(PrintChat);
        }

        public void PrintChat(Chat chat)
        {
            Dictionary<string, ConsoleColor> usernameColorMapping = new()
            {
                { "system", ConsoleColor.Green },
                { _name, ConsoleColor.Blue },
                { "default", ConsoleColor.White }
            };

            Console.SetCursorPosition(0, Console.CursorTop);

            string user = chat.User.Replace("\0", string.Empty);
            Console.ForegroundColor =

            Console.ForegroundColor = usernameColorMapping.ContainsKey(user)
                ? usernameColorMapping[user]
                : usernameColorMapping["default"];

            Console.Write(user + ">> ");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(chat.Message + "\n");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }

}
