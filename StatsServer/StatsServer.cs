using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StatsServer.DataAccess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Comunicacion.Dominio;
using ServerApp.DataAccess;

namespace StatsServer
{
    public class StatsServer
    {
        private readonly StatsData _statsData;
        private readonly GameRepository _gameRepository;
        private readonly IModel _publishChannel;
        private readonly ConcurrentDictionary<string, int> _subscribers = new ConcurrentDictionary<string, int>();

        public StatsServer(StatsData statsData, GameRepository gameRepository)
        {
            _statsData = statsData;
            _gameRepository = gameRepository;
            string rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
            var factory = new ConnectionFactory() { HostName =  rabbitMqHost }; // Defino la conexion

            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            _publishChannel = connection.CreateModel();

            _publishChannel.QueueDeclare(queue: "next_purchases",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            channel.QueueDeclare(queue: "steam_logs",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            //Defino el mecanismo de consumo
            var consumer = new EventingBasicConsumer(channel);
            //Defino el evento que sera invocado cuando llegue un mensaje
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine(" [x] Received {0}", message);
                if (message.Contains("New Login"))
                {
                    _statsData.IncrementTotalLogins();
                    Console.WriteLine("Total logins incremented. Current total: {0}", _statsData.GetTotalLogins());
                }

                if (message.Contains("New Game"))
                {
                    _ = AddNewGame(message);
                }

                if (message.Contains("Buy Game"))
                {
                    _ = ModifyGameUnits(message);
                }

                if (message.Contains("Deleted"))
                {
                    _ = DeleteGame(message);
                }

                if (message.Contains("View Next Purchases:"))
                {
                    var parts = message.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int count))
                    {
                        var clientId = Guid.NewGuid().ToString();
                        _subscribers[clientId] = count;
                    }
                }

                if (message.Contains("Modify"))
                {
                    ModifyGame(message);
                }

            };

            //"PRENDO" el consumo de mensajes
            channel.BasicConsume(queue: "steam_logs",
                autoAck: true,
                consumer: consumer);
        }

        private async Task DeleteGame(string message)
        {
            var gameName = message.Split(new[] { "Deleted: " }, StringSplitOptions.None)[1].Trim();
            await _gameRepository.RemoveGame(gameName);
        }

        private async Task ModifyGameUnits(string message)
        {
            var gameName = message.Split(new[] { "Buy Game: " }, StringSplitOptions.None)[1].Trim();
            var game = _gameRepository.GetGameByName(gameName);
            await _gameRepository.DiscountPurchasedGame(await game);

            foreach (var subscriber in _subscribers.Keys.ToList())
            {
                if (_subscribers[subscriber] > 0)
                {
                    PublishNextPurchase(await game, subscriber);
                    _subscribers[subscriber]--;
                    if (_subscribers[subscriber] == 0)
                    {
                        _subscribers.TryRemove(subscriber, out _);
                    }
                }
            }
        }

        private async Task AddNewGame(string message)
        {
            Console.WriteLine(message);

            var parts = message.Split(new[] { "New Game Published: " }, StringSplitOptions.None)[1].Split('-');

            var game = new Game
            {
                Name = parts[0],
                Genre = parts[1],
                ReleaseDate = DateTime.Parse(parts[2]),
                Platform = parts[3],
                UnitsAvailable = int.Parse(parts[4]),
                Price = int.Parse(parts[5]),
                Valoration = int.Parse(parts[6]),
                Publisher = parts[7]
            };

            await _gameRepository.AddGame(game);
            Console.WriteLine("New game added: " + game.Name);
        }

        private void PublishNextPurchase(Game game, string subscriber)
        {
            var message = $"New Game Purchased: {game.Name} ({game.Genre})";
            var body = Encoding.UTF8.GetBytes(message);

            _publishChannel.BasicPublish(exchange: "",
                routingKey: "next_purchases",
                basicProperties: null,
                body: body);

            Console.WriteLine(" [x] New Game Purchased: {0} for subscriber {1}", message, subscriber);
        }

        private async Task ModifyGame(string message)
        {
            var parts = message.Split(new[] { "Modify-", "-" }, StringSplitOptions.None);
            if (parts.Length >= 3)
            {
                var field = parts[1].Trim();
                var newValue = parts[2].Trim();
                var gameName = parts[3].Trim();
                var game = await _gameRepository.GetGameByName(gameName);

                if (game != null)
                {
                    switch (field.ToLower())
                    {
                        case "name":
                            game.Name = newValue;
                            break;
                        case "genre":
                            game.Genre = newValue;
                            break;
                        case "releasedate":
                            if (DateTime.TryParse(newValue, out var newReleaseDate))
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
                        case "price":
                            if (int.TryParse(newValue, out var newPrice))
                            {
                                game.Price = newPrice;
                            }
                            else
                            {
                                throw new ArgumentException("Invalid number format.");
                            }
                            break;
                        case "publisher":
                            game.Publisher = newValue;
                            break;
                        case "units":
                            if (int.TryParse(newValue, out var newUnitsAvailable))
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

                    await _gameRepository.UpdateGame(game);
                    Console.WriteLine($"Game {gameName} modified: {field} updated to {newValue}");
                }
                else
                {
                    Console.WriteLine($"Game {gameName} not found.");
                }
            }
        }
    }
}