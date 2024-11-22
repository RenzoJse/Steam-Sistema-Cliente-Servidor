namespace StatsServer.DataAccess;

public class StatsData
{
    private static StatsData _instance;
    private int _totalUsers;
    private static object _lock = new object();

    public static StatsData? GetInstance()
    {
        lock (_lock)
        {
            if (_instance == null)
            {
                _instance = new StatsData();
            }
        }
        return _instance;
    }

    public StatsData()
    {
        _totalUsers = 0;
    }

    public void IncrementTotalUsers()
    {
        _totalUsers++;
    }

    public int GetTotalUsers()
    {
        return _totalUsers;
    }
}