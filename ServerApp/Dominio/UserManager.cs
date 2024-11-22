using Comunicacion.Dominio;

namespace ServerApp.Dominio
{
    public class UserManager
    {
        private static List<User> _users = [];
        private static object _lock = new object();

        public UserManager()
        {
            PreLoadedUsers();
        }

        public static bool RegisterUser(string username, string password)
        {
            lock (_lock)
            {
                if (_users.Any(u => u.Username == username))
                {
                    return false; // Username already exists
                }
                else
                {
                    _users.Add(new User { Username = username, Password = password, PurchasedGames = new List<Game>(), PublishedGames = new List<Game>()});
                    return true;
                }
            }
        }

        public static User GetUserByUsername(string username)
        {
            lock (_lock)
            {
                return _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            }
        }


        public static User AuthenticateUser(string username, string password)
        {
            lock (_lock)
            {
                var user = _users.FirstOrDefault(u => u.Username == username);
                if (user != null && user.ValidatePassword(password))
                {
                    return user;
                }

                return null;
            }
        }

        public static bool PurchaseGame(Game game, User user)
        {
            lock (_lock)
            {
                if (game is null || user is null)
                {
                    return false;
                }

                User activeUser = _users.FirstOrDefault(u => u.Username == user.Username);

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

        public static void PublishGame(Game game, User user)
        {
            lock (_lock)
            {
                if (game is null || user is null)
                {
                    return;
                }

                User activeUser = _users.FirstOrDefault(u => u.Username == user.Username);

                if (activeUser is null)
                {
                    return;
                }

                activeUser.PublishedGames.Add(game);
            }
        }

        private void PreLoadedUsers()
        {
            var admin = new User
            {
                Username = "admin",
                Password = "admin",
                PublishedGames = [],
                PurchasedGames = []
            };
            _users.Add(admin);

            var user1 = new User
            {
                Username = "nicolasduarte",
                Password = "password",
                PublishedGames = new List<Game>(),
                PurchasedGames = new List<Game>()
            };
            _users.Add(user1);

            var user2 = new User
            {
                Username = "renzojose",
                Password = "password",
                PublishedGames = new List<Game>(),
                PurchasedGames = new List<Game>()
            };
            _users.Add(user2);

            var user3 = new User
            {
                Username = "johndoe",
                Password = "john2024",
                PublishedGames = new List<Game>(),
                PurchasedGames = new List<Game>()
            };
            _users.Add(user3);

            var user4 = new User
            {
                Username = "elitewarrior",
                Password = "elitepass",
                PublishedGames = new List<Game>(),
                PurchasedGames = new List<Game>()
            };
            _users.Add(user4);

            var user5 = new User
            {
                Username = "noobking",
                Password = "noobking",
                PublishedGames = new List<Game>(),
                PurchasedGames = new List<Game>()
            };
            _users.Add(user5);

            var user6 = new User
            {
                Username = "speedsterjack",
                Password = "fastgame",
                PublishedGames = new List<Game>(),
                PurchasedGames = new List<Game>()
            };
            _users.Add(user6);
        }
    }
}