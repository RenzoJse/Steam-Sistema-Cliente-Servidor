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

        public StatsServer(StatsData statsData, GameRepository gameRepository)
        {
            _statsData = statsData;
            _gameRepository = gameRepository;
            string rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
            var factory = new ConnectionFactory() { HostName =  rabbitMqHost }; // Defino la conexion

            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "the_first", // en el canal, definimos la Queue de la conexion
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            //Defino el mecanismo de consumo
            var consumer = new EventingBasicConsumer(channel);
            //Defino el evento que sera invocado cuando llegue un mensaje
            consumer.Received += (model, ea) =>
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
                    Console.WriteLine(message);

                    var parts = message.Split(["New Game Published: "], StringSplitOptions.None)[1].Split('-');

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

                    _gameRepository.AddGame(game);
                    Console.WriteLine("New game added: " + game.Name);
                }
            };

            //"PRENDO" el consumo de mensajes
            channel.BasicConsume(queue: "the_first",
                autoAck: true,
                consumer: consumer);
        }
    }
}