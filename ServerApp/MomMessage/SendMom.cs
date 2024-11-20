using System.Text;
using RabbitMQ.Client;

namespace ServerApp.MomMessage;

public class SendMom
{
    public void SendMessageToMom(string sendMessage)
    {
        Console.WriteLine("Bienvenido al RabbitMQ Sender.....!!");

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

            channel.QueueDeclare("the_first",
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
                "the_first",
                null,
                body);
        }
    }
}