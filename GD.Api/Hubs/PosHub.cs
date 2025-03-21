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
            var order = await context.Orders
                .Include(o => o.Client)
                .FirstOrDefaultAsync(o => o.CourierId == info.UserId && o.Status == GDOrderStatuses.InDelivery);

            if (order != null)
            {
                var courierCoord = new GeoCoordinate(info.TargetPosLati, info.TargetPosLong);
                var clientCoord = new GeoCoordinate(order.Client.PosLati, order.Client.PosLong);

                double distance = courierCoord.GetDistanceTo(clientCoord); // Расстояние в метрах

                if (distance < 150)
                {
                    await Clients.User(order.ClientId.ToString()).SendAsync("ReceiveNotification", "Курьер приближается, ожидайте через 3 минуты.");
                }
            }

            await Clients.All.SendAsync("SharePos", info);
        }

        //public async Task SendPos(HubPosInfo info)
        //{
        //    // Тестовые данные (временные)
        //    var courierCoord = new GeoCoordinate(info.TargetPosLati, info.TargetPosLong);
        //    var clientCoord = new GeoCoordinate(55.7520, 37.6176); // Координаты клиента

        //    double distance = courierCoord.GetDistanceTo(clientCoord); // Расстояние в метрах
        //    Console.WriteLine($"Расстояние до клиента: {distance} метров");

        //    if (distance < 150)
        //    {
        //        Console.WriteLine("Уведомление отправлено: Курьер приближается, ожидайте через 3 минуты.");
        //        await Clients.User("test-client-id").SendAsync("ReceiveNotification", "Курьер приближается, ожидайте через 3 минуты.");
        //    }
        //    else
        //    {
        //        Console.WriteLine("Расстояние больше 150 метров. Уведомление не отправлено.");
        //    }

        //    await Clients.All.SendAsync("SharePos", info);
        //}
    }
}