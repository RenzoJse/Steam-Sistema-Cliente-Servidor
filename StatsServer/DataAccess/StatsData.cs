namespace StatsServer.DataAccess;

public class StatsData
{
    private static StatsData _instance;
    private int _totalUsers;
    private static readonly object _lock = new();

    public static StatsData GetInstance()
    {
        lock (_lock)
        {
            if (_instance == null) _instance = new StatsData();
        }

        return _instance;
    }

    public StatsData()
    {
        _totalUsers = 0;
    }

    public void IncrementTotalUsers()
    {
        lock (_lock)
        {
            _totalUsers++;
        }
    }

    public int GetTotalUsers()
    {
        lock (_lock)
        {
            return _totalUsers;
        }
    }
}