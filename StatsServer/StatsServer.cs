using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StatsServer.DataAccess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StatsServer
{
    public class StatsServer : IHostedService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly StatsData _statsData;

        public StatsServer ()
        {
            _statsData = new StatsData();
            var factory = new ConnectionFactory() { HostName = "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            RecieveMomMessage();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _channel.Close();
            _connection.Close();
            return Task.CompletedTask;
        }

        private void RecieveMomMessage()
        {
            _channel.QueueDeclare(queue: "the_first",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (sender, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine("Message received => {0}", message);

                if (message.Contains("New User"))
                {
                    _statsData.IncrementTotalUsers();
                    Console.WriteLine("Total users incremented. Current total: {0}", _statsData.GetTotalUsers());
                }
            };

            _channel.BasicConsume(queue: "the_first",
                autoAck: true,
                consumer: consumer);

            Console.WriteLine("Press [enter] to exit.");
            Console.ReadLine();
        }
    }
}