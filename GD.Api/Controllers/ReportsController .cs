using Microsoft.AspNetCore.Mvc;
using GD.Api.DB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GD.Api.DB;
using Microsoft.EntityFrameworkCore;
using GD.Shared.Response;
using GD.Shared.Common;

namespace GD.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("order-statistics")]
        public async Task<IActionResult> GetOrderStatistics()
        {
            var now = DateTime.UtcNow;
            var last30Days = now.AddDays(-30);

            var orders = await _context.Orders
                .Where(o => o.CreatedAt >= last30Days)
                .ToListAsync();

            var dailyOrderStats = new Dictionary<DateTime, DailyOrderStatisticsDTO>();

            foreach (var order in orders)
            {
                var orderDate = order.CreatedAt.Date; // Получаем только дату (день)

                if (!dailyOrderStats.ContainsKey(orderDate))
                {
                    dailyOrderStats[orderDate] = new DailyOrderStatisticsDTO
                    {
                        Date = orderDate,
                        TotalOrders = 0,
                        SuccessfulOrders = 0,
                        FailedOrders = 0,
                        SelectingOrders = 0,
                        WaitingOrders = 0,
                        InDeliveryOrders = 0,
                        DeliveredOrders = 0
                    };
                }

                var stats = dailyOrderStats[orderDate];
                stats.TotalOrders++;

                switch (order.Status)
                {
                    case GDOrderStatuses.Selecting:
                        stats.SelectingOrders++;
                        break;
                    case GDOrderStatuses.Waiting:
                        stats.WaitingOrders++;
                        break;
                    case GDOrderStatuses.InDelivery:
                        stats.InDeliveryOrders++;
                        break;
                    case GDOrderStatuses.Delivered:
                        stats.DeliveredOrders++;
                        stats.SuccessfulOrders++;
                        break;
                    default:
                        stats.FailedOrders++;
                        break;
                }
            }

            var result = new OrderStatisticsResponseDTO
            {
                DailyStatistics = dailyOrderStats.Values.OrderBy(stat => stat.Date).ToList()
            };

            return Ok(result);
        }


        [HttpGet("order-history")]
        public async Task<IActionResult> GetOrderHistory()
        {
            // Предположим, что у заказа есть навигационные свойства для клиента и доставщика
            var orders = await _context.Orders
                .Include(o => o.Client) // Навигационное свойство для клиента
                .Include(o => o.Courier) // Навигационное свойство для клиента
                .Include(o => o.OrderItems) // Предполагается, что у заказа есть связанные заказанные товары
                    .ThenInclude(oi => oi.Product) // И у продуктов есть названия и цена и др.
                .ToListAsync();

            var orderHistory = orders.Select(order => new OrderHistoryItemDTO
            {
                Date = order.CreatedAt,
                OrderId = order.Id,
                CustomerName = $"{order.Client.Id} {order.Client.Email}",
                DeliveryAddress = order.ToAddress,
                Status = order.Status,
                DelivererName = order.Courier != null ? $"{order.Courier.Id} {order.Courier.Email}" : "Не назначен",
                TotalAmount = order.OrderItems.Sum(oi => oi.Product.Price * oi.Amount),
                OrderItems = order.OrderItems.Select(oi => new OrderItemDTO
                {
                    ProductName = oi.Product.Name,
                    Quantity = oi.Amount,
                    Price = oi.Product.Price,
                }).ToList()
            }).ToList();

            return Ok(orderHistory);
        }

        [HttpGet("revenue-statistics")]
        public async Task<IActionResult> GetRevenueStatistics()
        {
            var now = DateTime.UtcNow;
            var lastMonth = now.AddDays(-30);

            var revenue = await _context.Orders
                .Where(o => o.CreatedAt >= lastMonth)
                .SumAsync(o => o.TotalPrice);

            var result = new RevenueStatisticsDTO
            {
                Revenue = revenue
            };

            return Ok(result);
        }

        [HttpGet("least-busy-hours")]
        public async Task<IActionResult> GetLeastBusyHours()
        {
            var now = DateTime.UtcNow;
            var lastMonth = now.AddDays(-30);

            var orders = await _context.Orders
                .Where(o => o.CreatedAt >= lastMonth)
                .ToListAsync();

            var hourlyOrderStats = new Dictionary<int, int>();

            foreach (var order in orders)
            {
                var orderHour = order.CreatedAt.Hour;

                if (!hourlyOrderStats.ContainsKey(orderHour))
                {
                    hourlyOrderStats[orderHour] = 0;
                }

                hourlyOrderStats[orderHour]++;
            }

            var leastBusyHours = hourlyOrderStats
                .OrderBy(kvp => kvp.Value)
                .Take(3) // 3 наименее загруженных часа
                .Select(kvp => new { Hour = kvp.Key, OrderCount = kvp.Value })
                .ToList();

            return Ok(leastBusyHours);
        }
    }
}

