using System.Collections.Generic;
using System.Linq;

namespace Comunicacion.Dominio
{
    public class UserManager
    {
        private List<User> users = new List<User>();
        
        public UserManager()
        {
            users.Add(new User { Username = "admin", Password = "admin", PurchasedGames = new List<Game>() });
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