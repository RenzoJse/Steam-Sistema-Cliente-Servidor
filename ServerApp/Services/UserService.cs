using ServerApp.DataAccess;

namespace ServerApp.Services;

public class UserService
{
    private readonly UserRepository _userRepository;
    
    public UserService(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    public bool RegisterUser(string username, string password)
    {
        return _userRepository.RegisterUser(username, password);
    }
}