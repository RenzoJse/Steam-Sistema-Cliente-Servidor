using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using Comunicacion;
using ServerApp.Services;

namespace ClientApp
{

    internal class Program
    {
        static readonly SettingsManager settingsMngr = new SettingsManager();
        static NetworkDataHelper networkDataHelper;
        
        static bool clientRunning = false;
        
        private static void LoginMenu()
        {
            Console.WriteLine("1. Register");
            Console.WriteLine("2. Login");
            Console.WriteLine("3. Exit");
        }

        private static void LoggedInMenu()
        {
            Console.WriteLine("1. Search Games");
            Console.WriteLine("2. More information about a game");
            Console.WriteLine("3. Buy GAMES");
            Console.WriteLine("4. Review Game");
            Console.WriteLine("5. Publish a game");
            Console.WriteLine("6. Modify Published Game");
            Console.WriteLine("7. Delete Published Game");
            Console.WriteLine("8. Logout");
        }

        private static void PublishGame()
        {
            Console.Write("Ingrese el título del juego: ");
            string titulo = Console.ReadLine();
            SendMessage(titulo);

            Console.Write("Ingrese el género del juego: ");
            string genero = Console.ReadLine();
            SendMessage(genero);

            Console.Write("Ingrese la fecha de lanzamiento (dd/mm/yyyy): ");
            string fechaLanzamiento = Console.ReadLine();
            SendMessage(fechaLanzamiento);

            Console.Write("Ingrese la plataforma: ");
            string plataforma = Console.ReadLine();
            SendMessage(plataforma);

            Console.Write("Ingrese la cantidad de unidades disponibles: ");
            string unidadesDisponibles = Console.ReadLine();
            SendMessage(unidadesDisponibles);

            Console.Write("Ingrese el precio: ");
            string precio = Console.ReadLine();
            SendMessage(precio);

            Console.Write("Ingrese la valoración: ");
            string valoracion = Console.ReadLine();
            SendMessage(valoracion);

            Console.WriteLine("Juego publicado exitosamente.");

        }
        
        private static void RegisterUser()
        {
            Console.Write("Enter username: ");
            string username = Console.ReadLine();
            Console.Write("Enter password: ");
            string password = Console.ReadLine();
            
            SendMessage(username);
            SendAndReceiveMessage(password);
        }
        
        private static bool Login()
        {
            Console.Write("Enter username: ");
            string username = Console.ReadLine();
            Console.Write("Enter password: ");
            string password = Console.ReadLine();
            
            SendMessage(username);
            return SendAndReceiveMessageBool(password);
        }

        private static bool HavePublishedGames(string message)
        {
            return SendAndReceiveMessageBool(message);
        }
        
        private static void SendMessage(string message) // Con este metodo envias un mensaje al server sin recibir respuesta
        {
            byte[] data = Encoding.UTF8.GetBytes(message); // Convierte de string a una array de bytes
            byte[] dataLength = BitConverter.GetBytes(data.Length); // Calculo el largo de los datos que voy a enviar
            try
            {
                networkDataHelper.Send(dataLength); // Envio el largo del mensaje (parte fija)
                networkDataHelper.Send(data); // Envio el mensaje (parte variable));
            }
            catch (SocketException)
            {
                Console.WriteLine("Connection with the server has been interrupted");
                clientRunning = false;
            } catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }
        
        private static bool SendAndReceiveMessageBool(string message)
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
                string response = Encoding.UTF8.GetString(responseData);
                Console.WriteLine($"Server says: {response}");
                
                return response is "Login successful" or "True";
            }
            catch (SocketException)
            {
                Console.WriteLine("Connection with the server has been interrupted");
                clientRunning = false;
                return false;
            }catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                return false;
            }
        }

        private static void DeleteGame()
        {
            Console.Write("Ingrese el nombre del juego a borrar: ");
            string gameName = Console.ReadLine();
            
            SendMessage(gameName);
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
            }catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }
        
        private static void ReceiveMessage(string message)
        {
            try
            {
                // RECIBO DEL SERVER
                byte[] responseDataLength = networkDataHelper.Receive(4);
                byte[] responseData = networkDataHelper.Receive(BitConverter.ToInt32(responseDataLength));
                Console.WriteLine($"Server says: {Encoding.UTF8.GetString(responseData)}");
            }
            catch (SocketException)
            {
                Console.WriteLine("Connection with the server has been interrupted");
                clientRunning = false;
            }catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
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
            
            bool userConnected = false;
            while (clientRunning)
            {
                if (!userConnected)
                {
                    LoginMenu();
                    string option = Console.ReadLine();
                    SendAndReceiveMessage(option);

                    if (!clientRunning) 
                    {
                        break;
                    }
                    
                    switch (option)
                    {
                        case "1":
                            RegisterUser();
                            break;
                        case "2":
                            if (Login())
                            {
                                userConnected = true;
                            }
                            break;
                        case "3":
                            clientRunning = false;
                            break;
                        default:
                            Console.WriteLine("Invalid option. Please try again.");
                            break;
                    }
                }
                else
                {
                    LoggedInMenu();
                    string option = Console.ReadLine();
                    SendAndReceiveMessage(option);

                    switch (option)
                    {
                        case "1":
                            ReceiveMessage("ShowMeAllGames");
                            break;
                        case "2":
                            Console.WriteLine("Game Name: ");
                            string gameName = Console.ReadLine();
                            SendAndReceiveMessage(gameName);
                            break;
                        case "3":
                            Console.WriteLine("Titulo del juego a comprar: ");
                            string gamePurchase = Console.ReadLine();
                            SendAndReceiveMessage(gamePurchase);
                            break;
                        case "5":
                            PublishGame();
                            break;
                        case "6":
                            if (HavePublishedGames(option))
                            {
                                Console.WriteLine("Game Name You Want To Modify: ");
                                string gameName2 = Console.ReadLine();
                                SendAndReceiveMessage(gameName2);
                            }
                            break;
                        case "7":
                            DeleteGame();
                            break;
                        case "8": //Logout
                            userConnected = false;
                            break;
                        default:
                            Console.WriteLine("Invalid option. Please try again.");
                            break;
                    }
                }
                
            }
            Console.WriteLine("Will Close Connection...");
            socketClient.Shutdown(SocketShutdown.Both);
            socketClient.Close();
        }
    }
}