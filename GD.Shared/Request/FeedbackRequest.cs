namespace GD.Shared.Request;

public class FeedbackRequest
{
    public required Guid ProductId { get; set; }
    public required int Stars { get; set; }
    public required string Text { get; set; }
}
