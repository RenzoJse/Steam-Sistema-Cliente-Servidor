using Comunicacion.Dominio;
using ServerApp.Dominio;

namespace ServerApp.DataAccess;

public class UserRepository
{
    private List<User> _users;
    private static object _lock = new object();

    public UserRepository()
    {
        _users = [];
    }

    public bool RegisterUser(string username, string password)
    {
        lock (_lock)
        {
            if (_users.Any(u => u.Username == username))
            {
                return false; // Username already exists
            }

            _users.Add(new User { Username = username, Password = password, PurchasedGames = [] });
            return true;
        }
    }
}