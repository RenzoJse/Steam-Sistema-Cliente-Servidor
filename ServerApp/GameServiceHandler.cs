using ServerApp.Dominio;

public class GameServiceHandler
{
    public string PublishGame(string gameName, string genre, DateTime releaseDate, string platform, int unitsAvailable, int price, User publisher)
    {
        if (GameManager.DoesGameExist(gameName)) // Llama directamente al método estático
        {
            return "Error: That game already exists.";
        }

        var newGame = GameManager.CreateNewGame(gameName, genre, releaseDate, platform, unitsAvailable, price, 0, publisher);
        UserManager.PublishGame(newGame, publisher);

        return $"Game '{gameName}' published successfully.";
    }
}
