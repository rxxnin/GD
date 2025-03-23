using GD.Api.DB;
using GD.Shared.Common;
using GD.Shared.Response;
using GeoCoordinatePortable;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GD.Api.Hubs
{
    public class PosHub : Hub
    {
        private readonly AppDbContext context;
        private static readonly Dictionary<Guid, bool> NotificationSentMap = new Dictionary<Guid, bool>();

        public PosHub(AppDbContext context)
        {
            this.context = context;
        }

        public async Task SendPos(HubPosInfo info)
        {
            var u = await context.Users.FirstOrDefaultAsync(u => u.Id == info.UserId);
            if (u == null) return;

            u.PosLati = info.TargetPosLati;
            u.PosLong = info.TargetPosLong;

            context.Users.Update(u);
            await context.SaveChangesAsync();

            // Проверка расстояния до клиента
            var ordersInDelivery = await context.Orders
                .Include(o => o.Client)
                .Where(o => o.CourierId == info.UserId && o.Status == GDOrderStatuses.InDelivery)
                .ToListAsync();

            foreach (var order in ordersInDelivery)
            {
                // Skip if notification was already sent for this order
                if (NotificationSentMap.TryGetValue(order.Id, out bool sent) && sent)
                {
                    continue;
                }

                var courierCoord = new GeoCoordinate(info.TargetPosLati, info.TargetPosLong);
                var clientCoord = new GeoCoordinate(order.Client.PosLati, order.Client.PosLong);

                double distance = courierCoord.GetDistanceTo(clientCoord); // Расстояние в метрах

                if (distance < 150)
                {
                    // Try to calculate approximate arrival time based on previous positions
                    int estimatedMinutes = 3; // Default 3 minutes

                    // Mark as notification sent for this order to avoid sending multiple times
                    NotificationSentMap[order.Id] = true;

                    // Send notification to the client
                    await Clients.User(order.ClientId.ToString()).SendAsync("ReceiveNotification", $"Курьер приближается, ожидайте через {estimatedMinutes} минуты!");
                    
                    // Log notification sent
                    Console.WriteLine($"Notification sent to client {order.ClientId} for order {order.Id}, courier distance: {distance}m");
                }
            }

            await Clients.All.SendAsync("SharePos", info);
        }

        // Reset notification status when order status changes (can be called from OrderController)
        public static void ResetNotificationStatus(Guid orderId)
        {
            if (NotificationSentMap.ContainsKey(orderId))
            {
                NotificationSentMap.Remove(orderId);
            }
        }
    }
}