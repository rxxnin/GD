using GD.Api.Controllers.Base;
using GD.Api.DB;
using GD.Api.DB.Models;
using GD.Api.Hubs;
using GD.Shared.Common;
using GD.Shared.Request;
using GD.Shared.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GD.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CourierController : CustomController
{
    private readonly UserManager<GDUser> _userManager;
	private readonly AppDbContext context;
    private readonly IHubContext<PosHub> _hubContext;

    public CourierController(UserManager<GDUser> userManager, AppDbContext context, IHubContext<PosHub> hubContext)
    {
        _userManager = userManager;
		this.context = context;
        _hubContext = hubContext;
	}

    [HttpPost("new")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "admin")]
    public async Task<IActionResult> MakeCourier([FromBody] NewCourierRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.ClientId.ToString());
        if (user is null) return BadRequest("пользователь не найден");

        var isClient = await _userManager.IsInRoleAsync(user, "Client");
        if (!isClient) return BadRequest("роль пользователя либо админ, либо курьер");
        
        await _userManager.RemoveFromRoleAsync(user, GDUserRoles.Client);
        await _userManager.AddToRoleAsync(user, GDUserRoles.Courier);
        
        return Ok();
    }


	[HttpPost("stat")]
	[Authorize(AuthenticationSchemes = "Bearer", Policy = "courier")]
	public async Task<IActionResult> WorkStat()
	{
		var c = await _userManager.FindByIdAsync(ContextUserId.ToString());
		if (c == null)
		{
			return BadRequest("User not found: " + ContextUserId);
		}

		// Получаем дату 7 дней назад
		DateTime startDate = DateTime.UtcNow.AddDays(-7);

		// Выполняем запрос к базе данных для получения заказов курьера за последние 7 дней
		var stats = await context.Orders
			.Where(order => order.CourierId == c.Id && order.OrderClosedAt >= startDate)
			.Where(o => o.Status == GDOrderStatuses.Delivered)
			.GroupBy(order => order.OrderClosedAt!.Value.Date)
			.Select(g => new GD.Shared.Response.CStatResponse
			{
				Date = g.Key,
				DeleveredAmount = g.Count(),
				TotalPrice = g.Sum(order => order.TotalPrice)
			})
			.ToListAsync();

		return Ok(stats);
	}

    [HttpPost("update-position")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "courier")]
    public async Task<IActionResult> UpdatePosition([FromBody] LocationRequest request)
    {
        var courierId = ContextUserId; // ID курьера из контекста авторизации
        var hubPosInfo = new HubPosInfo
        {
            UserId = courierId,
            TargetPosLati = request.PosLati,
            TargetPosLong = request.PosLong
        };

        // Вызов метода SendPos через IHubContext
        await _hubContext.Clients.All.SendAsync("SendPos", hubPosInfo);

        return Ok();
    }
}
