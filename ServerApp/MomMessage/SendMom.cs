using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ServerApp.MomMessage;

public class SendMom
{
    public static void SendMessageToMom(string sendMessage)
    {
        Console.WriteLine("Sending message to MOM");

        // 1 - Definimos un FACTORY para inicializar la conexion
        // Esto es exclusivo de Rabbit, le indicamos donde esta el servidor de Rabbit

        var factory = new ConnectionFactory { HostName = "localhost" };

        // 2 - Creamos la connection y la conectamos al hostname indicado (en este caso es localhost)
        using (var connection = factory.CreateConnection())

            // 3 - Creamos el canal haciendo un createmodel a la conexion.
            // Esto seria el equivalente a hacer un connect en el mundo Socket.
        using (var channel = connection.CreateModel())

        {
            // 4 - Declaramos la cola de mensajes
            // Utilizamos ese canal para declarar la cola

            // Indicamos el nombre de la cola que vamos a utilizar, en este caso "the_first"
            // QueueDeclare lo que hace es crear la cola si no existe o vincularme si ya esta creada

            channel.QueueDeclare("steam_logs",
                false,
                false,
                false,
                null);

            var message = sendMessage;
            // Codifico el message a lo que sea y mando lo que sea.
            var body = Encoding.UTF8.GetBytes(message);

            // Exchange vacio, va a usar el exchange por defecto
            // Por el routingKey sabe a que cola enviar el mensaje
            // Las prperties esta en null, no le ponemos properties
            // El body es el cuerpo del mansaje que enviamos

            channel.BasicPublish("",
                "steam_logs",
                null,
                body);
        }
    }

    public void SuscribeToMom(int n)
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "next_purchases",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine(" [x] Received {0}", message);
            n--;
            if (n <= 0)
            {
                Console.WriteLine("Finished sending messages. Reached the limit of purchases to view.");
            }
        };

        channel.BasicConsume(queue: "next_purchases",
            autoAck: true,
            consumer: consumer);

        Console.WriteLine("Subscribed to next purchases queue. Waiting for messages...");
        Console.ReadLine(); // Keep the application running
    }
}