using Comunicacion.Dominio;

namespace ServerApp.DataAccess;

public class GameRepository
{
    private List<Game> _games;
    private static object _lock = new object();
    
    public GameRepository()
    {
        _games = new List<Game>();
    }
    
    public void AddGame(Game game)
    {
        if(game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }
        
        lock(_lock)
        {
            _games.Add(game);
        }
        
    }
    
}