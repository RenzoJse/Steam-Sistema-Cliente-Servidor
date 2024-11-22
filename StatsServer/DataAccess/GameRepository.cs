using Comunicacion.Dominio;
using StatsServer.Domain;

namespace ServerApp.DataAccess;

public class GameRepository
{
    private static GameRepository _instance;
    private List<Game> _games;
    private static object _lock = new object();

    public static GameRepository GetInstance()
    {
        lock (_lock)
        {
            if (_instance == null) _instance = new GameRepository();
        }

        return _instance;
    }

    public GameRepository()
    {
        _games = [];
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

    public Game[] GetFilteredGames(FilterGame filter)
    {
        lock (_lock)
        {
            var query = _games.AsQueryable();

            if (!string.IsNullOrEmpty(filter.Platform))
            {
                query = query.Where(o => o.Platform == filter.Platform);
            }

            if (filter.MinPrice.HasValue)
            {
                query = query.Where(o => o.Price >= filter.MinPrice.Value);
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(o => o.Price <= filter.MaxPrice.Value);
            }

            if (filter.MinValoration.HasValue)
            {
               query = query.Where(o => o.Valoration >= filter.MinValoration.Value);
            }

            if (filter.MaxValoration.HasValue)
            {
                query = query.Where(o => o.Valoration <= filter.MaxValoration.Value);
            }

            return query.ToArray();
        }

    }

}