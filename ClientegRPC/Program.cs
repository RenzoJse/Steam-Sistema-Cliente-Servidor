using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Grpc.Core;

namespace ClientegRPC
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            using var channel = GrpcChannel.ForAddress("http://localhost:5014");
            var client = new Game.GameClient(channel);

            try
            {
                Console.WriteLine("Welcome to the Game Publishing System!");

                // Solicitar datos al usuario para agregar un juego
                Console.Write("Enter the game title: ");
                string gameName = Console.ReadLine();

                Console.Write("Enter the genre: ");
                string genre = Console.ReadLine();

                Console.Write("Enter the release date (dd/MM/yyyy): ");
                string releaseDateInput = Console.ReadLine();
                DateTime releaseDate;
                while (!DateTime.TryParseExact(releaseDateInput, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out releaseDate))
                {
                    Console.WriteLine("Invalid date format. Please enter the date in the format dd/MM/yyyy.");
                    Console.Write("Enter the release date (dd/MM/yyyy): ");
                    releaseDateInput = Console.ReadLine();
                }

                Console.Write("Enter the platform: ");
                string platform = Console.ReadLine();

                Console.Write("Enter the number of units available: ");
                string unitsAvailableInput = Console.ReadLine();
                int unitsAvailable;
                while (!int.TryParse(unitsAvailableInput, out unitsAvailable))
                {
                    Console.WriteLine("Invalid input. Please enter a valid integer for the number of units available.");
                    Console.Write("Enter the number of units available: ");
                    unitsAvailableInput = Console.ReadLine();
                }

                Console.Write("Enter the price: ");
                string priceInput = Console.ReadLine();
                int price;
                while (!int.TryParse(priceInput, out price))
                {
                    Console.WriteLine("Invalid input. Please enter a valid integer for the price.");
                    Console.Write("Enter the price: ");
                    priceInput = Console.ReadLine();
                }

                // Crear una solicitud para el servidor gRPC
                var gameRequest = new GameRequest
                {
                    Name = gameName,
                    Genre = genre,
                    ReleaseDate = releaseDate.ToString("dd/MM/yyyy"),
                    Platform = platform,
                    UnitsAvailable = unitsAvailable,
                    Price = price
                };

                // Enviar la solicitud al servidor gRPC
                var gameReply = await client.AddGameAsync(gameRequest);

                // Mostrar la respuesta del servidor
                Console.WriteLine("Server Response: " + gameReply.Message);
            }
            catch (RpcException ex)
            {
                Console.WriteLine($"Error gRPC: {ex.StatusCode} - {ex.Status.Detail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error: {ex.Message}");
            }
        }
    }
}
