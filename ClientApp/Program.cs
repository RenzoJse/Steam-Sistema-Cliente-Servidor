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
        private static readonly SettingsManager SettingsMngr = new SettingsManager();
        private static NetworkDataHelper? _networkDataHelper;
        private static TcpClient _tcpClient = null!;

        private static bool _clientRunning = false;

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

        private static async Task PublishGame(TcpClient client)
        {
            Console.Write("Enter the game title:");
            var titulo = Console.ReadLine();
            await SendMessage(titulo!);
            var error = await ReceiveMessage();
            while (error == "Error: That Games Already Exist.")
            {
                Console.Write("Enter the game title:");
                titulo = Console.ReadLine();
                if (titulo != null) await SendMessage(titulo);
                error = await ReceiveMessage();
            }

            Console.Write("Enter the game's genre:");
            var genero = Console.ReadLine();
            if (genero != null) await SendMessage(genero);

            Console.Write("Enter the release date (dd/mm/yyyy):");
            var fechaLanzamiento = Console.ReadLine();
            DateTime releaseDate;
            while (!DateTime.TryParseExact(fechaLanzamiento, "dd/MM/yyyy", null,
                       System.Globalization.DateTimeStyles.None, out releaseDate))
            {
                Console.WriteLine("Invalid date format. Please enter the date in the format dd/mm/yyyy.");
                Console.Write("Enter the release date (dd/mm/yyyy):");
                fechaLanzamiento = Console.ReadLine();
            }

            await SendMessage(fechaLanzamiento);

            Console.Write("Enter the platform:");
            var plataforma = Console.ReadLine();
            if (plataforma != null) await SendMessage(plataforma);

            Console.Write("Enter the number of units available:");
            var unidadesDisponibles = Console.ReadLine();
            int unidades;
            while (!int.TryParse(unidadesDisponibles, out unidades))
            {
                Console.WriteLine("Invalid input. Please enter a valid integer for the number of units available.");
                Console.Write("Enter the number of units available:");
                unidadesDisponibles = Console.ReadLine();
            }

            await SendMessage(unidadesDisponibles);

            Console.Write("Enter the price:");
            var precio = Console.ReadLine();
            int precioValue;
            while (!int.TryParse(precio, out precioValue))
            {
                Console.WriteLine("Invalid input. Please enter a valid integer for the price.");
                Console.Write("Enter the price:");
                precio = Console.ReadLine();
            }

            await SendMessage(precio);

            Console.Write("Do you want to upload a cover image? (yes/no)");
            var variableSubida = Console.ReadLine();
            await SendMessage(variableSubida!);

            var uploadImage = await ReceiveMessage();
            if (uploadImage == "yes")
            {
                Console.WriteLine("Enter the full path of the file to send:");
                var abspath = Console.ReadLine();
                var fileCommonHandler = new FileCommsHandler(client);
                await fileCommonHandler.SendFile(abspath!);
                Console.WriteLine("The file was sent to the server");
            }

            Console.WriteLine("");
            Console.WriteLine("Game published successfully.");
            Console.WriteLine("");
        }

        private static async Task RegisterUser()
        {
            Console.Write("Enter username: ");
            var username = Console.ReadLine();
            Console.Write("Enter password: ");
            var password = Console.ReadLine();

            await SendMessage(username!);
            await SendAndReceiveMessage(password!);
        }

        private static async Task<bool> Login()
        {
            Console.Write("Enter username: ");
            var username = Console.ReadLine();
            Console.Write("Enter password: ");
            var password = Console.ReadLine();

            await SendMessage(username!);
            return await SendAndReceiveMessageBool(password!);
        }

        private static async Task SendMessage(string message)
        {
            var data = Encoding.UTF8.GetBytes(message);
            var dataLength = BitConverter.GetBytes(data.Length);
            try
            {
                await _networkDataHelper!.Send(dataLength);
                await _networkDataHelper.Send(data);
            }
            catch (SocketException)
            {
                Console.WriteLine("Connection with the server has been interrupted");
                _clientRunning = false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        private static async Task<bool> SendAndReceiveMessageBool(string message)
        {
            var data = Encoding.UTF8.GetBytes(message);
            var dataLength = BitConverter.GetBytes(data.Length);
            try
            {
                await _networkDataHelper!.Send(dataLength);
                await _networkDataHelper.Send(data);

                // RECIBO DEL SERVER
                var responseDataLength = await _networkDataHelper.Receive(4);
                var responseData = await _networkDataHelper.Receive(BitConverter.ToInt32(responseDataLength));
                var response = Encoding.UTF8.GetString(responseData);
                Console.WriteLine($"Server says: {response}");

                return response is "Login successful" or "True";
            }
            catch (SocketException)
            {
                Console.WriteLine("Connection with the server has been interrupted");
                _clientRunning = false;
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                return false;
            }
        }

        private static async Task ModifyGame(TcpClient client)
        {
            Console.Write("Enter the name of the game you want to modify: ");
            var gameName = Console.ReadLine();
            await SendMessage(gameName!);

            var response = await ReceiveMessage();
            if (response.Contains("Error"))
            {
                Console.WriteLine(response);
                return;
            }

            var modifying = true;
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
                    //     var fileCommonHandler = new FileCommsHandler(client);
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

                await SendMessage("modifyField");
                await SendMessage(field);
                await SendMessage(newValue);

                string modifyResponse = await ReceiveMessage();
                Console.WriteLine(modifyResponse);

                Console.WriteLine("Do you want to modify another attribute? (yes/no)");
                string continueModifying = Console.ReadLine().ToLower();
                if (continueModifying != "yes")
                {
                    modifying = false;
                    await SendMessage("finishModification");
                    Console.WriteLine("Finished modifying the game.");
                }
            }
        }

        private static async Task DeleteGame()
        {
            Console.Write("Which Game Do You Want To Delete?: ");
            var gameName = Console.ReadLine();

            await SendAndReceiveMessage(gameName!);
        }

        private static async Task SendAndReceiveMessage(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message); // Convierte de string a una array de bytes
            byte[] dataLength = BitConverter.GetBytes(data.Length); // Calculo el largo de los datos que voy a enviar
            try
            {
                // ENVIO AL SERVER
                await _networkDataHelper!.Send(dataLength); // Envio el largo del mensaje (parte fija)
                await _networkDataHelper.Send(data); // Envio el mensaje (parte variable)

                // RECIBO DEL SERVER
                byte[] responseDataLength = await _networkDataHelper.Receive(4);
                byte[] responseData = await _networkDataHelper.Receive(BitConverter.ToInt32(responseDataLength));
                Console.WriteLine($"Server says: {Encoding.UTF8.GetString(responseData)}");
            }
            catch (SocketException)
            {
                Console.WriteLine("Connection with the server has been interrupted");
                _clientRunning = false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        private static async Task<string> ReceiveMessage()
        {
            try
            {
                // RECIBO DEL SERVER
                var responseDataLength = await _networkDataHelper!.Receive(4);
                var responseData = await _networkDataHelper.Receive(BitConverter.ToInt32(responseDataLength));
                Console.WriteLine($"Server says: {Encoding.UTF8.GetString(responseData)}");
                return Encoding.UTF8.GetString(responseData);
            }
            catch (SocketException)
            {
                Console.WriteLine("Connection with the server has been interrupted");
                _clientRunning = false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                return "Error: " + e.Message;
            }

            return null;
        }

        private static async Task SearchGames()
        {
            Console.WriteLine("1. Search by genre");
            Console.WriteLine("2. Search by platform");
            Console.WriteLine("3. Search by valoration");
            Console.WriteLine("4. Show all games");
            var option = Console.ReadLine();
            await SendMessage(option!);
            switch (option)
            {
                case "1":
                    Console.Write("Enter genre: ");
                    string genre = Console.ReadLine()!;
                    await SendAndReceiveMessage(genre);
                    break;
                case "2":
                    Console.Write("Enter platform: ");
                    string platform = Console.ReadLine()!;
                    await SendAndReceiveMessage(platform);
                    break;
                case "3":
                    Console.Write("Enter valoration (1-10): ");
                    string valoration = Console.ReadLine()!;
                    while (!int.TryParse(valoration, out int val) || val < 1 || val > 10)
                    {
                        Console.WriteLine("Invalid valoration. Please enter a number between 1 and 10.");
                        valoration = Console.ReadLine()!;
                    }

                    await SendAndReceiveMessage(valoration);
                    break;
                case "4":
                    await ReceiveMessage();
                    break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }

        private static async Task ReviewGame()
        {
            Console.Write("Which Game Do You Want To Review? : ");
            string gameName = Console.ReadLine()!;
            await SendMessage(gameName!);
            string response = await ReceiveMessage();
            if (!response.Contains("Error"))
            {
                Console.Write("Write an opinion about the game: ");
                string reviewText = Console.ReadLine()!;
                await SendMessage(reviewText);
                Console.Write("How Would You Rate It? (0-10): ");
                string valoration = Console.ReadLine()!;
                while (!int.TryParse(valoration, out int val) || val < 1 || val > 10)
                {
                    Console.Write("Invalid valoration. Please enter a number between 1 and 10: ");
                    valoration = Console.ReadLine()!;
                }

                await SendMessage(valoration);
            }

            Console.WriteLine(ReceiveMessage());
        }

        private static async Task MoreInfoGame(TcpClient socketClient)
        {
            Console.WriteLine("Game Name: ");
            string gameName = Console.ReadLine()!;
            await SendAndReceiveMessage(gameName);

            Console.WriteLine("Receiving Game Photo (Images Folder)");

            var fileCommonHandler = new FileCommsHandler(socketClient);

            // Recibe el mensaje del servidor sobre la disponibilidad de la imagen
            string response = await ReceiveMessage();

            if (response == "No image available")
            {
                Console.WriteLine("No image available for this game.\n");
            }
            else
            {
                Console.WriteLine("Image incoming...");
                try
                {
                    await fileCommonHandler.ReceiveFile(gameName); // Recibe el archivo de manera asíncrona
                    Console.WriteLine("Image received successfully!\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error receiving image: " + ex.Message + "\n");
                }
            }

            Console.WriteLine("Read Reviews? (yes/no)");
            string readReviews = Console.ReadLine()!;
            if ("yes".Equals(readReviews, StringComparison.OrdinalIgnoreCase))
            {
                await SendAndReceiveMessage(readReviews); // Enviar y recibir la confirmación para leer reseñas
                string reviews = await ReceiveMessage();   // Recibe las reseñas del servidor
                Console.WriteLine(reviews);
            }

            Console.WriteLine("\n");

            // Muestra el menú de opciones después de terminar
            LoggedInMenu();
        }


        private static async Task BuyGame()
        {
            Console.WriteLine("Which Game Do You Want To Buy: ");
            string gamePurchase = Console.ReadLine()!;
            await SendAndReceiveMessage(gamePurchase);
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Client Application..");
            var tcpClient = new TcpClient();

            string ipServer = SettingsMngr.ReadSettings(ClientConfig.ServerIpConfigKey);
            string ipClient = SettingsMngr.ReadSettings(ClientConfig.ClientIpConfigKey);
            int serverPort = int.Parse(SettingsMngr.ReadSettings(ClientConfig.ServerPortConfigKey));
            int clientPort = int.Parse(SettingsMngr.ReadSettings(ClientConfig.ClientPortConfigKey));

            var localEndpoint = new IPEndPoint(IPAddress.Parse(ipClient), clientPort);
            var remoteEndpoint = new IPEndPoint(IPAddress.Parse(ipServer), serverPort);

            tcpClient.Client.Bind(localEndpoint);
            Console.WriteLine("Connecting to server...");
            await tcpClient.ConnectAsync(remoteEndpoint.Address, remoteEndpoint.Port);
            Console.WriteLine("Connected to server!!!!");
            _clientRunning = true;

            _networkDataHelper = new NetworkDataHelper(tcpClient);

            bool userConnected = false;
            while (_clientRunning)
            {
                try
                {
                    if (!userConnected)
                    {
                        LoginMenu();
                        string option = Console.ReadLine()!;
                        await SendAndReceiveMessage(option);

                        if (!_clientRunning)
                        {
                            break;
                        }

                        switch (option)
                        {
                            case "1":
                                await RegisterUser();
                                break;
                            case "2":
                                if (await Login())
                                {
                                    userConnected = true;
                                }

                                break;
                            case "3":
                                _clientRunning = false;
                                break;
                            default:
                                Console.WriteLine("Invalid option. Please try again.");
                                break;
                        }
                    }
                    else
                    {
                        LoggedInMenu();
                        string option = Console.ReadLine()!;
                        await SendAndReceiveMessage(option);

                        switch (option)
                        {
                            case "1":
                                await SearchGames();
                                break;
                            case "2":
                                await MoreInfoGame(tcpClient);
                                break;
                            case "3":
                                await BuyGame();
                                break;
                            case "4":
                                await ReviewGame();
                                break;
                            case "5":
                                await PublishGame(tcpClient);
                                break;
                            case "6":
                                await ModifyGame(tcpClient);
                                break;
                            case "7":
                                await DeleteGame();
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
                catch (SocketException)
                {
                    Console.WriteLine("The server has closed the connection.");
                    _clientRunning = false;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }

            Console.WriteLine("Will Close Connection...");
            tcpClient.Close();
        }
    }
}