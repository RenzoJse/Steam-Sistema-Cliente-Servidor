using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using ServerApp.MomMessage;

namespace ClientegRPC
{
    internal class Program
    {
        private static readonly SendMom SendMom = new SendMom();
        public static async Task Main(string[] args)
        {
            using var channel = GrpcChannel.ForAddress("http://localhost:5014");
            var client = new GameManagement.GameManagementClient(channel);

            while (true)
            {
                Console.WriteLine("\n--- MENU ---");
                Console.WriteLine("1. Add a Game");
                Console.WriteLine("2. Delete a Game");
                Console.WriteLine("3. Modify a Game");
                Console.WriteLine("4. View Game Reviews");
                Console.WriteLine("5. Subscribe to Next Purchases Queue");
                Console.WriteLine("6. Exit");
                Console.Write("Select an option: ");
                var option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        await AddGame(client);
                        break;
                    case "2":
                        await DeleteGame(client);
                        break;
                    case "3":
                        await ModifyGame(client);
                        break;
                    case "4":
                        await ViewGameReviews(client);
                        break;
                    case "5":
                        await SubscribeToNextPurchasesQueue();
                        break;
                    case "6":
                        Console.WriteLine("Exiting...");
                        return;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
        }

        private static async Task SubscribeToNextPurchasesQueue()
        {
            int n;
            while (true)
            {
                Console.Write("How many purchases you wanna see? : ");
                if (int.TryParse(Console.ReadLine(), out n))
                {
                    SendMom.SendMessageToMom("View Next Purchases:" + n);
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid input. Please enter a valid integer.");
                }
            }

            SendMom.SuscribeToMom(n);
            await Task.CompletedTask;
        }

        private static async Task AddGame(GameManagement.GameManagementClient client)
        {
            try
            {
                using var call = client.AddGameInteractive();

                Console.Write("Enter the game title: ");
                var name = Console.ReadLine();
                await SendData(call, "name", name);

                Console.Write("Enter the genre: ");
                var genre = Console.ReadLine();
                await SendData(call, "genre", genre);

                Console.Write("Enter the release date (dd/MM/yyyy): ");
                var releaseDate = Console.ReadLine();
                while (!DateTime.TryParseExact(releaseDate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out _))
                {
                    Console.WriteLine("Invalid date format. Please use dd/MM/yyyy.");
                    releaseDate = Console.ReadLine();
                }
                await SendData(call, "releasedate", releaseDate);

                Console.Write("Enter the platform: ");
                var platform = Console.ReadLine();
                await SendData(call, "platform", platform);

                Console.Write("Enter the number of units available: ");
                var unitsAvailable = Console.ReadLine();
                while (!int.TryParse(unitsAvailable, out _))
                {
                    Console.WriteLine("Invalid input. Please enter a valid integer for units available.");
                    unitsAvailable = Console.ReadLine();
                }
                await SendData(call, "unitsavailable", unitsAvailable);

                Console.Write("Enter the price: ");
                var price = Console.ReadLine();
                while (!int.TryParse(price, out _))
                {
                    Console.WriteLine("Invalid input. Please enter a valid integer for the price.");
                    price = Console.ReadLine();
                }
                await SendData(call, "price", price);

                Console.Write("Enter the owner's username: ");
                var username = Console.ReadLine();
                await SendData(call, "username", username);

                await call.RequestStream.CompleteAsync();

                while (await call.ResponseStream.MoveNext())
                {
                    var response = call.ResponseStream.Current.Message;
                    Console.WriteLine($"Server: {response}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Operation failed: {ex.Message}");
            }
        }

        private static async Task SendData(AsyncDuplexStreamingCall<GameData, ServerResponse> call, string key, string value)
        {
            await call.RequestStream.WriteAsync(new GameData { Key = key, Value = value });
            if (await call.ResponseStream.MoveNext())
            {
                var response = call.ResponseStream.Current.Message;
                Console.WriteLine($"Server: {response}");
                if (response.StartsWith("Error")) throw new InvalidOperationException(response);
            }
        }

        private static async Task DeleteGame(GameManagement.GameManagementClient client)
        {
            Console.Write("Enter the name of the game to delete: ");
            var gameName = Console.ReadLine();

            try
            {
                var response = await client.RemoveGameAsync(new RemoveGameRequest { GameName = gameName });
                Console.WriteLine($"Server: {response.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static async Task ModifyGame(GameManagement.GameManagementClient client)
        {
            Console.Write("Enter the name of the game to modify: ");
            var gameName = Console.ReadLine();

            try
            {
                var checkResponse = await client.ModifyGameAsync(new ModifyGameRequest
                {
                    GameName = gameName,
                    Field = "check",
                    NewValue = ""
                });

                if (checkResponse.Message.StartsWith("Error"))
                {
                    Console.WriteLine($"Server: {checkResponse.Message}");
                    return;
                }

                Console.WriteLine("\n--- Select field to modify ---");
                Console.WriteLine("1. Title");
                Console.WriteLine("2. Genre");
                Console.WriteLine("3. Release Date");
                Console.WriteLine("4. Platform");
                Console.WriteLine("5. Units Available");
                Console.WriteLine("6. Price");
                Console.Write("Select an option: ");
                var option = Console.ReadLine();

                string field = option switch
                {
                    "1" => "name",
                    "2" => "genre",
                    "3" => "release date",
                    "4" => "platform",
                    "5" => "units available",
                    "6" => "price",
                    _ => null
                };

                if (field == null)
                {
                    Console.WriteLine("Invalid option. Operation cancelled.");
                    return;
                }

                Console.Write($"Enter the new value for {field}: ");
                var newValue = Console.ReadLine();

                if (field == "release date" && !DateTime.TryParseExact(newValue, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out _))
                {
                    Console.WriteLine("Invalid date format. Please use dd/MM/yyyy.");
                    return;
                }

                if ((field == "units available" || field == "price") && !int.TryParse(newValue, out _))
                {
                    Console.WriteLine($"Invalid input. {field} must be a valid integer.");
                    return;
                }

                var modifyResponse = await client.ModifyGameAsync(new ModifyGameRequest
                {
                    GameName = gameName,
                    Field = field,
                    NewValue = newValue
                });

                Console.WriteLine($"Server: {modifyResponse.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static async Task ViewGameReviews(GameManagement.GameManagementClient client)
        {
            Console.Write("Enter the name of the game to view reviews: ");
            var gameName = Console.ReadLine();

            try
            {
                var response = await client.GetGameReviewsAsync(new GetGameReviewsRequest { GameName = gameName });

                Console.WriteLine($"Server: {response.Message}");

                if (response.Reviews.Count > 0)
                {
                    foreach (var review in response.Reviews)
                    {
                        Console.WriteLine($"- Valoration: {review.Valoration}, Review: {review.Description}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
