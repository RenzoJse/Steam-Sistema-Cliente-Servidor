namespace Comunicacion.Dominio;

public class User
{
    public string Username { get; set; }
    public string Password { get; set; }
    public List<Game> PurchasedGames { get; set; }
    public List<Game> PublishedGames { get; set; }
    public bool ValidatePassword(string password)
    {
        return Password == password;
    }
}