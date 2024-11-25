
    using ServerApp.TCP;

    namespace AdmingRPC
    {
        public class Program
        {
            public static void Main(string[] args)
            {
                var builder = WebApplication.CreateBuilder(args);

                builder.Services.AddSingleton<TcpServer>(provider =>
                {
                    var ipAddress = "127.0.0.1"; // Cambia esto por la IP que deseas utilizar
                    var port = 20000;            // Cambia esto por el puerto que deseas utilizar
                    return new TcpServer(ipAddress, port);
                });

                builder.Services.AddTransient<GameManagementService>();
                builder.Services.AddGrpc();

                var app = builder.Build();

                // Configure the HTTP request pipeline.
                app.MapGrpcService<GameManagementService>();
                app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                var tareaServidorViejo = Task.Run( async ()=> await ServerApp.Program.StartTcpServer());
                app.Run();
            }
        }
    }