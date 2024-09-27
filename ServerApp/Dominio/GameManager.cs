namespace Comunicacion.Dominio
{
    public class GameManager
    {
        private List<Game> Games = new List<Game>();
        private static object _lock = new object();

        public GameManager()
        {
            Games.Add(new Game
            {
                Name = "Fornite",
                Genre = "Action",
                Publisher = "Dev 1",
                ReleaseDate = new DateTime(2020, 1, 1),
                UnitsAvailable = 10,
                Valoration = 8
            });
            Games.Add(new Game
            {
                Name = "Game 2",
                Genre = "Adventure",
                Publisher = "Dev 2",
                ReleaseDate = new DateTime(2021, 2, 2),
                UnitsAvailable = 10
            });
        }

        public List<Game> GetAllGames()
        {
            lock (_lock)
            {
                return Games;
            }
        }

        public Game GetGameByName(string name)
        {
            return Games.FirstOrDefault(g => g.Name == name);
        }

        public void AddGame(Game game)
        {
            lock (_lock)
            {
                Games.Add(game);
            }
        }

        public void RemoveGame(string name)
        {
            lock (_lock)
            {
                var game = GetGameByName(name);
                if (game != null)
                {
                    Games.Remove(game);
                }
            }
        }

        public void DiscountPurchasedGame(Game game)
        {
            Game gamePurchased = Games.FirstOrDefault(g => g.Name == game.Name);

            if (gamePurchased != null)
                gamePurchased.UnitsAvailable--;
        }

        public bool DoesGameExist(string name)
        {
            lock (_lock)
            {
                return Games.Any(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }

        public void AddValoration(string name, int valoration)
        {
            lock (_lock)
            {
                var game = GetGameByName(name);
                if (game != null)
                {
                    game.Valoration = (game.Valoration + valoration) / 2;
                }
            }
        }

        public List<Game> GetGamesByAttribute(string attributeName, string attributeValue)
        {
            lock (_lock)
            {
                return Games.Where(g =>
                {
                    var property = typeof(Game).GetProperty(attributeName);
                    if (property != null)
                    {
                        var value = property.GetValue(g)?.ToString();
                        return value != null && value.Contains(attributeValue, StringComparison.OrdinalIgnoreCase);
                    }

                    return false;
                }).ToList();
            }
        }
    }
}