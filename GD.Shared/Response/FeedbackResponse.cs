namespace GD.Shared.Response;

public class FeedbackResponse
{
    public required Guid Id { get; set; }
    public required Guid ProductId { get; set; }
    public required Guid ClientId { get; set; }
    public required int Stars { get; set; }
    public required string Text { get; set; }
    public required DateTime CreatedAt { get; set; }
}
