using Comunicacion.Dominio;
using ServerApp.DataAccess;


namespace ServerApp.Services;

public class GameService
{

    private readonly GameRepository _gameRepository;
    public GameService(GameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public void AddGame(Game game)
    {
       _gameRepository.AddGame(game);
    }

}