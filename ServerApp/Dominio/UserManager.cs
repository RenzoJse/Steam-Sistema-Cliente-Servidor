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
                Name = "Fortnite",
                Genre = "Action",
                Publisher = "Dev 1",
                ReleaseDate = new DateTime(2020, 1, 1),
                UnitsAvailable = 10,
                Valoration = 8
            });
            users.Add(admin);

            var user1 = new User
            {
                Username = "alexgamer",
                Password = "password1",
                PublishedGames = new List<Game>(),
                PurchasedGames = new List<Game>()
            };
            users.Add(user1);

            var user2 = new User
            {
                Username = "lunaplay",
                Password = "pass1234",
                PublishedGames = new List<Game>(),
                PurchasedGames = new List<Game>()
            };
            users.Add(user2);

            var user3 = new User
            {
                Username = "johndoe",
                Password = "john2024",
                PublishedGames = new List<Game>(),
                PurchasedGames = new List<Game>()
            };
            users.Add(user3);

            var user4 = new User
            {
                Username = "elitewarrior",
                Password = "elitepass",
                PublishedGames = new List<Game>(),
                PurchasedGames = new List<Game>()
            };
            users.Add(user4);

            var user5 = new User
            {
                Username = "noobking",
                Password = "noobking",
                PublishedGames = new List<Game>(),
                PurchasedGames = new List<Game>()
            };
            users.Add(user5);

            var user6 = new User
            {
                Username = "speedsterjack",
                Password = "fastgame",
                PublishedGames = new List<Game>(),
                PurchasedGames = new List<Game>()
            };
            users.Add(user6);
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