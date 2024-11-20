using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace StatsServer
{
    public class StatsServer
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public StatsServer()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: "stats_queue",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);
        }

        public void StartListening()
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine(" [x] Received {0}", message);
                // Aquí puedes procesar el mensaje y actualizar las estadísticas
            };
            _channel.BasicConsume(queue: "stats_queue",
                autoAck: true,
                consumer: consumer);
        }

        public void Stop()
        {
            _channel.Close();
            _connection.Close();
        }
    }
}