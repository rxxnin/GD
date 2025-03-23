using GD.Api.Controllers.Base;
using GD.Api.DB;
using GD.Api.DB.Models;
using GD.Api.Hubs;
using GD.Shared.Common;
using GD.Shared.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GD.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : CustomController
{
    private readonly AppDbContext _appDbContext;
    private readonly UserManager<GDUser> _userManager;

    public OrderController(AppDbContext appDbContext, UserManager<GDUser> userManager)
    {
        _appDbContext = appDbContext;
        _userManager = userManager;
    }

    [HttpPost("open")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "client")]
    public async Task<IActionResult> OpenOrder()
    {
        var order = new Order
        {
            CreatedAt = DateTime.Now,
            Status = GDOrderStatuses.Selecting,
            ClientId = ContextUserId
        };
        
        await _appDbContext.Orders.AddAsync(order);
        await _appDbContext.SaveChangesAsync();
        return Ok(order);
    }

    [HttpGet("{id:guid}")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "client")]
    public async Task<IActionResult> GetOrder([FromRoute] Guid id)
    {
        var order = await _appDbContext.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(o => o.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
        
        if (order is null) return BadRequest("заказ не найден");
        if (order.ClientId != ContextUserId) return BadRequest("заказ не ваш");

        var result = new
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
                oi.Product.ImageValue,
                oi.Product.Description,
                oi.Product.Price,
                oi.Product.Tags,
                oi.Amount
            })
        };

        return Ok(result);    
    }

    [HttpGet("waiting")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "courier")]
    public IActionResult GetWaitingOrders()
    {
        var res = _appDbContext.Orders
            .Where(o => o.Status != GDOrderStatuses.Delivered)
            .Include(o => o.OrderItems)
            .ThenInclude(o => o.Product)
            .ToList();
        
        return Ok(res);
    }

	[HttpGet("waiting-f")]
	[Authorize(AuthenticationSchemes = "Bearer", Policy = "courier")]
	public IActionResult GetLiteInfo()
	{
		var res = _appDbContext.Orders
            .Where(o => o.Status != GDOrderStatuses.Delivered)
			.Include(o => o.OrderItems) // Сначала загружаем связанные элементы заказов
				.ThenInclude(o => o.Product) // Загружаем продукты
			.Select(order => new Order
			{
				Id = order.Id,
				CreatedAt = order.CreatedAt,
				ClientId = order.ClientId,
                Status = order.Status,
                StartDeliveryAt = order.StartDeliveryAt,
                OrderClosedAt = order.OrderClosedAt,
                PayMethod = order.PayMethod,
                ToAddress = order.ToAddress,
                TargetPosLati = order.TargetPosLati,
                TargetPosLong = order.TargetPosLong,
                TotalPrice = order.TotalPrice,
                CourierId = order.CourierId,
				OrderItems = order.OrderItems.Select(item => new OrderItem
				{
					Id = item.Id,
				    Amount = item.Amount,
					ProductId = item.ProductId,
					Product = new Product { Id = item.Product.Id, Amount = item.Product.Amount, Feedbacks = item.Product.Feedbacks,
                    Description = item.Product.Description, Name = item.Product.Name, Price = item.Product.Price, Tags = item.Product.Tags}, // например, название продукта
                    OrderId = item.OrderId,
				}).ToList()
			})
			.ToList();

		return Ok(res);
	}


	[HttpPost("add")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "client")]
    public async Task<IActionResult> AddToOrder([FromBody] OrderItemRequest request)
    {
        if (!await _appDbContext.Products.AnyAsync(p => p.Id == request.ProductId))
        {
            return BadRequest("товар не найден");
        }
        
        if (!await _appDbContext.Orders.AnyAsync(p => p.Id == request.OrderId))
        {
            return BadRequest("заказ не найден");
        }

        var orderItem = new OrderItem
        {
            Amount = request.Amount,
            OrderId = request.OrderId,
            ProductId = request.ProductId
        };
        
        await _appDbContext.OrderItems.AddAsync(orderItem);
        await _appDbContext.SaveChangesAsync();

        return Ok();
    }
    
    [HttpPost("complete")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "client")]
    public async Task<IActionResult> CompleteOrder([FromBody] OrderRequest orderRequest)
    {
        var order = await _appDbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderRequest.OrderId);

        if (order is null)
        {
            return BadRequest("заказ не найден");
        }

        if (order.Status != GDOrderStatuses.Selecting)
        {
            return BadRequest("заказ уже обрабатывается");
        }
        
        var orderItems = _appDbContext.OrderItems.Include(o => o.Product).Where(o => o.OrderId == orderRequest.OrderId).ToList();
        var total = orderItems.Sum(o => o.Amount * o.Product.Price);
        
        if (orderRequest.PayMethod.ToLower() == "online")
        {
            var user = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Id == ContextUserId);
            if (user!.Balance < total) return BadRequest("недостаточно средств");
        }

        order.ToAddress = orderRequest.ToAddress;
        order.TargetPosLong = orderRequest.TargetPosLong;
        order.TargetPosLati = orderRequest.TargetPosLati;
        order.CreatedAt = DateTime.UtcNow;
        order.Status = GDOrderStatuses.Waiting;
        order.TotalPrice = total;
        order.PayMethod = orderRequest.PayMethod;
        
        _appDbContext.Orders.Update(order);
        await _appDbContext.SaveChangesAsync();
    
        return Ok(order);
    }
    
    [HttpPost("completewithdefault")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "client")]
    public async Task<IActionResult> CompleteOrderWithDefault([FromBody] OrderRequestWithDefault orderRequest)
    {
        var order = await _appDbContext.Orders.Include(o => o.Client).FirstOrDefaultAsync(o => o.Id == orderRequest.OrderId);

        if (order is null)
        {
            return BadRequest("заказ не найден");
        }
        
        var orderItems = _appDbContext.OrderItems.Include(o => o.Product).Where(o => o.OrderId == orderRequest.OrderId).ToList();
        var total = orderItems.Sum(o => o.Amount * o.Product.Price);
        
        if (orderRequest.PayMethod.ToLower() == "online")
        {
            var user = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Id == ContextUserId);
            if (user!.Balance < total) return BadRequest("недостаточно средств");
        }
        
        order.ToAddress = orderRequest.ToAddress;
        order.TargetPosLong = order.Client.PosLong;
        order.TargetPosLati = order.Client.PosLati;
        order.PayMethod = orderRequest.PayMethod;
        order.CreatedAt = DateTime.UtcNow;
        order.Status = GDOrderStatuses.Waiting;
        order.TotalPrice = total;
        
        _appDbContext.Orders.Update(order);
        await _appDbContext.SaveChangesAsync();
    
        return Ok(order);
    }
    
    [HttpPost("take")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "courier")]
    public async Task<IActionResult> TakeOrder([FromQuery] Guid id)
    {
        var order = await _appDbContext.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return BadRequest("заказ не найден");
        
        order.StartDeliveryAt = DateTime.UtcNow;
        order.Status = GDOrderStatuses.InDelivery;
        order.CourierId = ContextUserId;
        
        _appDbContext.Orders.Update(order);
        await _appDbContext.SaveChangesAsync();

        return Ok(order);
    }    
    
    [HttpPost("letgo/{oid:guid}")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "courier")]
    public async Task<IActionResult> Letgo([FromRoute] Guid oid)
    {
        var order = await _appDbContext.Orders.FirstOrDefaultAsync(o => o.Id == oid);
        if (order is null) return BadRequest("заказ не найден");
        
        order.CourierId = null;
        order.Status = GDOrderStatuses.Waiting;
        
        _appDbContext.Orders.Update(order);
        await _appDbContext.SaveChangesAsync();
        
        // Reset notification status for this order
        PosHub.ResetNotificationStatus(oid);

        return Ok(order);
    }

    [HttpPost("close/{id:Guid}")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "courier")]
    public async Task<IActionResult> CloseOrder([FromRoute] Guid id)
    {
        var order = await _appDbContext.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return BadRequest("заказ не найден");

        order.OrderClosedAt = DateTime.UtcNow;
        order.Status = GDOrderStatuses.Delivered;
        
        _appDbContext.Orders.Update(order);
        await _appDbContext.SaveChangesAsync();
        
        // Reset notification status for this order
        PosHub.ResetNotificationStatus(id);

        return Ok(order);
    }
}