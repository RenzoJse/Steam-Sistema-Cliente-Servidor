using System.Net;
using System.Net.Sockets;
using System.Text;
using Communication;
using Comunicacion;
using Comunicacion.Dominio;

namespace ServerApp;

internal class Program
{
    private static readonly SettingsManager SettingsMngr = new();
    private static readonly UserManager UserManager = new();
    private static readonly GameManager GameManager = new();
    private static readonly List<Socket> ClientSockets = [];
    private const int LargoDataLength = 4; // Pasar a una clase con constantes del protocolo

    private static bool _serverRunning = true;
    //private static object _lock = new object();

    public static UserManager GetInstance()
    {
        return UserManager;
    }

    private static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Server Application..");
        var socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        var ipaddress = SettingsMngr.ReadSettings(ServerConfig.ServerIpConfigKey);
        var port = int.Parse(SettingsMngr.ReadSettings(ServerConfig.ServerPortConfigKey));

        var localEndpoint = new IPEndPoint(IPAddress.Parse(ipaddress), port); //Puerto va entre 0 y 65535
        socketServer.Bind(localEndpoint);
        socketServer.Listen(3); // Nuestro Socket pasa a estar en modo escucha
        Console.WriteLine("Waiting for clients...");
        // Hilo para manejar la entrada de la consola del servidor

        Console.WriteLine("Type 'shutdown' to close the server");
        new Thread(() =>
        {
            var command = Console.ReadLine();
            if (command == "shutdown")
            {
                foreach (var client in ClientSockets) client.Close();

                socketServer.Close();
                _serverRunning = false;
                Console.WriteLine("Server is shutting down...");
            }
        }).Start();

        while (_serverRunning)
            try
            {
                var programInstance = new Program();
                var
                    clientSocket =
                        socketServer.Accept(); // El accept es bloqueante, espera hasta que llega una nueva conexión
                ClientSockets.Add(clientSocket);
                Console.WriteLine("Client connected");
                new Thread(() => HandleClient(clientSocket, programInstance))
                    .Start(); // Lanzamos un nuevo hilo para manejar al nuevo cliente
            }
            catch (Exception ex)
            {
                Console.WriteLine("Server has been shut down.");
            }

        //HILO QUE MANEJA LOS CLIENTES
        static void HandleClient(Socket clientSocket, Program program)
        {
            var clientIsConnected = true;
            User connectedUser = null;
            var networkDataHelper = new NetworkDataHelper(clientSocket);

            while (clientIsConnected)
                try
                {
                    while (connectedUser == null)
                        switch (ProtocolMessage(networkDataHelper))
                        {
                            case "1":
                                program.RegisterNewUser(networkDataHelper);
                                break;
                            case "2":
                                connectedUser = LoginUser(networkDataHelper);
                                break;
                            case "3":
                                clientIsConnected = false;
                                break;
                        }

                    while (connectedUser != null)
                        switch (ProtocolMessage(networkDataHelper))
                        {
                            case "1":
                                program.SearchGames(networkDataHelper);
                                break;
                            case "2":
                                program.ShowAllGameInformation(networkDataHelper, clientSocket);
                                break;
                            case "3":
                                program.PurchaseGame(networkDataHelper, connectedUser);
                                break;
                            case "4":
                                program.ReviewGame(networkDataHelper, connectedUser);
                                break;
                            case "5":
                                program.PublishGame(networkDataHelper, connectedUser, clientSocket);
                                break;
                            case "6":
                                program.EditPublishedGame(networkDataHelper, connectedUser, clientSocket);
                                break;
                            case "7":
                                program.DeleteGame(networkDataHelper, connectedUser);
                                break;
                            case "8":
                                connectedUser = null;
                                break;
                        }
                }
                catch (SocketException)
                {
                    Console.WriteLine("Client disconnected");
                    clientIsConnected = false;
                }
                catch (ArgumentException ex)
                {
                    var response = ex.Message;
                    var responseData = Encoding.UTF8.GetBytes(response);
                    var responseDataLength = BitConverter.GetBytes(responseData.Length);
                    networkDataHelper.Send(responseDataLength);
                    networkDataHelper.Send(responseData);
                }
                catch (InvalidOperationException ex)
                {
                    var response = ex.Message;
                    var responseData = Encoding.UTF8.GetBytes(response);
                    var responseDataLength = BitConverter.GetBytes(responseData.Length);
                    networkDataHelper.Send(responseDataLength);
                    networkDataHelper.Send(responseData);
                }
        }
    }

    private void RegisterNewUser(NetworkDataHelper networkDataHelper)
    {
        var usernameLength = networkDataHelper.Receive(LargoDataLength);
        var usernameData = networkDataHelper.Receive(BitConverter.ToInt32(usernameLength));
        var username = Encoding.UTF8.GetString(usernameData);

        var passwordLength = networkDataHelper.Receive(LargoDataLength);
        var passwordData = networkDataHelper.Receive(BitConverter.ToInt32(passwordLength));
        var password = Encoding.UTF8.GetString(passwordData);

        if (password.Length < 4)
            throw new ArgumentException("Password must be at least 4 characters long.");
        if (username.Length < 4) throw new ArgumentException("Username must be at least 4 characters long.");

        Console.WriteLine("Database.RegisterNewUser -Initiated");
        Console.WriteLine("Database.RegisterNewUser -Executing");
        if (UserManager.RegisterUser(username, password))
        {
            Console.WriteLine("Database.RegisterNewUser - New User: " + username + " Registered");
            SuccesfulResponse("User registered successfully", networkDataHelper);
        }
        else
        {
            throw new InvalidOperationException("User already exists.");
        }
    }

    private static User LoginUser(NetworkDataHelper networkDataHelper)
    {
        var usernameLength = networkDataHelper.Receive(LargoDataLength);
        var usernameData = networkDataHelper.Receive(BitConverter.ToInt32(usernameLength));
        var username = Encoding.UTF8.GetString(usernameData);
        var passwordLength = networkDataHelper.Receive(LargoDataLength);
        var passwordData = networkDataHelper.Receive(BitConverter.ToInt32(passwordLength));
        var password = Encoding.UTF8.GetString(passwordData);

        Console.WriteLine("Database.LoginUser -Initiated");
        Console.WriteLine("Database.LoginUser -Executing");
        var user = UserManager.AuthenticateUser(username, password);
        if (user != null)
        {
            SuccesfulResponse("Login successful", networkDataHelper);
            Console.WriteLine("User " + user.Username + " has logged in.");
            return user;
        }

        throw new InvalidOperationException("Invalid username or password.");

        return null;
    }

    private void ShowAllGameInformation(NetworkDataHelper networkDataHelper, Socket socketClient)
    {
        Console.WriteLine("Database.ShowAllGameInformation -Initiated");
        Console.WriteLine("Database.ShowAllGameInformation -Executing");
        var gameIdLength = networkDataHelper.Receive(LargoDataLength);
        var gameIdData = networkDataHelper.Receive(BitConverter.ToInt32(gameIdLength));
        var gameName = Encoding.UTF8.GetString(gameIdData);

        var game = GameManager.GetGameByName(gameName);
        if (game != null)
        {
            var response = game.ToString();
            SuccesfulResponse(response, networkDataHelper);

            Console.WriteLine("Sending File...");
            try
            {
                var abspath = Path.Combine(Directory.GetCurrentDirectory(), "Images", game.Name + ".jpg");
                if (!File.Exists(abspath))
                {
                    Console.WriteLine($"File does not exist at path: {abspath}");
                    return;
                }

                var fileCommonHandler = new FileCommsHandler(socketClient);
                fileCommonHandler.SendFile(abspath);
                Console.WriteLine("File Sent Successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending file: {ex.Message}");
            }

            var option = ReceiveStringData(networkDataHelper);
            if (option.Equals("yes"))
            {
                var reviews = new StringBuilder("Reviews:\n");
                foreach (var review in game.Reviews)
                    reviews.Append("\n- " + review.Description + " - Valoration: " + review.Valoration);

                SuccesfulResponse(reviews.ToString(), networkDataHelper);
            }else if (option.Equals("no"))
            {
                SuccesfulResponse("Enjoy your game info!", networkDataHelper);
            }
        }
        else
        {
            throw new InvalidOperationException("Game not found.");
        }
    }

    private void PublishGame(NetworkDataHelper networkDataHelper, User connectedUser, Socket socketClient)
    {
        Console.WriteLine("Database.PublishGame -Initiated");
        Console.WriteLine("Database.PublishGame -Executing");

        var gameName = ReceiveStringData(networkDataHelper);
        var gameExists = GameManager.DoesGameExist(gameName);
        while (gameExists)
        {
            SuccesfulResponse("Error: That Games Already Exist.", networkDataHelper);
            gameName = ReceiveStringData(networkDataHelper);
            gameExists = GameManager.DoesGameExist(gameName);
            if (!gameExists) SuccesfulResponse("Succesful New Game Name", networkDataHelper);
        }

        SuccesfulResponse("Succesful New Game Name", networkDataHelper);
        var genre = ReceiveStringData(networkDataHelper);
        var releaseDateInput = ReceiveStringData(networkDataHelper);
        var releaseDate = DateTime.Parse(releaseDateInput);
        var platform = ReceiveStringData(networkDataHelper);
        var unitsAvailable = int.Parse(ReceiveStringData(networkDataHelper));
        var price = int.Parse(ReceiveStringData(networkDataHelper));
        var variableSubida = ReceiveStringData(networkDataHelper);
        SuccesfulResponse(variableSubida, networkDataHelper);
        if (variableSubida == "yes")
        {
            Console.WriteLine("Image incoming...");
            var fileCommonHandler = new FileCommsHandler(socketClient);
            fileCommonHandler.ReceiveFile(gameName);
            Console.WriteLine("Image received!");
        }

        var valoration = 0;
        var newGame = GameManager.CreateNewGame(gameName, genre, releaseDate, platform, unitsAvailable, price,
            valoration, connectedUser);
        UserManager.PublishGame(newGame, connectedUser);
    }

    private static string ReceiveStringData(NetworkDataHelper networkDataHelper)
    {
        var dataLength = networkDataHelper.Receive(LargoDataLength);
        var data = networkDataHelper.Receive(BitConverter.ToInt32(dataLength));
        return Encoding.UTF8.GetString(data);
    }

    private void DeleteGame(NetworkDataHelper networkDataHelper, User connectedUser)
    {
        var gameNameLength = networkDataHelper.Receive(LargoDataLength);
        var gameNameData = networkDataHelper.Receive(BitConverter.ToInt32(gameNameLength));
        var gameName = Encoding.UTF8.GetString(gameNameData);

        if (GameManager.DoesGameExist(gameName))
        {
            if (connectedUser.PublishedGames.Contains(GameManager.GetGameByName(gameName)))
            {
                GameManager.RemoveGame(gameName);

                // Delete the image file
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Images", $"{gameName}.jpg");
                if (File.Exists(imagePath)) File.Delete(imagePath);

                SuccesfulResponse("Game and its image deleted successfully.", networkDataHelper);
            }
            else
            {
                throw new InvalidOperationException("You are not the publisher of the game.");
            }
        }
        else
        {
            SuccesfulResponse("Game not found.", networkDataHelper);
        }
    }

    private void ShowAllGames(NetworkDataHelper networkDataHelper)
    {
        Console.WriteLine("Database.ShowAllGames -Initiated");
        Console.WriteLine("Database.ShowAllGames -Executing");

        var games = GameManager.GetAllGames();
        if (games.Count == 0) throw new InvalidOperationException("No games found.");

        var response = new StringBuilder("All games:\n");
        foreach (var game in games)
            response.Append("\n- " + game.Name + " - Price: " + game.Price + " - Units: " + game.UnitsAvailable);

        SuccesfulResponse(response.ToString(), networkDataHelper);
    }

    private void SearchGames(NetworkDataHelper networkDataHelper)
    {
        Console.WriteLine("Database.SearchGames -Initiated");
        Console.WriteLine("Database.SearchGames -Executing");
        var option = ReceiveStringData(networkDataHelper);
        switch (option)
        {
            case "1":
                ShowAllGamesByGenre(networkDataHelper);
                break;
            case "2":
                ShowAllGamesByPlatform(networkDataHelper);
                break;
            case "3":
                ShowAllGamesByValorations(networkDataHelper);
                break;
            case "4":
                ShowAllGames(networkDataHelper);
                break;
        }
    }

    private void ShowAllGamesByValorations(NetworkDataHelper networkDataHelper)
    {
        var valoration = ReceiveStringData(networkDataHelper);
        Console.WriteLine("Received valoration: " + valoration);

        var distinctGamesByValoration = GameManager.GetGamesByAttribute("Valoration", valoration);
        Console.WriteLine("Found " + distinctGamesByValoration.Count + " games with valoration " + valoration);

        var response = new StringBuilder("Games with valoration " + valoration + ":\n");
        foreach (var game in distinctGamesByValoration)
            response.Append("\n- " + game.Name + " - Price: " + game.Price + " - Units: " + game.UnitsAvailable);

        SuccesfulResponse(response.ToString(), networkDataHelper);
    }

    private void ShowAllGamesByGenre(NetworkDataHelper networkDataHelper)
    {
        var genre = ReceiveStringData(networkDataHelper);
        Console.WriteLine("Received genre: " + genre);

        var distinctGamesByGenre = GameManager.GetGamesByAttribute("Genre", genre);
        Console.WriteLine("Found " + distinctGamesByGenre.Count + " games in genre " + genre);

        var response = new StringBuilder("Games in genre " + genre + ":\n");
        foreach (var game in distinctGamesByGenre)
            response.Append("\n- " + game.Name + " - Price: " + game.Price + " - Units: " + game.UnitsAvailable);

        SuccesfulResponse(response.ToString(), networkDataHelper);
    }

    private void ShowAllGamesByPlatform(NetworkDataHelper networkDataHelper)
    {
        var platform = ReceiveStringData(networkDataHelper);
        Console.WriteLine("Received platform: " + platform);

        var distinctGamesByPlatform = GameManager.GetGamesByAttribute("Platform", platform);
        Console.WriteLine("Found " + distinctGamesByPlatform.Count + " games in platform " + platform);

        var response = new StringBuilder("Games in platform " + platform + ":\n");
        foreach (var game in distinctGamesByPlatform)
            response.Append("\n- " + game.Name + " - Price: " + game.Price + " - Units: " + game.UnitsAvailable);

        SuccesfulResponse(response.ToString(), networkDataHelper);
    }

    private void ShowPublishedGames(NetworkDataHelper networkDataHelper, User connectedUser)
    {
        Console.WriteLine("Database.ShowPublishedGames -Initiated");
        Console.WriteLine("Database.ShowPublishedGames -Executing");
        var response = new StringBuilder("Published games: ");
        foreach (var game in connectedUser.PublishedGames) response.Append(game.Name).Append("\n ");

        if (response.Length > 0) response.Length -= 2;

        SuccesfulResponse(response.ToString(), networkDataHelper);
    }

    private void EditPublishedGame(NetworkDataHelper networkDataHelper, User connectedUser, Socket socketClient)
    {
        Console.WriteLine("Database.EditPublishedGame - Initiated");

        var gameName = ReceiveStringData(networkDataHelper);
        var game = GameManager.GetGameByName(gameName);

        if (game == null || !connectedUser.PublishedGames.Contains(game))
        {
            SuccesfulResponse("Error: Game not found or you are not the publisher.", networkDataHelper);
            return;
        }

        SuccesfulResponse("Game found. You can modify it.", networkDataHelper);

        var modifying = true;
        while (modifying)
        {
            var action = ReceiveStringData(networkDataHelper);

            if (action == "finishModification")
            {
                modifying = false;
                Console.WriteLine("Modification finished for game: " + game.Name);
                continue;
            }

            if (action == "modifyField")
            {
                var field = ReceiveStringData(networkDataHelper);
                var newValue = ReceiveStringData(networkDataHelper);

                try
                {
                    switch (field.ToLower())
                    {
                        case "title":
                            game.Name = newValue;
                            break;
                        case "genre":
                            game.Genre = newValue;
                            break;
                        case "release date":
                            if (DateTime.TryParse(newValue, out var newReleaseDate))
                                game.ReleaseDate = newReleaseDate;
                            else
                                throw new ArgumentException("Invalid date format.");
                            break;
                        case "platform":
                            game.Platform = newValue;
                            break;
                        case "publisher":
                            game.Publisher = newValue;
                            break;
                        case "units available":
                            if (int.TryParse(newValue, out var newUnitsAvailable))
                                game.UnitsAvailable = newUnitsAvailable;
                            else
                                throw new ArgumentException("Invalid number format.");
                            break;
                        // case "cover image":  // Comentado para evitar error de excepcion, logica implementada
                        //     string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Images", $"{game.Name}.jpg");
                        //     if (File.Exists(imagePath))
                        //     {
                        //         File.Delete(imagePath);
                        //         Console.WriteLine("Existing cover image deleted.");
                        //     }
                        //     Console.WriteLine("Receiving new cover image...");
                        //     var fileCommonHandler = new FileCommsHandler(socketClient);
                        //     fileCommonHandler.ReceiveFile(game.Name);
                        //     Console.WriteLine("New cover image received!");
                        //     break;
                        default:
                            throw new ArgumentException("Invalid field.");
                    }

                    SuccesfulResponse("Game edited successfully", networkDataHelper);
                }
                catch (ArgumentException ex)
                {
                    SuccesfulResponse($"Error: {ex.Message}", networkDataHelper);
                }
            }
        }
    }

    private void PurchaseGame(NetworkDataHelper networkDataHelper, User connectedUser)
    {
        var gameNameLength = networkDataHelper.Receive(LargoDataLength);
        var gameNameData = networkDataHelper.Receive(BitConverter.ToInt32(gameNameLength));
        var gameName = Encoding.UTF8.GetString(gameNameData);

        var game = GameManager.GetGameByName(gameName);

        if (game == null)
            throw new InvalidOperationException("The game does not exist.");
        if (game.UnitsAvailable <= 0) throw new InvalidOperationException("No units available.");

        Console.WriteLine("Database.PurchaseGame - Initiated");
        Console.WriteLine("Database.PurchaseGame - Executing");
        if (UserManager.PurchaseGame(game, connectedUser))
        {
            GameManager.DiscountPurchasedGame(game);
            Console.WriteLine("Database.PurchaseGame - The game: " + game.Name + " has been purchased");
            SuccesfulResponse("Game purchased successfully", networkDataHelper);
        }
        else
        {
            throw new InvalidOperationException("Error purchasing the game.");
        }
    }

    public void ReviewGame(NetworkDataHelper networkDataHelper, User connectedUser)
    {
        var gameName = ReceiveStringData(networkDataHelper);
        var game = GameManager.GetGameByName(gameName);
        if (game == null) throw new InvalidOperationException("Error: Game not found.");

        if (!connectedUser.PurchasedGames.Contains(game))
            throw new InvalidOperationException("Error: You must purchase the game to review it.");

        SuccesfulResponse("Review Added Successfully", networkDataHelper);
        var reviewText = ReceiveStringData(networkDataHelper);
        if (string.IsNullOrEmpty(reviewText)) reviewText = "No review";

        var valoration = ReceiveStringData(networkDataHelper);
        var review = new Review { Valoration = int.Parse(valoration), Description = reviewText };
        GameManager.AddReview(gameName, review);
        GameManager.AddValoration(gameName, int.Parse(valoration));
        SuccesfulResponse("Thanks For Your Collaboration!", networkDataHelper);
    }

    private static void SuccesfulResponse(string message, NetworkDataHelper networkDataHelper)
    {
        var responseData = Encoding.UTF8.GetBytes(message);
        var responseDataLength = BitConverter.GetBytes(responseData.Length);
        networkDataHelper.Send(responseDataLength);
        networkDataHelper.Send(responseData);
    } // Este metodo envia un mensaje de respuesta exitosa al cliente

    private static string ProtocolMessage(NetworkDataHelper networkDataHelper)
    {
        var dataLength = networkDataHelper.Receive(LargoDataLength); // Recibo la parte fija de los datos
        var data =
            networkDataHelper.Receive(BitConverter.ToInt32(dataLength)); // Recibo los datos(parte variable)
        Console.Write("Client says:");
        var message = Encoding.UTF8.GetString(data);

        var response = $"Option '{message}' received successfully";

        var responseData = Encoding.UTF8.GetBytes(response);
        var responseDataLength = BitConverter.GetBytes(responseData.Length);

        networkDataHelper.Send(responseDataLength);
        networkDataHelper.Send(responseData);

        Console.WriteLine(message);
        return message;
    } // Este metodo recibe un mensaje del cliente y envia una respuesta exitosa
}