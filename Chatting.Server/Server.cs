using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Chatting.Server
{
    record TcpClientHolder(int Id, string Name, TcpClient Client, Thread Thread);
    record Chat(string User, string Message);

    public class Server
    {
        private List<TcpClientHolder> _clients = [];
        private int _clientCounter = 0;

        private List<Chat> _chats = [];
      
        public void Startup() 
        { 
            // Start server
            var serverSocket = new TcpListener(IPAddress.Any, 8888);
            serverSocket.Start();
            Console.WriteLine("Chat Server Started");

            bool running = true;
            while (running)
            {
                // Accept a new incoming client.
                TcpClient clientSocket = serverSocket.AcceptTcpClient();
                Console.WriteLine($"Client #{++_clientCounter} connected");

                // Wait for initial packet
                string user = string.Empty;
                while (user == string.Empty)
                {
                    if (!clientSocket.GetStream().DataAvailable)
                        continue;

                    byte[] buffer = new byte[10025];
                    NetworkStream stream = clientSocket.GetStream();
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    // Encode message
                    string data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    user = data;
                }

                // Start a thread for listening to the client.
                var thread = new Thread(() => CheckClientMessages(clientSocket, _clientCounter, user));
                thread.Start();

                // Add the client to the list and warn everyone someone new is here.
                _clients.Add(new(_clientCounter, user, clientSocket, thread));
                this.Broadcast($"{user} joined the conversation");
            }
        }

        private void CheckClientMessages(TcpClient client, int clientId, string user)
        {
            while (true)
            {
                if (!client.GetStream().DataAvailable)
                    continue;

                try
                {
                    // Read stream info into the buffer
                    byte[] buffer = new byte[10025];
                    NetworkStream stream = client.GetStream();
                    int bufferSize = stream.Read(buffer, 0, buffer.Length);

                    // Encode message
                    string data = Encoding.UTF8.GetString(buffer, 0, bufferSize);

                    // Output
                    Console.WriteLine($"recieved: {user}({clientId})>> {data}");
                    this.Broadcast(data, user);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message.ToString());
                }
            }
        }

        public void Broadcast(string message, string user = "system")
        {
            this._chats.Add(new(user, message));

            foreach (var client in _clients)
            {
                var stream = client.Client.GetStream();

                var filteredChats = this._chats.Select(chat =>
                    new Chat(chat.User, chat.Message.Replace("\0", string.Empty).Trim()));

                var json = JsonSerializer.Serialize(filteredChats);
                var encoded = Encoding.UTF8.GetBytes(json);

                stream.Write(encoded, 0, encoded.Length);
                stream.Flush();
            }
        }
    }
}
