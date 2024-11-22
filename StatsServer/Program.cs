namespace StatsServer
{
    public class Program
    {
        private static global::StatsServer.StatsServer? _mq;
        public static void Main(string[] args)
        {
            _mq = new global::StatsServer.StatsServer();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}