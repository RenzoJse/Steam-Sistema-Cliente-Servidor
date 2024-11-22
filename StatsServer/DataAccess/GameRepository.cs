using Comunicacion.Dominio;
using ServerApp.Dominio;
using StatsServer.Domain;

namespace ServerApp.DataAccess;

public class GameRepository
{
    private static GameRepository _instance;
    private readonly List<Game> _games;
    private static readonly object _lock = new();

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
        _games = PreLoadedGames();
    }

    public void AddGame(Game game)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));

        lock (_lock)
        {
            _games.Add(game);
        }
    }

    public Game GetGameByName(string name)
    {
        return _games.FirstOrDefault(g => g.Name == name)!;
    }


    public void RemoveGame(string name)
    {
        lock (_lock)
        {
            var game = GetGameByName(name);
            if (game != null!) _games.Remove(game);
        }
    }

    public void DiscountPurchasedGame(Game game)
    {
        lock (_lock)
        {
            var gamePurchased = _games.FirstOrDefault(g => g.Name == game.Name);

            if (gamePurchased != null) gamePurchased.UnitsAvailable--;
        }
    }

    public bool DoesGameExist(string name)
    {
        lock (_lock)
        {
            return _games.Any(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void AddValoration(string name, int valoration)
    {
        lock (_lock)
        {
            var game = GetGameByName(name);
            if (game != null!) game.Valoration = (game.Valoration + valoration) / 2;
        }
    }

    public void AddReview(string name, Review review)
    {
        lock (_lock)
        {
            var game = GetGameByName(name);
            game?.Reviews.Add(review);
        }
    }

    public Game[] GetFilteredGames(FilterGame filter)
    {
        lock (_lock)
        {
            var query = _games.AsQueryable();

            if (!string.IsNullOrEmpty(filter.Platform)) query = query.Where(o => o.Platform == filter.Platform);

            if (filter.MinPrice.HasValue) query = query.Where(o => o.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue) query = query.Where(o => o.Price <= filter.MaxPrice.Value);

            if (filter.MinValoration.HasValue) query = query.Where(o => o.Valoration >= filter.MinValoration.Value);

            if (filter.MaxValoration.HasValue) query = query.Where(o => o.Valoration <= filter.MaxValoration.Value);

            return query.ToArray();
        }
    }

    private List<Game> PreLoadedGames()
    {
        var games = new List<Game>();

        var fortnite = new Game
        {
            Name = "Fornite",
            Genre = "Action",
            Publisher = "admin",
            ReleaseDate = new DateTime(2020, 1, 1),
            UnitsAvailable = 10,
            Valoration = 8,
            Price = 10,
            Platform = "PC"
        };
        games.Add(fortnite);

        var roblox = new Game
        {
            Name = "Roblox",
            Genre = "Adventure",
            Publisher = "Roblox Corporation",
            ReleaseDate = new DateTime(2009, 2, 2),
            UnitsAvailable = 150000,
            Valoration = 6,
            Price = 5,
            Platform = "PC"
        };
        games.Add(roblox);

        var minecraft = new Game
        {
            Name = "Minecraft",
            Genre = "Adventure",
            Publisher = "Mojang",
            ReleaseDate = new DateTime(2010, 3, 3),
            UnitsAvailable = 150,
            Valoration = 10,
            Price = 20,
            Platform = "PC"
        };
        games.Add(minecraft);

        var pokemonSword = new Game
        {
            Name = "Pokemon Sword",
            Genre = "Adventure",
            Publisher = "Nintendo",
            ReleaseDate = new DateTime(1996, 4, 4),
            UnitsAvailable = 1,
            Valoration = 9,
            Price = 150,
            Platform = "Nintendo Switch"
        };
        games.Add(pokemonSword);

        var noMorePokemon = new Game
        {
            Name = "NoMorePokemon",
            Genre = "MMORPG",
            Publisher = "Nintendo",
            ReleaseDate = new DateTime(1548, 4, 4),
            UnitsAvailable = 0,
            Valoration = 2,
            Price = 9,
            Platform = "IOS"
        };
        games.Add(noMorePokemon);

        var leagueOfLegends = new Game
        {
            Name = "League of Legends",
            Genre = "MOBA",
            Publisher = "Riot Games",
            ReleaseDate = new DateTime(2009, 4, 4),
            UnitsAvailable = 1000000,
            Valoration = 7,
            Price = 0,
            Platform = "PC"
        };
        games.Add(leagueOfLegends);

        return games;
    }
}