using System.Net.Sockets;
using System.Net;
using System.Text;
using Comunicacion;
using Comunicacion.Dominio;
using ServerApp.DataAccess;
using ServerApp.Services;

namespace ServerApp
{
    internal class Program
    {
        static readonly SettingsManager settingsMngr = new SettingsManager();
        static readonly UserManager userManager = new UserManager();
        static readonly GameManager GameManager = new GameManager();
        const int largoDataLength = 4; // Pasar a una clase con constantes del protocolo
        static bool serverRunning = true;
        
        public static UserManager getInstance()
        {
            return userManager;
        }

        private void RegisterNewUser(NetworkDataHelper networkDataHelper)
        {
            lock (this)
            {
                byte[] usernameLength = networkDataHelper.Receive(largoDataLength);
                byte[] usernameData = networkDataHelper.Receive(BitConverter.ToInt32(usernameLength));
                string username = Encoding.UTF8.GetString(usernameData);
                
                if (username.Length < 4)
                {
                    throw new ArgumentException("Username must be at least 4 characters long.");
                }
                
                byte[] passwordLength = networkDataHelper.Receive(largoDataLength);
                byte[] passwordData = networkDataHelper.Receive(BitConverter.ToInt32(passwordLength));
                string password = Encoding.UTF8.GetString(passwordData);
                                
                if (password.Length < 4)
                {
                    throw new ArgumentException("Password must be at least 4 characters long.");
                }
                
                Console.WriteLine("Database.RegisterNewUser -Initiated");
                Console.WriteLine("Database.RegisterNewUser -Executing");
                if (userManager.RegisterUser(username, password))
                {
                    Console.WriteLine("Database.RegisterNewUser - New User: " + username + " Registered");
                    SuccesfulResponse("User registered successfully", networkDataHelper);
                }
                else
                {
                    throw new InvalidOperationException("User already exists.");
                }
            }
        }
        
        private User LoginUser(NetworkDataHelper networkDataHelper)
        {
            byte[] usernameLength = networkDataHelper.Receive(largoDataLength);
            byte[] usernameData = networkDataHelper.Receive(BitConverter.ToInt32(usernameLength));
            string username = Encoding.UTF8.GetString(usernameData);
            byte[] passwordLength = networkDataHelper.Receive(largoDataLength);
            byte[] passwordData = networkDataHelper.Receive(BitConverter.ToInt32(passwordLength));
            string password = Encoding.UTF8.GetString(passwordData);
            
            Console.WriteLine("Database.LoginUser -Initiated");
            Console.WriteLine("Database.LoginUser -Executing");
            User user = userManager.AuthenticateUser(username, password);
            if (user != null)
            {
                SuccesfulResponse("Login successful", networkDataHelper);
                Console.WriteLine("User " + user.Username + " has logged in.");
                return user;
            }
            else
            {
                throw new InvalidOperationException("Invalid username or password.");
            }

            return null;
        }

        private void ShowAllGameInformation(NetworkDataHelper networkDataHelper)
        {
            Console.WriteLine("Database.ShowAllGameInformation -Initiated");
            Console.WriteLine("Database.ShowAllGameInformation -Executing");
            byte[] gameIdLength = networkDataHelper.Receive(largoDataLength);
            byte[] gameIdData = networkDataHelper.Receive(BitConverter.ToInt32(gameIdLength));
            string gameName = Encoding.UTF8.GetString(gameIdData);

            Game game = GameManager.GetGameByName(gameName);
            if (game != null)
            {
                string response = game.ToString();
                SuccesfulResponse(response, networkDataHelper);
            }
            else
            {
                throw new InvalidOperationException("Game not found.");
            }
        }
        
        private void SuccesfulResponse(string message, NetworkDataHelper networkDataHelper)
        {
            byte[] responseData = Encoding.UTF8.GetBytes(message);
            byte[] responseDataLength = BitConverter.GetBytes(responseData.Length);
            networkDataHelper.Send(responseDataLength);
            networkDataHelper.Send(responseData);
        } // Este metodo envia un mensaje de respuesta exitosa al cliente

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
            // Hilo para manejar la entrada de la consola del servidor
            
            Console.WriteLine("Type 'shutdown' to close the server");
            new Thread(() =>
            {
                while (serverRunning)
                {
                    string command = Console.ReadLine();
                    if (command == "shutdown")
                    {
                        serverRunning = false;
                        socketServer.Close();
                        Console.WriteLine("Server is shutting down...");
                    }
                }
            }).Start();
            
            while (serverRunning)
            {
                try
                {
                    var programInstance = new Program();
                    Socket clientSocket = socketServer.Accept(); // El accept es bloqueante, espera hasta que llega una nueva conexión
                    Console.WriteLine("Client connected");
                    new Thread(() => HandleClient(clientSocket, programInstance)).Start(); // Lanzamos un nuevo hilo para manejar al nuevo cliente
                }
                catch (SocketException)
                {
                    if (!serverRunning)
                    {
                        Console.WriteLine("Server has been shut down.");
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            //HILO QUE MANEJA LOS CLIENTES
            static void HandleClient(Socket clientSocket, Program program)
            {
                bool clientIsConnected = true;
                User connectedUser = null;
                NetworkDataHelper networkDataHelper = new NetworkDataHelper(clientSocket);

                while (clientIsConnected)
                {
                    try
                    {
                        byte[] dataLength =
                            networkDataHelper.Receive(largoDataLength); // Recibo la parte fija de los datos
                        byte[] data =
                            networkDataHelper.Receive(
                                BitConverter.ToInt32(dataLength)); // Recibo los datos(parte variable)
                        Console.Write("Client says:");
                        string message = Encoding.UTF8.GetString(data);

                        string response = $"Message '{message}' received successfully";

                        byte[] responseData = Encoding.UTF8.GetBytes(response);
                        byte[] responseDataLength = BitConverter.GetBytes(responseData.Length);

                        networkDataHelper.Send(responseDataLength);
                        networkDataHelper.Send(responseData);

                        Console.WriteLine(message);

                        if (connectedUser == null)
                        {
                            switch (message)
                            {
                                case "1":
                                    program.RegisterNewUser(networkDataHelper);
                                    break;
                                case "2":
                                    connectedUser = program.LoginUser(networkDataHelper);
                                    break;
                                case "3":
                                    clientIsConnected = false;
                                    break;
                            }
                        }
                        else
                        {
                            switch (message)
                            {
                                case "1":
                                    program.RegisterNewUser(networkDataHelper);
                                    break;
                                case "2":
                                    program.ShowAllGameInformation(networkDataHelper);
                                    break;
                                case "8":
                                    connectedUser = null;
                                    break;
                            }
                        }
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine("Client disconnected");
                        clientIsConnected = false;
                    }
                    catch (ArgumentException ex)
                    {
                        string response = ex.Message;
                        byte[] responseData = Encoding.UTF8.GetBytes(response);
                        byte[] responseDataLength = BitConverter.GetBytes(responseData.Length);
                        networkDataHelper.Send(responseDataLength);
                        networkDataHelper.Send(responseData);
                    }
                    catch (InvalidOperationException ex)
                    {
                        string response = ex.Message;
                        byte[] responseData = Encoding.UTF8.GetBytes(response);
                        byte[] responseDataLength = BitConverter.GetBytes(responseData.Length);
                        networkDataHelper.Send(responseDataLength);
                        networkDataHelper.Send(responseData);
                    }
                }
            }
        }
    }
}
