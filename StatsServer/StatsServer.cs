using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace StatsServer
{
    public class StatsServer
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        class Program
        {
            static void Main(string[] args)
            {
                var statsServer = new StatsServer();
                statsServer.RecieveMomMessage();
            }
        }

        private void RecieveMomMessage()
        {
            // 1 - Defino la conexion
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // En el canal defino la cola
                channel.QueueDeclare(queue: "the_first",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                // Tengo que definir un consumer
                // Defino el mecanismo de consumo
                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += (sender, eventArgs) =>
                {
                    var body = eventArgs.Body.ToArray();

                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine(" Message received => {0}", message);

                };

                // "PRENDO" el consumo de mensajes
                // El ack es la confirmacion para que la cola lo borre
                channel.BasicConsume(queue: "the_first",
                    autoAck: true,
                    consumer: consumer);

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }
        }
    }
}