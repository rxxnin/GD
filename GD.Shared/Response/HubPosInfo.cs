using GD.Shared.Common;

namespace GD.Shared.Response;

public class HubPosInfo
{
	public Guid UserId { get; set; }
	public double TargetPosLati { get; set; }
	public double TargetPosLong { get; set; }
}

public class DailyOrderStatisticsDTO
{
    public DateTime Date { get; set; }
    public int TotalOrders { get; set; }
    public int SuccessfulOrders { get; set; }
    public int FailedOrders { get; set; }
    public int SelectingOrders { get; set; }
    public int WaitingOrders { get; set; }
    public int InDeliveryOrders { get; set; }
    public int DeliveredOrders { get; set; }
}

public class OrderStatisticsResponseDTO
{
    public List<DailyOrderStatisticsDTO> DailyStatistics { get; set; }
}

public class RevenueStatisticsDTO
{
    public double Revenue { get; set; }
}

public class OrderHistoryItemDTO
{
    public DateTime Date { get; set; }
    public Guid OrderId { get; set; }
    public string CustomerName { get; set; }
    public string DeliveryAddress { get; set; }
    public string Status { get; set; }
    public string DelivererName { get; set; }
    public double TotalAmount { get; set; }
    public List<OrderItemDTO> OrderItems { get; set; }
    public bool IsItemsVisible { get; set; }
}

public class OrderItemDTO
{
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public double Price { get; set; }
}

