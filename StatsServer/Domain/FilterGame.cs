namespace StatsServer.Domain;

public class FilterGame
{
    public string? Platform { get; set; }
    public int? MinPrice { get; set; }
    public int? MaxPrice { get; set; }
    public int? MinValoration { get; set; }
    public int? MaxValoration { get; set; }

}