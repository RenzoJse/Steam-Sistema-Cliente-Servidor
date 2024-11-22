using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StatsServer.DataAccess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StatsServer
{
    public class StatsServer
    {
        public StatsServer()
        {
            string rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
            var factory = new ConnectionFactory() { HostName =  rabbitMqHost }; // Defino la conexion
            var statsData = new StatsData();

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
                if (message.Contains("New User"))
                {
                    statsData.IncrementTotalUsers();
                    Console.WriteLine("Total users incremented. Current total: {0}", statsData.GetTotalUsers());
                }
            };

            //"PRENDO" el consumo de mensajes
            channel.BasicConsume(queue: "the_first",
                autoAck: true,
                consumer: consumer);
        }
    }
}