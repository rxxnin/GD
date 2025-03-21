using GD.Api.Controllers.Base;
using GD.Api.DB;
using GD.Api.DB.Models;
using GD.Shared.Request;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GD.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientController : CustomController
{
    private readonly AppDbContext _appDbContext;

    public ClientController(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    [HttpPost("balance/add")]
    public async Task<IActionResult> AddToBalance(AddToBalanceRequest request)
    {
        var user = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Id == ContextUserId);
        if (user is null) return BadRequest("пользователь не найден");

        user.Balance += request.Amount;
        _appDbContext.Update(user);
        await _appDbContext.SaveChangesAsync();
        return Ok();
    }

    [Authorize(AuthenticationSchemes = "Bearer")]
    [HttpPost("location")]
    public async Task<IActionResult> SetLocation(LocationRequest request)
    {
        var user = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Id == ContextUserId);
        if (user is null) return BadRequest("пользователь не найден");

        user.Address = request.Address;
        user.PosLati = request.PosLati;
        user.PosLong = request.PosLong;

        _appDbContext.Update(user);
        await _appDbContext.SaveChangesAsync();
        return Ok(user);
    }

    [Authorize(AuthenticationSchemes = "Bearer")]
    [HttpGet("basket")]
    public async Task<IActionResult> GetBasket()
    {
        var orders = await _appDbContext.Orders.Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Where(o => o.ClientId == ContextUserId)
            .Select(order => new
            {
                order.Id,
                order.CreatedAt,
                order.StartDeliveryAt,
                order.OrderClosedAt,
                order.ToAddress,
                order.TotalPrice,
                order.Status,
                order.PayMethod,
                Products = order.OrderItems.Select(oi => new
                {
                    oi.Product.Id,
                    oi.Product.Name,
                    oi.Product.Description,
                    oi.Product.ImageValue,
                    oi.Product.Price,
                    oi.Product.Tags,
                    oi.Amount
                })
            })
            .ToListAsync();


        return Ok(orders);

        /*return Ok(_appDbContext.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(o => o.Product)
            .Where(o => o.Status == "Waiting")
            .Select(order => new
            {
                order.Id,
                order.CreatedAt,
                order.StartDeliveryAt,
                order.OrderClosedAt,
                order.ToAddress,
                order.TotalPrice,
                order.Status,
                order.PayMethod,
                Products = order.OrderItems.Select(oi => new
                {
                    oi.Product.Id,
                    oi.Product.Name,
                    oi.Product.Description,
                    oi.Product.ImageValue,
                    oi.Product.Price,
                    oi.Product.Tags,
                    oi.Amount
                })
            }));*/
    }
}