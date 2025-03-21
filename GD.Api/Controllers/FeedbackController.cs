using GD.Api.Controllers.Base;
using GD.Api.DB;
using GD.Api.DB.Models;
using GD.Shared.Request;
using GD.Shared.Response;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GD.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedbackController : CustomController
{
    private readonly AppDbContext _appDbContext;

    public FeedbackController(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    [HttpPost("feedback")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "client")]
    public async Task<IActionResult> NewFeedback([FromBody] FeedbackRequest request)
    {
        if(!await _appDbContext.Products.AnyAsync(p => p.Id == request.ProductId))
            return BadRequest("товар не найден");
        
        if(!await _appDbContext.Users.AnyAsync(u => u.Id == ContextUserId))
            return BadRequest("пользователь не найден");
        
        var feedback = request.Adapt<Feedback>();
        feedback.ClientId = ContextUserId;
        feedback.CreatedAt = DateTime.UtcNow;
        
        await _appDbContext.Feedbacks.AddAsync(feedback);
        await _appDbContext.SaveChangesAsync();
        
        return Ok(feedback);
    }


    [HttpGet("feedback/{productId:guid}")]
    [AllowAnonymous] // Или используйте свою логику авторизации
    public async Task<IActionResult> GetFeedbacks(Guid productId)
    {
        var feedbacks = await _appDbContext.Feedbacks
            .Where(f => f.ProductId == productId)
            .Select(f => new FeedbackResponse { ClientId = f.ClientId, CreatedAt = f.CreatedAt,
                Id = f.Id, ProductId = f.ProductId, Stars = f.Stars, Text = f.Text})
            .ToListAsync();

        return Ok(feedbacks);
    }


    [HttpDelete("feedback/{id:guid}")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "admin")]
    public async Task<IActionResult> RemoveFeedback([FromRoute] Guid id)
    {
        var feedback = await _appDbContext.Feedbacks.FirstOrDefaultAsync(f => f.Id == id);
        if (feedback is null) return BadRequest("отзыв не найден");

        _appDbContext.Feedbacks.Remove(feedback);
        await _appDbContext.SaveChangesAsync();
        
        return Ok(id);
    }
}
