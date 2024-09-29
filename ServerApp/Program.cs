using System.Net.Sockets;
using System.Net;
using System.Text;
using Comunicacion;
using Comunicacion.Dominio;
using ServerApp.DataAccess;
using ServerApp.Services;
using Communication;

namespace ServerApp
{
    internal class Program
    {
        static readonly SettingsManager settingsMngr = new SettingsManager();
        static readonly UserManager userManager = new UserManager();
        static readonly GameManager GameManager = new GameManager();
        static List<Socket> clientSockets = new List<Socket>();
        const int largoDataLength = 4; // Pasar a una clase con constantes del protocolo
        static bool serverRunning = true;
        private static object _lock = new object();

        public static UserManager getInstance()
        {
            return userManager;
        }

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
                string command = Console.ReadLine();
                if (command == "shutdown")
                {
                    foreach (var client in clientSockets)
                    {
                        client.Close();
                    }

                    socketServer.Close();
                    serverRunning = false;
                    Console.WriteLine("Server is shutting down...");
                }
            }).Start();

            while (serverRunning)
            {
                try
                {
                    var programInstance = new Program();
                    Socket
                        clientSocket =
                            socketServer.Accept(); // El accept es bloqueante, espera hasta que llega una nueva conexión
                    clientSockets.Add(clientSocket);
                    Console.WriteLine("Client connected");
                    new Thread(() => HandleClient(clientSocket, programInstance))
                        .Start(); // Lanzamos un nuevo hilo para manejar al nuevo cliente
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Server has been shut down.");
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
                        while (connectedUser == null)
                        {
                            switch (protocolMessage(networkDataHelper))
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

                        while (connectedUser != null)
                        {
                            switch (protocolMessage(networkDataHelper))
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
                                    if (connectedUser.PublishedGames.Count == 0)
                                    {
                                        string response = $"You Dont Own Any Game.";
                                        byte[] responseData = Encoding.UTF8.GetBytes(response);
                                        byte[] responseDataLength = BitConverter.GetBytes(responseData.Length);
                                        networkDataHelper.Send(responseDataLength);
                                        networkDataHelper.Send(responseData);
                                    }
                                    else
                                    {
                                        program.ShowPublishedGames(networkDataHelper, connectedUser);
                                        program.EditPublishedGame(networkDataHelper, connectedUser);
                                    }
                                    break;
                                case "7":
                                    program.DeleteGame(networkDataHelper, connectedUser);
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
        
        private void RegisterNewUser(NetworkDataHelper networkDataHelper)
        {
            byte[] usernameLength = networkDataHelper.Receive(largoDataLength);
            byte[] usernameData = networkDataHelper.Receive(BitConverter.ToInt32(usernameLength));
            string username = Encoding.UTF8.GetString(usernameData);

            byte[] passwordLength = networkDataHelper.Receive(largoDataLength);
            byte[] passwordData = networkDataHelper.Receive(BitConverter.ToInt32(passwordLength));
            string password = Encoding.UTF8.GetString(passwordData);

            if (password.Length < 4)
            {
                throw new ArgumentException("Password must be at least 4 characters long.");
            }
            else if (username.Length < 4)
            {
                throw new ArgumentException("Username must be at least 4 characters long.");
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

        private void ShowAllGameInformation(NetworkDataHelper networkDataHelper, Socket socketClient)
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
                
                Console.WriteLine("Sending File...");
                String abspath = Path.Combine(Directory.GetCurrentDirectory(), "Images", game.ImageName);
                var fileCommonHandler = new FileCommsHandler(socketClient);
                fileCommonHandler.SendFile(abspath);
                Console.WriteLine("File Sent Successfully!");
                
                string option = ReceiveStringData(networkDataHelper);
                if (option.Equals("yes"))
                {
                    StringBuilder reviews = new StringBuilder("Reviews:\n");
                    foreach (var review in game.Reviews)
                    {
                        reviews.Append("\n- " + review.Description + " - Valoration: " + review.Valoration);
                    }
                    SuccesfulResponse(reviews.ToString(), networkDataHelper);
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

            string gameName = ReceiveStringData(networkDataHelper);
            bool gameExists = GameManager.DoesGameExist(gameName);
            while (gameExists)
            {
                SuccesfulResponse("Error: That Games Already Exist.", networkDataHelper);
                gameName = ReceiveStringData(networkDataHelper);
                gameExists = GameManager.DoesGameExist(gameName);
                if (!gameExists)
                {
                    SuccesfulResponse("Succesful New Game Name", networkDataHelper);
                }
            }

            SuccesfulResponse("Succesful New Game Name", networkDataHelper);
            string genre = ReceiveStringData(networkDataHelper);
            string releaseDateInput = ReceiveStringData(networkDataHelper);
            DateTime releaseDate = DateTime.Parse(releaseDateInput);
            string platform = ReceiveStringData(networkDataHelper);
            int unitsAvailable = int.Parse(ReceiveStringData(networkDataHelper));
            int price = int.Parse(ReceiveStringData(networkDataHelper));
            string variableSubida = ReceiveStringData(networkDataHelper);
            SuccesfulResponse(variableSubida, networkDataHelper);
            if (variableSubida == "yes")
            {
                Console.WriteLine("Image incoming...");
                var fileCommonHandler = new FileCommsHandler(socketClient);
                fileCommonHandler.ReceiveFile(gameName);
                Console.WriteLine("Image received!");
            }

            int valoration = 0;
            Game newGame = CreateNewGame(gameName, genre, releaseDate, platform, unitsAvailable, price, valoration,
                connectedUser);
            GameManager.AddGame(newGame);
            connectedUser.PublishedGames.Add(newGame);
        }

        private string ReceiveStringData(NetworkDataHelper networkDataHelper)
        {
            byte[] dataLength = networkDataHelper.Receive(largoDataLength);
            byte[] data = networkDataHelper.Receive(BitConverter.ToInt32(dataLength));
            return Encoding.UTF8.GetString(data);
        }

        private Game CreateNewGame(string name, string genre, DateTime releaseDate, string platform, int unitsAvailable,
            int price, int valoration, User owner)
        {
            return new Game
            {
                Name = name,
                Genre = genre,
                ReleaseDate = releaseDate,
                Platform = platform,
                Publisher = owner.Username,
                UnitsAvailable = unitsAvailable,
                Price = price,
                Valoration = valoration,
                Reviews = new List<Review>(),
                ImageName = name
            };
        }

        private void DeleteGame(NetworkDataHelper networkDataHelper, User connectedUser)
        {
            byte[] gameNameLength = networkDataHelper.Receive(largoDataLength);
            byte[] gameNameData = networkDataHelper.Receive(BitConverter.ToInt32(gameNameLength));
            string gameName = Encoding.UTF8.GetString(gameNameData);


            if (GameManager.DoesGameExist(gameName))
            {
                if (connectedUser.PublishedGames.Contains(GameManager.GetGameByName(gameName)))
                {
                    GameManager.RemoveGame(gameName);
                    SuccesfulResponse("Game deleted successfully.", networkDataHelper);
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
            if (games.Count == 0)
            {
                throw new InvalidOperationException("No games found.");
            }

            StringBuilder response = new StringBuilder("All games:\n");
            foreach (var game in games)
            {
                response.Append("\n- " + game.Name + " - Price: " + game.Price + " - Units: " + game.UnitsAvailable);
            }

            SuccesfulResponse(response.ToString(), networkDataHelper);
        }

        private void SearchGames(NetworkDataHelper networkDataHelper)
        {
            Console.WriteLine("Database.SearchGames -Initiated");
            Console.WriteLine("Database.SearchGames -Executing");
            string option = ReceiveStringData(networkDataHelper);
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
            string valoration = ReceiveStringData(networkDataHelper);
            Console.WriteLine("Received valoration: " + valoration);

            var distinctGamesByValoration = GameManager.GetGamesByAttribute("Valoration", valoration);
            Console.WriteLine("Found " + distinctGamesByValoration.Count + " games with valoration " + valoration);

            StringBuilder response = new StringBuilder("Games with valoration " + valoration + ":\n");
            foreach (var game in distinctGamesByValoration)
            {
                response.Append("\n- " + game.Name + " - Price: " + game.Price + " - Units: " + game.UnitsAvailable);
            }

            SuccesfulResponse(response.ToString(), networkDataHelper);
        }

        private void ShowAllGamesByGenre(NetworkDataHelper networkDataHelper)
        {
            string genre = ReceiveStringData(networkDataHelper);
            Console.WriteLine("Received genre: " + genre);

            var distinctGamesByGenre = GameManager.GetGamesByAttribute("Genre", genre);
            Console.WriteLine("Found " + distinctGamesByGenre.Count + " games in genre " + genre);

            StringBuilder response = new StringBuilder("Games in genre " + genre + ":\n");
            foreach (var game in distinctGamesByGenre)
            {
                response.Append("\n- " + game.Name + " - Price: " + game.Price + " - Units: " + game.UnitsAvailable);
            }

            SuccesfulResponse(response.ToString(), networkDataHelper);
        }

        private void ShowAllGamesByPlatform(NetworkDataHelper networkDataHelper)
        {
            string platform = ReceiveStringData(networkDataHelper);
            Console.WriteLine("Received platform: " + platform);

            var distinctGamesByPlatform = GameManager.GetGamesByAttribute("Platform", platform);
            Console.WriteLine("Found " + distinctGamesByPlatform.Count + " games in platform " + platform);

            StringBuilder response = new StringBuilder("Games in platform " + platform + ":\n");
            foreach (var game in distinctGamesByPlatform)
            {
                response.Append("\n- " + game.Name + " - Price: " + game.Price + " - Units: " + game.UnitsAvailable);
            }

            SuccesfulResponse(response.ToString(), networkDataHelper);
        }

        private void ShowPublishedGames(NetworkDataHelper networkDataHelper, User connectedUser)
        {
            Console.WriteLine("Database.ShowPublishedGames -Initiated");
            Console.WriteLine("Database.ShowPublishedGames -Executing");
            StringBuilder response = new StringBuilder("Published games: ");
            foreach (var game in connectedUser.PublishedGames)
            {
                response.Append(game.Name).Append("\n ");
            }

            if (response.Length > 0)
            {
                response.Length -= 2;
            }

            SuccesfulResponse(response.ToString(), networkDataHelper);
        }

        private void EditPublishedGame(NetworkDataHelper networkDataHelper, User connectedUser)
        {
            Console.WriteLine("Database.EditPublishedGame -Initiated");
            Console.WriteLine("Database.EditPublishedGame -Executing");

            byte[] gameNameLength = networkDataHelper.Receive(largoDataLength);
            byte[] gameNameData = networkDataHelper.Receive(BitConverter.ToInt32(gameNameLength));
            string gameName = Encoding.UTF8.GetString(gameNameData);

            Game game = GameManager.GetGameByName(gameName);
            if (game != null && connectedUser.PublishedGames.Contains(game))
            {
                byte[] fieldLength = networkDataHelper.Receive(largoDataLength);
                byte[] fieldData = networkDataHelper.Receive(BitConverter.ToInt32(fieldLength));
                string field = Encoding.UTF8.GetString(fieldData);

                byte[] newValueLength = networkDataHelper.Receive(largoDataLength);
                byte[] newValueData = networkDataHelper.Receive(BitConverter.ToInt32(newValueLength));
                string newValue = Encoding.UTF8.GetString(newValueData);

                switch (field.ToLower())
                {
                    case "title":
                        game.Name = newValue;
                        break;
                    case "genre":
                        game.Genre = newValue;
                        break;
                    case "release date":
                        if (DateTime.TryParse(newValue, out DateTime newReleaseDate))
                        {
                            game.ReleaseDate = newReleaseDate;
                        }
                        else
                        {
                            throw new ArgumentException("Invalid date format.");
                        }

                        break;
                    case "platform":
                        game.Platform = newValue;
                        break;
                    case "publisher":
                        game.Publisher = newValue;
                        break;
                    case "units available":
                        if (int.TryParse(newValue, out int newUnitsAvailable))
                        {
                            game.UnitsAvailable = newUnitsAvailable;
                        }
                        else
                        {
                            throw new ArgumentException("Invalid number format.");
                        }

                        break;
                    default:
                        throw new ArgumentException("Invalid field.");
                }

                SuccesfulResponse("Game edited successfully", networkDataHelper);
            }
            else
            {
                throw new InvalidOperationException("Game not found or user is not the publisher.");
            }
        }

        private void PurchaseGame(NetworkDataHelper networkDataHelper, User connectedUser)
        {
            lock (this)
            {
                byte[] gameNameLength = networkDataHelper.Receive(largoDataLength);
                byte[] gameNameData = networkDataHelper.Receive(BitConverter.ToInt32(gameNameLength));
                string gameName = Encoding.UTF8.GetString(gameNameData);

                Game game = GameManager.GetGameByName(gameName);

                if (game == null)
                {
                    throw new InvalidOperationException("El juego no existe.");
                }
                else if (game.UnitsAvailable <= 0)
                {
                    throw new InvalidOperationException("No hay unidades disponibles.");
                }

                Console.WriteLine("Database.PurchaseGame -Initiated");
                Console.WriteLine("Database.PurchaseGame -Executing");
                if (userManager.PurchaseGame(game, connectedUser))
                {
                    GameManager.DiscountPurchasedGame(game);
                    Console.WriteLine("Database.PurchaseGame - El juego: " + game.Name + " ha sido comprado");
                    SuccesfulResponse("Juego comprado exitosamente", networkDataHelper);
                }
                else
                {
                    throw new InvalidOperationException("Error al comprar el juego.");
                }
            }
        }

        public void ReviewGame(NetworkDataHelper networkDataHelper, User connectedUser)
        {
            string gameName = ReceiveStringData(networkDataHelper);
            Game game = GameManager.GetGameByName(gameName);
            if (game == null)
            {
                throw new InvalidOperationException("Error: Game not found.");
            }
            if (!connectedUser.PurchasedGames.Contains(game))
            {
                throw new InvalidOperationException("Error: You must purchase the game to review it.");
            }
            SuccesfulResponse("Review Added Successfully", networkDataHelper);
            string reviewText = ReceiveStringData(networkDataHelper);
            if (string.IsNullOrEmpty(reviewText))
            {
                reviewText = "No review";
            }
            string valoration = ReceiveStringData(networkDataHelper);
            Review review = new Review
            {
                Valoration = int.Parse(valoration),
                Description = reviewText
            };
            game.Reviews.Add(review);
            GameManager.AddValoration(gameName, int.Parse(valoration));
            SuccesfulResponse("Thanks For Your Collaboration!", networkDataHelper);
        }

        private void SuccesfulResponse(string message, NetworkDataHelper networkDataHelper)
        {
            byte[] responseData = Encoding.UTF8.GetBytes(message);
            byte[] responseDataLength = BitConverter.GetBytes(responseData.Length);
            networkDataHelper.Send(responseDataLength);
            networkDataHelper.Send(responseData);
        } // Este metodo envia un mensaje de respuesta exitosa al cliente

        private static string protocolMessage(NetworkDataHelper networkDataHelper)
        {
            byte[] dataLength = networkDataHelper.Receive(largoDataLength); // Recibo la parte fija de los datos
            byte[] data =
                networkDataHelper.Receive(BitConverter.ToInt32(dataLength)); // Recibo los datos(parte variable)
            Console.Write("Client says:");
            string message = Encoding.UTF8.GetString(data);

            string response = $"Option '{message}' received successfully";

            byte[] responseData = Encoding.UTF8.GetBytes(response);
            byte[] responseDataLength = BitConverter.GetBytes(responseData.Length);

            networkDataHelper.Send(responseDataLength);
            networkDataHelper.Send(responseData);

            Console.WriteLine(message);
            return message;
        } // Este metodo recibe un mensaje del cliente y envia una respuesta exitosa
    }
}