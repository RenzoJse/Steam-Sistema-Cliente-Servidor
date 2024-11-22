using System;
using System.Threading.Tasks;
using Grpc.Net.Client;

namespace ClientegRPC
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            using var channel = GrpcChannel.ForAddress("http://localhost:5014");
            var client = new GameManagement.GameManagementClient(channel);

            while (true)
            {
                Console.WriteLine("\n--- MENU ---");
                Console.WriteLine("1. Alta de Juego");
                Console.WriteLine("2. Baja de Juego");
                Console.WriteLine("3. Modificar Juego");
                Console.WriteLine("4. Salir");
                Console.Write("Selecciona una opción: ");
                var option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        await AltaJuego(client);
                        break;
                    case "2":
                        await BajaJuego(client);
                        break;
                    case "3":
                        await ModificarJuego(client);
                        break;
                    case "4":
                        Console.WriteLine("Saliendo...");
                        return;
                    default:
                        Console.WriteLine("Opción inválida. Intenta de nuevo.");
                        break;
                }
            }
        }

        private static async Task AltaJuego(GameManagement.GameManagementClient client)
        {
            // Implementación de alta de juego (ya provista).
        }

        private static async Task BajaJuego(GameManagement.GameManagementClient client)
        {
            // Implementación de baja de juego (ya provista).
        }

        private static async Task ModificarJuego(GameManagement.GameManagementClient client)
        {
            Console.Write("Enter the name of the game to modify: ");
            var gameName = Console.ReadLine();

            Console.Write("Enter the field to modify (name, genre, release date, platform, units available, price): ");
            var field = Console.ReadLine();

            Console.Write("Enter the new value: ");
            var newValue = Console.ReadLine();

            try
            {
                var response = await client.ModifyGameAsync(new ModifyGameRequest
                {
                    GameName = gameName,
                    Field = field,
                    NewValue = newValue
                });
                Console.WriteLine($"Server: {response.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
