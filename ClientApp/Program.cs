using System.Net.Sockets;
using System.Net;
using System.Text;
using Comunicacion;
using ServerApp.Services;

namespace ClientApp
{

    internal class Program
    {
        private static UserService _userService;
        static readonly SettingsManager settingsMngr = new SettingsManager();
        static NetworkDataHelper networkDataHelper;
        
        static bool clientRunning = false;
        
        private static void LoginMenu()
        {
            Console.WriteLine("1. Register");
            Console.WriteLine("2. Login");
            Console.WriteLine("3. Exit");
        }

        private static void RegisterUser()
        {
            Console.Write("Enter username: ");
            string username = Console.ReadLine();
            Console.Write("Enter password: ");
            string password = Console.ReadLine();

            try
            {
                _userService.RegisterUser(username, password);
                Console.WriteLine("Te has registrado!");
            }
            catch (SocketException)
            {
                Console.WriteLine("Algo salio mal");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }

        }
        
        private static void SendAndReceiveMessage(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message); // Convierte de string a una array de bytes
            byte[] dataLength = BitConverter.GetBytes(data.Length); // Calculo el largo de los datos que voy a enviar
            try
            {
                // ENVIO AL SERVER
                networkDataHelper.Send(dataLength); // Envio el largo del mensaje (parte fija)
                networkDataHelper.Send(data); // Envio el mensaje (parte variable)

                // RECIBO DEL SERVER
                byte[] responseDataLength = networkDataHelper.Receive(4);
                byte[] responseData = networkDataHelper.Receive(BitConverter.ToInt32(responseDataLength));
                Console.WriteLine($"Server says: {Encoding.UTF8.GetString(responseData)}");
            }
            catch (SocketException)
            {
                Console.WriteLine("Connection with the server has been interrupted");
                clientRunning = false;
            }
        }
        
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Client Application..");
            var socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            string ipServer = settingsMngr.ReadSettings(ClientConfig.serverIPConfigKey);
            string ipClient = settingsMngr.ReadSettings(ClientConfig.clientIPConfigKey);
            int serverPort = int.Parse(settingsMngr.ReadSettings(ClientConfig.serverPortConfigKey));
            int clientPort = int.Parse(settingsMngr.ReadSettings(ClientConfig.clientPortConfigKey));


            var localEndpoint = new IPEndPoint(IPAddress.Parse(ipClient), clientPort);
            var remoteEndpoint = new IPEndPoint(IPAddress.Parse(ipServer), serverPort);

            socketClient.Bind(localEndpoint);
            Console.WriteLine("Connecting to server...");
            socketClient.Connect(remoteEndpoint);
            Console.WriteLine("Connected to server!!!!");
            clientRunning = true;

            networkDataHelper = new NetworkDataHelper(socketClient);

            while (clientRunning)
            {
                LoginMenu();
                string option = Console.ReadLine();
                SendAndReceiveMessage(option);
                switch (option)
                {
                    case "1":
                        RegisterUser();
                        break;
                    case "2":
                        //Login();
                        break;
                    case "3":
                        clientRunning = false;
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }

            }

            Console.WriteLine("Will Close Connection...");
            socketClient.Shutdown(SocketShutdown.Both);
            socketClient.Close();
        }
    }
}