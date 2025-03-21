namespace GD.Shared.Request;

public class ProductRequest
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required double Price { get; set; }
    public required string Tags { get; set; }
    public required int Amount { get; set; }
}
