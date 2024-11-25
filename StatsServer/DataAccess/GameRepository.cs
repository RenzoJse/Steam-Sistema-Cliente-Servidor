using Comunicacion.Dominio;
using ServerApp.Dominio;
using StatsServer.Domain;

namespace ServerApp.DataAccess;

public class GameRepository
{
    private static GameRepository _instance;
    private readonly List<Game> _games;
    private readonly List<Game> _purchasedGames;
    private static bool _reportGeneratedStatus;
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
        _purchasedGames = [];
    }

    public async Task AddGame(Game game)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));

        lock (_lock)
        {
            _games.Add(game);
        }
    }

    public Task<Game> GetGameByName(string name)
    {
        return Task.FromResult(_games.FirstOrDefault(g => g.Name == name)!);
    }


    public async Task RemoveGame(string name)
    {
        lock (_lock)
        {
            var game = GetGameByName(name).Result;
            if (game != null!) _games.Remove(game);
        }
    }

    public async Task DiscountPurchasedGame(Game game)
    {
        lock (_lock)
        {
            var gamePurchased = _games.FirstOrDefault(g => g.Name == game.Name);

            if (gamePurchased != null)
            {
                gamePurchased.UnitsAvailable--;
                _purchasedGames.Add(game);
            }
        }
    }

    public async Task UpdateGame(Game game)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));

        lock (_lock)
        {
            var existingGame = _games.FirstOrDefault(g => g.Name == game.Name);
            if (existingGame != null)
            {
                _games.Remove(existingGame);
                _games.Add(game);
            }
        }
    }

    public async Task AddValoration(string name, int valoration)
    {
        lock (_lock)
        {
            var game = GetGameByName(name);
            if (game != null!) game.Result.Valoration = (game.Result.Valoration + valoration) / 2;
        }
    }

    public async Task AddReview(string name, Review review)
    {
        lock (_lock)
        {
            var game = GetGameByName(name);
            game?.Result.Reviews.Add(review);
        }
    }

    public async Task<Game[]> GetFilteredGames(FilterGame filter)
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

    public async Task<Dictionary<string, int>> GetSalesReport()
    {
        try
        {
            if (_reportGeneratedStatus == false)
            {
                throw new InvalidOperationException("Sales report has not been generated yet.");
            }
            else
            {
                return await GenerateSellsReport(_purchasedGames);
            }
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine(ex.Message);
            return new Dictionary<string, int>();
        }
    }

    public static Task<bool> GetSalesReportStatus()
    {
        return Task.FromResult(_reportGeneratedStatus);
    }

    public async void PostSellsReport()
    {
        _reportGeneratedStatus = false;
        await GenerateSellsReport(_purchasedGames);
    }

    public static async Task<Dictionary<string, int>> GenerateSellsReport(List<Game>
        games)
    {
        Dictionary<string, int> sellsFromPublisher = new Dictionary<string, int>();
        foreach (var game in games)
        {
            await Task.Delay(5000);
            if (sellsFromPublisher.ContainsKey(game.Publisher))
            {
                sellsFromPublisher[game.Publisher] ++;
            }
            else
            {
                sellsFromPublisher[game.Publisher] = 1;
            }
        }
        _reportGeneratedStatus = true;
        return sellsFromPublisher;
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