namespace GD.Shared.Request;

public class OrderItemRequest
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Amount { get; set; }
}
