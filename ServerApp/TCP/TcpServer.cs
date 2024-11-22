using System.Net;
using System.Net.Sockets;
using System.Text;
using Communication;
using Comunicacion;
using Comunicacion.Dominio;
using ServerApp.Dominio;
using ServerApp.MomMessage;

namespace ServerApp.TCP;

public class TcpServer
{
    private static readonly UserManager UserManager = new UserManager();
    private static readonly GameManager GameManager = new GameManager();
    private const int LargoDataLength = 4;

    private static readonly SendMom SendMom = new SendMom();
    private readonly TcpListener _server;
    private readonly List<TcpClient> _connectedClients = [];
    private readonly object _lock = new();
    private bool _serverRunning = true;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly CancellationToken _cancellationToken;
    private static readonly object Lock = new object();

    public TcpServer(string ipAddress, int port)
    {
        _server = new TcpListener(IPAddress.Parse(ipAddress), port);
        _cancellationToken = _cancellationTokenSource.Token;
    }

    public void Start()
    {
        _server.Start();
        Console.WriteLine("Server started and waiting for clients...");
        Task.Run(HandleIncomingConnections);
    }

    public void Stop()
    {
        lock (_lock)
        {
            foreach (var client in _connectedClients) client.Close();
        }

        _serverRunning = false;
        _server.Stop();
        Console.WriteLine("Server stopped.");
    }

    private async Task HandleIncomingConnections()
    {
        while (_serverRunning)
            try
            {
                var client = await _server.AcceptTcpClientAsync();
                lock (_lock)
                {
                    _connectedClients.Add(client);
                }

                Console.WriteLine("Client connected.");
                await Task.Run(() => HandleClient(client, UserManager), _cancellationToken);
            }
            catch (Exception ex) when (!_serverRunning)
            {
                Console.WriteLine("Server has been shut down.");
            }
    }

    private async Task HandleClient(TcpClient client, UserManager userManager)
    {
        var clientIsConnected = true;
        User connectedUser = null!;
        var networkDataHelper = new NetworkDataHelper(client);

        try
        {
            while (clientIsConnected && !_cancellationToken.IsCancellationRequested)
                try
                {
                    while (connectedUser == null)
                        switch (await ProtocolMessage(networkDataHelper))
                        {
                            case "1":
                                await RegisterNewUser(networkDataHelper);
                                break;
                            case "2":
                                connectedUser = await LoginUser(networkDataHelper);
                                break;
                            case "3":
                                lock (Lock)
                                {
                                    _connectedClients.Remove(client);
                                }
                                client.Close();
                                clientIsConnected = false;
                                break;
                        }

                    while (connectedUser != null)
                        switch (await ProtocolMessage(networkDataHelper))
                        {
                            case "1":
                                await SearchGames(networkDataHelper);
                                break;
                            case "2":
                                await ShowAllGameInformation(networkDataHelper, client);
                                break;
                            case "3":
                                await PurchaseGame(networkDataHelper, connectedUser);
                                break;
                            case "4":
                                await ReviewGame(networkDataHelper, connectedUser);
                                break;
                            case "5":
                                await PublishGame(networkDataHelper, connectedUser, client);
                                break;
                            case "6":
                                await EditPublishedGame(networkDataHelper, connectedUser, client);
                                break;
                            case "7":
                                await DeleteGame(networkDataHelper, connectedUser);
                                break;
                            case "8":
                                connectedUser = null!;
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
                    await networkDataHelper.Send(responseDataLength);
                    await networkDataHelper.Send(responseData);
                }
                catch (InvalidOperationException ex)
                {
                    var response = ex.Message;
                    var responseData = Encoding.UTF8.GetBytes(response);
                    var responseDataLength = BitConverter.GetBytes(responseData.Length);
                    await networkDataHelper.Send(responseDataLength);
                    await networkDataHelper.Send(responseData);
                }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client: {ex.Message}");
        }
        finally
        {
            lock (_lock)
            {
                _connectedClients.Remove(client);
            }

            client.Close();
            Console.WriteLine("Client disconnected.");
        }
    }

    private static async Task RegisterNewUser(NetworkDataHelper networkDataHelper)
    {
        var usernameLengthData = await networkDataHelper.Receive(LargoDataLength);
        var usernameLength = BitConverter.ToInt32(usernameLengthData);
        var usernameData = await networkDataHelper.Receive(usernameLength);
        var username = Encoding.UTF8.GetString(usernameData);

        var passwordLengthData = await networkDataHelper.Receive(LargoDataLength);
        var passwordLength = BitConverter.ToInt32(passwordLengthData);
        var passwordData = await networkDataHelper.Receive(passwordLength);
        var password = Encoding.UTF8.GetString(passwordData);

        if (password.Length < 4)
            throw new ArgumentException("Password must be at least 4 characters long.");
        if (username.Length < 4) throw new ArgumentException("Username must be at least 4 characters long.");

        Console.WriteLine("Database.RegisterNewUser -Initiated");
        Console.WriteLine("Database.RegisterNewUser -Executing");
        if (UserManager.RegisterUser(username, password))
        {
            Console.WriteLine("Database.RegisterNewUser - New User: " + username + " Registered");
            await SuccesfulResponse("User registered successfully", networkDataHelper);
        }
        else
        {
            throw new InvalidOperationException("User already exists.");
        }
    }

    private static async Task<User> LoginUser(NetworkDataHelper networkDataHelper)
    {
        var usernameLengthData = await networkDataHelper.Receive(LargoDataLength);
        var usernameLength = BitConverter.ToInt32(usernameLengthData);
        var usernameData = await networkDataHelper.Receive(usernameLength);
        var username = Encoding.UTF8.GetString(usernameData);

        var passwordLengthData = await networkDataHelper.Receive(LargoDataLength);
        var passwordLength = BitConverter.ToInt32(passwordLengthData);
        var passwordData = await networkDataHelper.Receive(passwordLength);
        var password = Encoding.UTF8.GetString(passwordData);

        Console.WriteLine("Database.LoginUser -Initiated");
        Console.WriteLine("Database.LoginUser -Executing");
        var user = UserManager.AuthenticateUser(username, password);
        if (user == null) throw new InvalidOperationException("Invalid username or password.");
        await SuccesfulResponse("Login successful", networkDataHelper);
        Console.WriteLine("User " + user.Username + " has logged in.");
        SendMom.SendMessageToMom("New Login User: " + user.Username);
        return user;
    }

    private static async Task ShowAllGameInformation(NetworkDataHelper networkDataHelper, TcpClient socketClient)
    {
        Console.WriteLine("Database.ShowAllGameInformation -Initiated");
        Console.WriteLine("Database.ShowAllGameInformation -Executing");

        var gameIdLengthData = await networkDataHelper.Receive(LargoDataLength);
        var gameIdLength = BitConverter.ToInt32(gameIdLengthData);
        var gameIdData = await networkDataHelper.Receive(gameIdLength);
        var gameName = Encoding.UTF8.GetString(gameIdData);

        var game = GameManager.GetGameByName(gameName);
        if (game != null)
        {
            var response = game.ToString();
            await SuccesfulResponse(response, networkDataHelper);

            Console.WriteLine("Checking if image file exists...");
            var abspath = Path.Combine(Directory.GetCurrentDirectory(), "Images", game.Name + ".jpg");

            if (File.Exists(abspath))
            {
                Console.WriteLine("Sending File...");
                var fileCommonHandler = new FileCommsHandler(socketClient);
                await fileCommonHandler.SendFile(abspath);
                Console.WriteLine("File Sent Successfully!");
            }
            else
            {
                Console.WriteLine("Image file not found, skipping file send.");
                await SuccesfulResponse("No image available", networkDataHelper); // Envía el mensaje al cliente
            }

            var option = await ReceiveStringData(networkDataHelper);
            switch (option)
            {
                case "yes":
                    var reviews = new StringBuilder("Reviews:\n");
                    foreach (var review in game.Reviews)
                        reviews.Append("\n- " + review.Description + " - Valoration: " + review.Valoration);
                    await SuccesfulResponse(reviews.ToString(), networkDataHelper);
                    break;

                case "no":
                    await SuccesfulResponse("Enjoy your game info!", networkDataHelper);
                    break;
            }
        }
        else
        {
            throw new InvalidOperationException("Game not found.");
        }
    }
    public async Task PublishGamegRCP(NetworkDataHelper networkDataHelper, User connectedUser, TcpClient client)
    {
        Console.WriteLine("Database.PublishGame - hola");
        Console.WriteLine("Database.PublishGame - Executing");

        // Obtener el nombre del juego y verificar si ya existe
        string gameName = null;
        while (string.IsNullOrEmpty(gameName))
        {
            try
            {
                gameName = await ReceiveStringData(networkDataHelper);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving game name: {ex.Message}");
                await Task.Delay(100); // Esperar brevemente antes de intentar nuevamente
            }
        }

        var gameExists = GameManager.DoesGameExist(gameName);
        while (gameExists)
        {
            await SuccesfulResponse("Error: That Game Already Exists.", networkDataHelper);
            gameName = await ReceiveStringData(networkDataHelper);
            gameExists = GameManager.DoesGameExist(gameName);
        }

        await SuccesfulResponse("Successful New Game Name", networkDataHelper);

        // Continuar con el flujo normal
        var genre = await ReceiveStringData(networkDataHelper);
        var releaseDateInput = await ReceiveStringData(networkDataHelper);
        if (!DateTime.TryParseExact(releaseDateInput, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var releaseDate))
        {
            await SuccesfulResponse("Error: Invalid Date Format. Please use dd/MM/yyyy.", networkDataHelper);
            return;
        }

        var platform = await ReceiveStringData(networkDataHelper);
        var unitsAvailableInput = await ReceiveStringData(networkDataHelper);
        if (!int.TryParse(unitsAvailableInput, out var unitsAvailable))
        {
            await SuccesfulResponse("Error: Invalid Number Format for Units Available.", networkDataHelper);
            return;
        }

        var priceInput = await ReceiveStringData(networkDataHelper);
        if (!int.TryParse(priceInput, out var price))
        {
            await SuccesfulResponse("Error: Invalid Number Format for Price.", networkDataHelper);
            return;
        }

        var valoration = 0;
        var newGame = GameManager.CreateNewGame(gameName, genre, releaseDate, platform, unitsAvailable, price, valoration, connectedUser);

        UserManager.PublishGame(newGame, connectedUser);

        await SuccesfulResponse($"Game '{gameName}' Published Successfully.", networkDataHelper);
    }




    private async Task PublishGame(NetworkDataHelper networkDataHelper, User connectedUser, TcpClient client)
    {
        Console.WriteLine("Database.PublishGame -Initiated");
        Console.WriteLine("Database.PublishGame -Executing");

        var gameName = await ReceiveStringData(networkDataHelper);
        var gameExists = GameManager.DoesGameExist(gameName);
        while (gameExists)
        {
            await SuccesfulResponse("Error: That Games Already Exist.", networkDataHelper);
            gameName = await ReceiveStringData(networkDataHelper);
            gameExists = GameManager.DoesGameExist(gameName);
            if (!gameExists) await SuccesfulResponse("Succesful New Game Name", networkDataHelper);
        }

        await SuccesfulResponse("Succesful New Game Name", networkDataHelper);
        var genre = await ReceiveStringData(networkDataHelper);
        var releaseDateInput = await ReceiveStringData(networkDataHelper);
        var releaseDate = DateTime.Parse(releaseDateInput);
        var platform = await ReceiveStringData(networkDataHelper);
        var unitsAvailable = int.Parse(await ReceiveStringData(networkDataHelper));
        var price = int.Parse(await ReceiveStringData(networkDataHelper));
        var variableSubida = await ReceiveStringData(networkDataHelper);
        await SuccesfulResponse(variableSubida, networkDataHelper);
        if (variableSubida == "yes")
        {
            Console.WriteLine("Image incoming...");
            var fileCommonHandler = new FileCommsHandler(client);
            await fileCommonHandler.ReceiveFile(gameName);
            Console.WriteLine("Image received!");
        }

        var valoration = 0;
        var newGame = GameManager.CreateNewGame(gameName, genre, releaseDate, platform, unitsAvailable, price,
            valoration, connectedUser);
        UserManager.PublishGame(newGame, connectedUser);
        SendMom.SendMessageToMom("New Game Published: " +gameName + "-" + genre + "-" + releaseDate + "-" + platform + "-" + unitsAvailable + "-" + price + "-" +valoration + "-" + connectedUser);
    }

    private static async Task<string> ReceiveStringData(NetworkDataHelper networkDataHelper)
    {
        var dataLength = await networkDataHelper.Receive(LargoDataLength);
        var data = await networkDataHelper.Receive(BitConverter.ToInt32(dataLength));
        return Encoding.UTF8.GetString(data);
    }

    private static async Task DeleteGame(NetworkDataHelper networkDataHelper, User connectedUser)
    {
        var gameNameLengthData = await networkDataHelper.Receive(LargoDataLength);
        var gameNameLength = BitConverter.ToInt32(gameNameLengthData);
        var gameNameData = await networkDataHelper.Receive(gameNameLength);
        var gameName = Encoding.UTF8.GetString(gameNameData);

        if (GameManager.DoesGameExist(gameName))
        {
            if (connectedUser.PublishedGames.Contains(GameManager.GetGameByName(gameName)))
            {
                GameManager.RemoveGame(gameName);

                // Delete the image file
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Images", $"{gameName}.jpg");
                if (File.Exists(imagePath)) File.Delete(imagePath);

                await SuccesfulResponse("Game and its image deleted successfully.", networkDataHelper);
            }
            else
            {
                throw new InvalidOperationException("You are not the publisher of the game.");
            }
        }
        else
        {
            await SuccesfulResponse("Game not found.", networkDataHelper);
        }
    }

    private static async Task ShowAllGames(NetworkDataHelper networkDataHelper)
    {
        Console.WriteLine("Database.ShowAllGames -Initiated");
        Console.WriteLine("Database.ShowAllGames -Executing");

        var games = GameManager.GetAllGames();
        if (games.Count == 0) throw new InvalidOperationException("No games found.");

        var response = new StringBuilder("All games:\n");
        foreach (var game in games)
            response.Append("\n- " + game.Name + " - Price: " + game.Price + " - Units: " + game.UnitsAvailable);

        await SuccesfulResponse(response.ToString(), networkDataHelper);
    }

    private static async Task SearchGames(NetworkDataHelper networkDataHelper)
    {
        Console.WriteLine("Database.SearchGames -Initiated");
        Console.WriteLine("Database.SearchGames -Executing");
        var option = await ReceiveStringData(networkDataHelper);
        switch (option)
        {
            case "1":
                await ShowAllGamesByGenre(networkDataHelper);
                break;
            case "2":
                await ShowAllGamesByPlatform(networkDataHelper);
                break;
            case "3":
                await ShowAllGamesByValorations(networkDataHelper);
                break;
            case "4":
                await ShowAllGames(networkDataHelper);
                break;
        }
    }

    private static async Task ShowAllGamesByValorations(NetworkDataHelper networkDataHelper)
    {
        var valoration = ReceiveStringData(networkDataHelper);
        Console.WriteLine("Received valoration: " + valoration);

        var distinctGamesByValoration = GameManager.GetGamesByAttribute("Valoration", await valoration);
        Console.WriteLine("Found " + distinctGamesByValoration.Count + " games with valoration " + valoration);

        var response = new StringBuilder("Games with valoration " + valoration + ":\n");
        foreach (var game in distinctGamesByValoration)
            response.Append("\n- " + game.Name + " - Price: " + game.Price + " - Units: " + game.UnitsAvailable);

        await SuccesfulResponse(response.ToString(), networkDataHelper);
    }

    private static async Task ShowAllGamesByGenre(NetworkDataHelper networkDataHelper)
    {
        var genre = await ReceiveStringData(networkDataHelper);
        Console.WriteLine("Received genre: " + genre);

        var distinctGamesByGenre = GameManager.GetGamesByAttribute("Genre", genre);
        Console.WriteLine("Found " + distinctGamesByGenre.Count + " games in genre " + genre);

        var response = new StringBuilder("Games in genre " + genre + ":\n");
        foreach (var game in distinctGamesByGenre)
            response.Append("\n- " + game.Name + " - Price: " + game.Price + " - Units: " + game.UnitsAvailable);

        await SuccesfulResponse(response.ToString(), networkDataHelper);
    }

    private static async Task ShowAllGamesByPlatform(NetworkDataHelper networkDataHelper)
    {
        var platform = await ReceiveStringData(networkDataHelper);
        Console.WriteLine("Received platform: " + platform);

        var distinctGamesByPlatform = GameManager.GetGamesByAttribute("Platform", platform);
        Console.WriteLine("Found " + distinctGamesByPlatform.Count + " games in platform " + platform);

        var response = new StringBuilder("Games in platform " + platform + ":\n");
        foreach (var game in distinctGamesByPlatform)
            response.Append("\n- " + game.Name + " - Price: " + game.Price + " - Units: " + game.UnitsAvailable);

        await SuccesfulResponse(response.ToString(), networkDataHelper);
    }

    private static async Task ShowPublishedGames(NetworkDataHelper networkDataHelper, User connectedUser)
    {
        Console.WriteLine("Database.ShowPublishedGames -Initiated");
        Console.WriteLine("Database.ShowPublishedGames -Executing");
        var response = new StringBuilder("Published games: ");
        foreach (var game in connectedUser.PublishedGames) response.Append(game.Name).Append("\n ");

        if (response.Length > 0) response.Length -= 2;

        await SuccesfulResponse(response.ToString(), networkDataHelper);
    }

    private async Task EditPublishedGame(NetworkDataHelper networkDataHelper, User connectedUser, TcpClient client)
    {
        Console.WriteLine("Database.EditPublishedGame - Initiated");

        var gameName = await ReceiveStringData(networkDataHelper);
        var game = GameManager.GetGameByName(gameName);

        if (game == null || !connectedUser.PublishedGames.Contains(game))
        {
            await SuccesfulResponse("Error: Game not found or you are not the publisher.", networkDataHelper);
            return;
        }

        await SuccesfulResponse("Game found. You can modify it.", networkDataHelper);

        var modifying = true;
        while (modifying)
        {
            var action = await ReceiveStringData(networkDataHelper);

            switch (action)
            {
                case "modifyField":
                    var field = await ReceiveStringData(networkDataHelper);
                    var newValue = await ReceiveStringData(networkDataHelper);
                    await UpdateGameField(game, field, newValue, networkDataHelper);
                    break;

                case "coverImageConfirmation":
                    var uploadImage = await ReceiveStringData(networkDataHelper);
                    if (uploadImage == "yes")
                    {
                        var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Images", $"{game.Name}.jpg");
                        if (File.Exists(imagePath))
                        {
                            File.Delete(imagePath);
                            Console.WriteLine("Existing image deleted.");
                        }

                        Console.WriteLine("Image incoming...");
                        var fileCommonHandler = new FileCommsHandler(client);
                        await fileCommonHandler.ReceiveFile(game.Name);
                        Console.WriteLine("Image received!");
                        await SuccesfulResponse("Cover image updated successfully", networkDataHelper);
                    }
                    else
                    {
                        await SuccesfulResponse("Cover image upload was skipped.", networkDataHelper);
                    }

                    break;

                case "finishModification":
                    modifying = false;
                    Console.WriteLine("Modification finished for game: " + game.Name);
                    break;

                default:
                    await SuccesfulResponse("Error: Unknown action.", networkDataHelper);
                    break;
            }
        }
    }

    private async Task UpdateGameField(Game game, string field, string newValue, NetworkDataHelper networkDataHelper)
    {
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
                default:
                    throw new ArgumentException("Invalid field.");
            }

            await SuccesfulResponse("Game edited successfully", networkDataHelper);
        }
        catch (ArgumentException ex)
        {
            await SuccesfulResponse($"Error: {ex.Message}", networkDataHelper);
        }
    }


    private async Task PurchaseGame(NetworkDataHelper networkDataHelper, User connectedUser)
    {
        var gameNameLengthData = await networkDataHelper.Receive(LargoDataLength);
        var gameNameLength = BitConverter.ToInt32(gameNameLengthData);
        var gameNameData = await networkDataHelper.Receive(gameNameLength);
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
            await SuccesfulResponse("Game purchased successfully", networkDataHelper);
            SendMom.SendMessageToMom("Buy Game: " + gameName);
        }
        else
        {
            throw new InvalidOperationException("Error purchasing the game.");
        }
    }

    private async Task ReviewGame(NetworkDataHelper networkDataHelper, User connectedUser)
    {
        var gameName = await ReceiveStringData(networkDataHelper);
        var game = GameManager.GetGameByName(gameName);
        if (game == null) throw new InvalidOperationException("Error: Game not found.");

        if (!connectedUser.PurchasedGames.Contains(game))
            throw new InvalidOperationException("Error: You must purchase the game to review it.");

        await SuccesfulResponse("Review Added Successfully", networkDataHelper);
        var reviewText = await ReceiveStringData(networkDataHelper);
        if (string.IsNullOrEmpty(reviewText)) reviewText = "No review";

        var valoration = await ReceiveStringData(networkDataHelper);
        var review = new Review { Valoration = int.Parse(valoration), Description = reviewText };
        GameManager.AddReview(gameName, review);
        GameManager.AddValoration(gameName, int.Parse(valoration));
        await SuccesfulResponse("Thanks For Your Collaboration!", networkDataHelper);
    }

    private static async Task SuccesfulResponse(string message, NetworkDataHelper networkDataHelper)
    {
        var responseData = Encoding.UTF8.GetBytes(message);
        var responseDataLength = BitConverter.GetBytes(responseData.Length);
        await networkDataHelper.Send(responseDataLength);
        await networkDataHelper.Send(responseData);
    } // Este metodo envia un mensaje de respuesta exitosa al cliente

    private static async Task<string> ProtocolMessage(NetworkDataHelper networkDataHelper)
    {
        var dataLength = await networkDataHelper.Receive(LargoDataLength); // Recibo la parte fija de los datos
        var data = await networkDataHelper.Receive(
            BitConverter.ToInt32(dataLength)); // Recibo los datos(parte variable)
        Console.Write("Client says:");
        var message = Encoding.UTF8.GetString(data);

        var response = $"Option '{message}' received successfully";

        var responseData = Encoding.UTF8.GetBytes(response);
        var responseDataLength = BitConverter.GetBytes(responseData.Length);

        await networkDataHelper.Send(responseDataLength);
        await networkDataHelper.Send(responseData);

        Console.WriteLine(message);
        return message;
    } // Este metodo recibe un mensaje del cliente y envia una respuesta exitosa
}