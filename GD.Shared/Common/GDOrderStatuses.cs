namespace GD.Shared.Common;

public class GDOrderStatuses
{
    public const string Selecting = "Сборка";
    public const string Waiting = "Ожидание";
    public const string InDelivery = "Доставляется";
    public const string Delivered = "Доставлен";
}

/// <summary>
/// Наличка, Карта, Онлайн
/// </summary>
public class GDPayMethods
{
    public const string Online = "online";
    public const string BankCard = "по карте";
    public const string Cash = "наличкой";
}