namespace Comunicacion.Dominio;

public class Game
{
    public string Name { get; set; }
    public string Genre { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string Publisher { get; set; }
    public string Platform { get; set; }
    public int Price { get; set; }
    public string ImageName { get; set; }
    
    public int UnitsAvailable { get; set; }
    public int Valoration { get; set; }
    
    public override string ToString()
    {
        return $"Name: {Name}" +
               $"\nGenre: {Genre}" +
               $"\nDeveloper: {Publisher}" +
               $"\nRelease Date: {ReleaseDate.ToString()}" +
               $"\nPlatform: {Platform}" +
               $"\nValoration: {Valoration}" +
               $"\nUnitsAvailable: {UnitsAvailable}" +
               $"\nPrice: {Price}";
    }
}