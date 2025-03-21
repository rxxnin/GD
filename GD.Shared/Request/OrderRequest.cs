namespace GD.Shared.Request;

public class OrderRequest
{
    public Guid OrderId { get; set; }
    public double TargetPosLati { get; set; }
    public double TargetPosLong { get; set; }
    public string ToAddress { get; set; }
    /// <summary>
    /// Наличка, Карта, Онлайн
    /// </summary>
    public string PayMethod { get; set; }
}

public class OrderRequestWithDefault
{
    public Guid OrderId { get; set; }
    /// <summary>
    /// Наличка, Карта, Онлайн
    /// </summary>
    public string PayMethod { get; set; }
    public string ToAddress { get; set; }
}
