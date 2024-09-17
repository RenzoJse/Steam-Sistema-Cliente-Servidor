using System.Net.Sockets;
using System.Net;
using System.Text;
using Comunicacion;

namespace ClientApp
{
    internal class Program
    {
        static readonly SettingsManager settingsMngr = new SettingsManager();

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
            Console.WriteLine("Type a message and press enter to send it");
            bool clientRunning = true;

            NetworkDataHelper networkDataHelper = new NetworkDataHelper(socketClient);


            while (clientRunning)
            {
                string message = Console.ReadLine();
                if (message.Equals("exit"))
                {
                    clientRunning = false;
                }
                else
                {
                    byte[] data = Encoding.UTF8.GetBytes(message); // Convierte de string a una array de bytes
                    byte[] dataLength =
                        BitConverter.GetBytes(data.Length); // Calculo el largo de los datos que voy a enviar
                    try
                    {
                        // ENVIO AL SERVER
                        networkDataHelper.Send(dataLength); // Envio el largo del mensaje parte fija)
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
            }

            Console.WriteLine("Will Close Connection...");
            socketClient.Shutdown(SocketShutdown.Both);
            socketClient.Close();
        }
    }
}