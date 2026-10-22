using System.Text.Json.Serialization;

namespace IceCreamShop.Desktop.Models;

public class Item
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Chocolate";

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Price { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int Quantity { get; set; }

    public string? Description { get; set; }

    public string PriceFormatted => $"${Price:F2}";
}
