using StatsServer.DataAccess;

namespace StatsServer
{
    public class Program
    {
        private static global::StatsServer.StatsServer? _mq;
        public static StatsData _statsData;
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            // Resolver e inicializar StatsServer
            using (var scope = host.Services.CreateScope())
            {
                var statsServer = scope.ServiceProvider.GetRequiredService<StatsServer>();
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}