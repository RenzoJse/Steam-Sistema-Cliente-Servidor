using System.Net.Sockets;
using System.Net;
using System.Text;
using Comunicacion;
using Comunicacion.Dominio;
using ServerApp.DataAccess;

namespace ServerApp
{
    internal class Program
    {
        static readonly SettingsManager settingsMngr = new SettingsManager();
        static UserRepository userRepository = new UserRepository();
        static readonly UserManager userManager = new UserManager();
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Server Application..");
            var socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            string ipaddress = settingsMngr.ReadSettings(ServerConfig.serverIPConfigKey);
            int port = int.Parse(settingsMngr.ReadSettings(ServerConfig.serverPortConfigKey));

            var localEndpoint = new IPEndPoint(IPAddress.Parse(ipaddress), port); //Puerto va entre 0 y 65535
            socketServer.Bind(localEndpoint);
            socketServer.Listen(3); // Nuestro Socket pasa a estar en modo escucha
            Console.WriteLine("Waiting for clients...");
            while (true)
            {
                Socket clientSocket = socketServer.Accept(); // El accept es bloqueante, espera hasta que llega una nueva conexión
                Console.WriteLine("Client connected");
                new Thread(() => HandleClient(clientSocket)).Start(); // Lanzamos un nuevo hilo para manejar al nuevo cliente
            }
            
            //HILO QUE MANEJA LOS CLIENTES
            static void HandleClient(Socket clientSocket)
            {
                bool clientIsConnected = true;
                NetworkDataHelper networkDataHelper = new NetworkDataHelper(clientSocket);

                const int largoDataLength = 4; // Pasar a una clase con constantes del protocolo
                while (clientIsConnected)
                {
                    try
                    {
                        byte[] dataLength = networkDataHelper.Receive(largoDataLength); // Recibo la parte fija de los datos
                        byte[] data = networkDataHelper.Receive(BitConverter.ToInt32(dataLength)); // Recibo los datos(parte variable)
                        Console.Write("Client says:");
                        string message = Encoding.UTF8.GetString(data);
                        Console.WriteLine(message);
                        string response = $"Message '{message}' received successfully";
                        
                        byte[] responseData = Encoding.UTF8.GetBytes(response);
                        byte[] responseDataLength = BitConverter.GetBytes(responseData.Length);
                        
                        networkDataHelper.Send(responseDataLength);
                        networkDataHelper.Send(responseData);
                        
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine("Client disconnected");
                        clientIsConnected = false;
                    }

                }
            }
        }
    }
}