using Comunicacion;
using Grpc.Core;
using ServerApp.Dominio;
using ServerApp.TCP;
using System.Net.Sockets;
using System.Text;

namespace AdmingRPC.Services;

public class GameService : Game.GameBase
{
    private readonly ILogger<GameService> _logger;
    private readonly TcpServer _tcpServer;

    public GameService(ILogger<GameService> logger, TcpServer tcpServer)
    {
        _logger = logger;
        _tcpServer = tcpServer; // Inyección de TcpServer para usar la lógica de PublishGamegRCP
    }

    public override async Task<GameResponse> AddGame(GameRequest request, ServerCallContext context)
    {
        try
        {
            
            var connectedUser = new User { Username = "Admin" };

            
            var dummyClient = new TcpClient();
            var networkDataHelper = new NetworkDataHelper(dummyClient);

           
            await networkDataHelper.Send(Encoding.UTF8.GetBytes(request.Name));
            await networkDataHelper.Send(Encoding.UTF8.GetBytes(request.Genre));
            await networkDataHelper.Send(Encoding.UTF8.GetBytes(request.ReleaseDate));
            await networkDataHelper.Send(Encoding.UTF8.GetBytes(request.Platform));
            await networkDataHelper.Send(Encoding.UTF8.GetBytes(request.UnitsAvailable.ToString()));
            await networkDataHelper.Send(Encoding.UTF8.GetBytes(request.Price.ToString()));

            
            await _tcpServer.PublishGamegRCP(networkDataHelper, connectedUser, dummyClient);

            return new GameResponse
            {
                Message = $"Game '{request.Name}' Published Successfully via gRPC."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing game via gRPC.");
            return new GameResponse
            {
                Message = $"Error: Unable to publish the game due to {ex.Message}."
            };
        }
    }
}
