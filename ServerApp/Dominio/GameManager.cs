namespace Comunicacion.Dominio
{
    public class GameManager
    {
        private static List<Game> Games = new List<Game>();
        private static object _lock = new object();

        public GameManager()
        {
            PreLoadedGames();
        }

        public Game CreateNewGame(string name, string genre, DateTime releaseDate, string platform, int unitsAvailable,
            int price, int valoration, User owner)
        {
            lock(_lock)
            {
                Game newGame = new Game
                {
                    Name = name,
                    Genre = genre,
                    ReleaseDate = releaseDate,
                    Platform = platform,
                    Publisher = owner.Username,
                    UnitsAvailable = unitsAvailable,
                    Price = price,
                    Valoration = valoration,
                    Reviews = new List<Review>(),
                    ImageName = name
                };
                Games.Add(newGame);
                return newGame;
            }
        }
        
        public List<Game> GetAllGames()
        {
            lock (_lock)
            {
                return Games;
            }
        }

        public static Game GetGameByName(string name)
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
            lock (_lock)
            {
                Game gamePurchased = Games.FirstOrDefault(g => g.Name == game.Name);

                if (gamePurchased != null) gamePurchased.UnitsAvailable--;
            }
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
        
        public void AddReview(string name, Review review)
        {
            lock (_lock)
            {
                var game = GetGameByName(name);
                if (game != null)
                {
                    game.Reviews.Add(review);
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
                    })
                    .ToList();
            }
        }

        private void PreLoadedGames()
        {
            Game fortnite = new Game
            {
                Name = "Fornite",
                Genre = "Action",
                Publisher = "Dev 1",
                ReleaseDate = new DateTime(2020, 1, 1),
                UnitsAvailable = 10,
                Valoration = 8,
                Price = 10,
                Platform = "PC"
            };
            Games.Add(fortnite);

            Game roblox = new Game
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
            Games.Add(roblox);

            Game minecraft = new Game
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
            Games.Add(minecraft);

            Game pokemonSword = new Game
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
            Games.Add(pokemonSword);

            Game noMorePokemon = new Game
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
            Games.Add(noMorePokemon);

            Game leagueOfLegends = new Game
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
            Games.Add(leagueOfLegends);
        }
    }
}