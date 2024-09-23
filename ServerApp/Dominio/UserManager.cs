using System.Collections.Generic;
using System.Linq;

namespace Comunicacion.Dominio
{
    public class UserManager
    {
        private List<User> users = new List<User>();

        
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
            if (users.Any(u => u.Username == username))
            {
                return false; // Username already exists
            }else
            {
                users.Add(new User { Username = username, Password = password, PurchasedGames = new List<Game>() });
                return true;
            }
        }

        public User AuthenticateUser(string username, string password)
        {
            var user = users.FirstOrDefault(u => u.Username == username);
            if (user != null && user.ValidatePassword(password))
            {
                return user;
            }

            return null;
        }
        
    }
}