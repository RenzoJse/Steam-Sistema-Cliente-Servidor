namespace Comunicacion.Dominio
{
    public class GameManager
    {
        private List<Game> Games = new List<Game>();
        
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
            return Games;
        }

        public Game GetGameByName(string name)
        {
            return Games.FirstOrDefault(g => g.Name == name);
        }

        public void AddGame(Game game)
        {
            Games.Add(game);
        }

        public void RemoveGame(string name)
        {
            var game = GetGameByName(name);
            if (game != null)
            {
                Games.Remove(game);
            }
        }
    }
}