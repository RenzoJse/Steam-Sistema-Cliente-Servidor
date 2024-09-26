using System.Collections.Generic;
using System.Linq;

namespace Comunicacion.Dominio
{
    public class UserManager
    {
        private List<User> users = new List<User>();
        private static object _lock = new object();

        public UserManager()
        {
            var admin = new User
            {
                Username = "admin",
                Password = "admin",
                PublishedGames = new List<Game>(),
                PurchasedGames = new List<Game>()
            };
            admin.PurchasedGames.Add(new Game
            {
                Name = "Fornite",
                Genre = "Action",
                Publisher = "Dev 1",
                ReleaseDate = new DateTime(2020, 1, 1),
                UnitsAvailable = 10,
                Valoration = 8
            });
            users.Add(admin);
        }

        public bool RegisterUser(string username, string password)
        {
            lock (_lock)
            {
                if (users.Any(u => u.Username == username))
                {
                    return false; // Username already exists
                }
                else
                {
                    users.Add(new User { Username = username, Password = password, PurchasedGames = new List<Game>(), PublishedGames = new List<Game>()});
                    return true;
                }
            }
        }

        public User AuthenticateUser(string username, string password)
        {
            lock (_lock)
            {
                var user = users.FirstOrDefault(u => u.Username == username);
                if (user != null && user.ValidatePassword(password))
                {
                    return user;
                }

                return null;
            }
        }

        public bool PurchaseGame(Game game, User user)
        {
            lock (_lock)
            {
                if (game is null || user is null)
                {
                    return false;
                }

                User activeUser = users.FirstOrDefault(u => u.Username == user.Username);

                if (activeUser is null) //si el usuario no existe
                {
                    return false;
                }
                else if (activeUser.PurchasedGames.Any(g => g.Name == game.Name)) //si el juego ya lo tiene comprado 
                {
                    return false;
                }

                activeUser.PurchasedGames.Add(game);
                return true;
            }
        }
    }
}