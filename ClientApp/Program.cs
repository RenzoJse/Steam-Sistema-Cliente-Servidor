using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using Comunicacion;
using ServerApp.Services;
using Communication;

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

        private static void PublishGame(Socket socketClient)
        {
            Console.Write("Enter the game title:");
            string titulo = Console.ReadLine();
            SendMessage(titulo);
            string error = ReceiveMessage();
            while (error == "Error: That Games Already Exist.")
            {
                Console.Write("Enter the game title:");
                titulo = Console.ReadLine();
                SendMessage(titulo);
                error = ReceiveMessage();
            }

            Console.Write("Enter the game's genre:");
            string genero = Console.ReadLine();
            SendMessage(genero);

            Console.Write("Enter the release date (dd/mm/yyyy):");
            string fechaLanzamiento = Console.ReadLine();
            DateTime releaseDate;
            while (!DateTime.TryParseExact(fechaLanzamiento, "dd/MM/yyyy", null,
                       System.Globalization.DateTimeStyles.None, out releaseDate))
            {
                Console.WriteLine("Invalid date format. Please enter the date in the format dd/mm/yyyy.");
                Console.Write("Enter the release date (dd/mm/yyyy):");
                fechaLanzamiento = Console.ReadLine();
            }

            SendMessage(fechaLanzamiento);

            Console.Write("Enter the platform:");
            string plataforma = Console.ReadLine();
            SendMessage(plataforma);

            Console.Write("Enter the number of units available:");
            string unidadesDisponibles = Console.ReadLine();
            int unidades;
            while (!int.TryParse(unidadesDisponibles, out unidades))
            {
                Console.WriteLine("Invalid input. Please enter a valid integer for the number of units available.");
                Console.Write("Enter the number of units available:");
                unidadesDisponibles = Console.ReadLine();
            }

            SendMessage(unidadesDisponibles);

            Console.Write("Enter the price:");
            string precio = Console.ReadLine();
            int precioValue;
            while (!int.TryParse(precio, out precioValue))
            {
                Console.WriteLine("Invalid input. Please enter a valid integer for the price.");
                Console.Write("Enter the price:");
                precio = Console.ReadLine();
            }

            SendMessage(precio);

            Console.Write("Do you want to upload a cover image? (yes/no)");
            string variableSubida = Console.ReadLine();
            SendMessage(variableSubida);

            string vairableSubida2 = ReceiveMessage();
            if (vairableSubida2 == "yes")
            {
                Console.WriteLine("Enter the full path of the file to send:");
                String abspath = Console.ReadLine();
                var fileCommonHandler = new FileCommsHandler(socketClient);
                fileCommonHandler.SendFile(abspath);
                Console.WriteLine("The file was sent to the server");
            }

            Console.WriteLine("");
            Console.WriteLine("Game published successfully.");
            Console.WriteLine("");
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

        private static void SendMessage(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            byte[] dataLength = BitConverter.GetBytes(data.Length);
            try
            {
                networkDataHelper.Send(dataLength);
                networkDataHelper.Send(data);
            }
            catch (SocketException)
            {
                Console.WriteLine("Connection with the server has been interrupted");
                clientRunning = false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        private static bool SendAndReceiveMessageBool(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            byte[] dataLength = BitConverter.GetBytes(data.Length);
            try
            {
                networkDataHelper.Send(dataLength);
                networkDataHelper.Send(data);

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
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                return false;
            }
        }

        private static void ModifyGame(Socket socketClient)
        {
            Console.Write("Enter the name of the game you want to modify: ");
            string gameName = Console.ReadLine();
            SendMessage(gameName);

            string response = ReceiveMessage();
            if (response.Contains("Error"))
            {
                Console.WriteLine(response);
                return;
            }

            bool modifying = true;
            while (modifying)
            {
                Console.WriteLine("What attribute would you like to modify?");
                Console.WriteLine("1. Title");
                Console.WriteLine("2. Genre");
                Console.WriteLine("3. Release Date");
                Console.WriteLine("4. Platform");
                Console.WriteLine("5. Publisher");
                Console.WriteLine("6. Units Available");
                // Console.WriteLine("7. Cover Image");
                Console.WriteLine("7. Finish");

                string option = Console.ReadLine();
                string field = string.Empty;
                string newValue = string.Empty;

                switch (option)
                {
                    case "1":
                        field = "title";
                        Console.Write("Enter new title: ");
                        newValue = Console.ReadLine();
                        break;
                    case "2":
                        field = "genre";
                        Console.Write("Enter new genre: ");
                        newValue = Console.ReadLine();
                        break;
                    case "3":
                        field = "release date";
                        Console.Write("Enter new release date (dd/mm/yyyy): ");
                        newValue = Console.ReadLine();
                        DateTime releaseDate;
                        while (!DateTime.TryParseExact(newValue, "dd/MM/yyyy", null,
                                   System.Globalization.DateTimeStyles.None, out releaseDate))
                        {
                            Console.WriteLine("Invalid date format. Please enter the date in the format dd/mm/yyyy.");
                            newValue = Console.ReadLine();
                        }

                        break;
                    case "4":
                        field = "platform";
                        Console.Write("Enter new platform: ");
                        newValue = Console.ReadLine();
                        break;
                    case "5":
                        field = "publisher";
                        Console.Write("Enter new publisher: ");
                        newValue = Console.ReadLine();
                        break;
                    case "6":
                        field = "units available";
                        Console.Write("Enter new units available: ");
                        newValue = Console.ReadLine();
                        int units;
                        while (!int.TryParse(newValue, out units))
                        {
                            Console.WriteLine("Invalid input. Please enter a valid integer for units available.");
                            newValue = Console.ReadLine();
                        }

                        break;
                    // case "7":  // Comentado para evitar error de excpecion, logica implementada
                    //     field = "cover image";
                    //     SendMessage("deleteExistingCover");
                    //     Console.WriteLine("Enter the full path of the file to send:");
                    //     string abspath = Console.ReadLine();
                    //     var fileCommonHandler = new FileCommsHandler(socketClient);
                    //     fileCommonHandler.SendFile(abspath);
                    //     Console.WriteLine("The new cover image has been sent to the server.");
                    //     continue;
                    case "7":
                        Console.WriteLine("Finished modifying.");
                        modifying = false;
                        SendMessage("finishModification");
                        continue;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        continue;
                }

                SendMessage("modifyField");
                SendMessage(field);
                SendMessage(newValue);

                string modifyResponse = ReceiveMessage();
                Console.WriteLine(modifyResponse);

                Console.WriteLine("Do you want to modify another attribute? (yes/no)");
                string continueModifying = Console.ReadLine().ToLower();
                if (continueModifying != "yes")
                {
                    modifying = false;
                    SendMessage("finishModification");
                    Console.WriteLine("Finished modifying the game.");
                }
            }
        }

        private static void DeleteGame()
        {
            Console.Write("Which Game Do You Want To Delete?: ");
            string gameName = Console.ReadLine();

            SendAndReceiveMessage(gameName);
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
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        private static string ReceiveMessage()
        {
            try
            {
                // RECIBO DEL SERVER
                byte[] responseDataLength = networkDataHelper.Receive(4);
                byte[] responseData = networkDataHelper.Receive(BitConverter.ToInt32(responseDataLength));
                Console.WriteLine($"Server says: {Encoding.UTF8.GetString(responseData)}");
                return Encoding.UTF8.GetString(responseData);
            }
            catch (SocketException)
            {
                Console.WriteLine("Connection with the server has been interrupted");
                clientRunning = false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                return "Error: " + e.Message;
            }

            return null;
        }

        private static void SearchGames()
        {
            Console.WriteLine("1. Search by genre");
            Console.WriteLine("2. Search by platform");
            Console.WriteLine("3. Search by valoration");
            Console.WriteLine("4. Show all games");
            string option = Console.ReadLine();
            SendMessage(option);
            switch (option)
            {
                case "1":
                    Console.Write("Enter genre: ");
                    string genre = Console.ReadLine();
                    if (genre != null) SendAndReceiveMessage(genre);
                    break;
                case "2":
                    Console.Write("Enter platform: ");
                    string platform = Console.ReadLine();
                    SendAndReceiveMessage(platform);
                    break;
                case "3":
                    Console.Write("Enter valoration (1-10): ");
                    string valoration = Console.ReadLine();
                    while (!int.TryParse(valoration, out int val) || val < 1 || val > 10)
                    {
                        Console.WriteLine("Invalid valoration. Please enter a number between 1 and 10.");
                        valoration = Console.ReadLine();
                    }

                    SendAndReceiveMessage(valoration);
                    break;
                case "4":
                    ReceiveMessage();
                    break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }

        private static void ReviewGame()
        {
            Console.Write("Which Game Do You Want To Review? : ");
            string gameName = Console.ReadLine();
            SendMessage(gameName);
            string response = ReceiveMessage();
            if (!response.Contains("Error"))
            {
                Console.Write("Write an opinion about the game: ");
                string reviewText = Console.ReadLine();
                SendMessage(reviewText);
                Console.Write("How Would You Rate It? (0-10): ");
                string valoration = Console.ReadLine();
                while (!int.TryParse(valoration, out int val) || val < 1 || val > 10)
                {
                    Console.Write("Invalid valoration. Please enter a number between 1 and 10: ");
                    valoration = Console.ReadLine();
                }

                SendMessage(valoration);
            }

            Console.WriteLine(ReceiveMessage());
        }

        private static void MoreInfoGame(Socket socketClient)
        {
            Console.WriteLine("Game Name: ");
            string gameName = Console.ReadLine();
            SendAndReceiveMessage(gameName);

            Console.WriteLine("Receiving Game Photo (Images Folder)");

            var fileCommonHandler = new FileCommsHandler(socketClient);

            string response = ReceiveMessage(); // Recibe el mensaje del servidor

            if (response == "No image available")
            {
                Console.WriteLine("No image available for this game.\n");
            }
            else
            {
                Console.WriteLine("Image incoming...");
                try
                {
                    fileCommonHandler.ReceiveFile(gameName);
                    Console.WriteLine("Image received successfully!\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error receiving image: " + ex.Message + "\n");
                }
            }

            Console.WriteLine("Read Reviews? (yes/no)");
            string readReviews = Console.ReadLine();
            if ("yes".Equals(readReviews, StringComparison.OrdinalIgnoreCase))
            {
                SendAndReceiveMessage(readReviews);
            }

            Console.WriteLine("\n");

            // Muestra el menú de opciones después de terminar
            LoggedInMenu();
        }




        private static void BuyGame()
        {
            Console.WriteLine("Which Game Do You Want To Buy: ");
            string gamePurchase = Console.ReadLine();
            SendAndReceiveMessage(gamePurchase);
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
                            SearchGames();
                            break;
                        case "2":
                            MoreInfoGame(socketClient);
                            break;
                        case "3":
                            BuyGame();
                            break;
                        case "4":
                            ReviewGame();
                            break;
                        case "5":
                            PublishGame(socketClient);
                            break;
                        case "6":
                            ModifyGame(socketClient);
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