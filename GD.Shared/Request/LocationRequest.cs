namespace GD.Shared.Request;

public class LocationRequest
{
    public string Address { get; set; } = "";
    public required double PosLati { get; set; }
    public required double PosLong { get; set; }
}
