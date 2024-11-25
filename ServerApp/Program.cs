using System.Net;
using System.Net.Sockets;
using System.Text;
using Communication;
using Comunicacion;
using Comunicacion.Dominio;
using ServerApp.TCP;

namespace ServerApp;

public class Program
{
    private static readonly SettingsManager SettingsMngr = new();

    public static Task StartTcpServer()
    {
        Console.WriteLine("Starting Server Application...");

        var ipAddress = SettingsMngr.ReadSettings(ServerConfig.ServerIpConfigKey);
        var port = int.Parse(SettingsMngr.ReadSettings(ServerConfig.ServerPortConfigKey));
        var tcpServer = new TcpServer(ipAddress, port);

        tcpServer.Start();

        Console.WriteLine("Type 'shutdown' to close the server");
        while (true)
        {
            var command = Console.ReadLine();
            if (command?.ToLower() == "shutdown")
            {
                Console.WriteLine("Shutting down server...");
                tcpServer.Stop();
                break;
            }
        }

        return Task.CompletedTask;
    }
}