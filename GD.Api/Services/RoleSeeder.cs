using GD.Api.DB;
using GD.Api.DB.Models;
using GD.Shared.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static class RoleSeeder
{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        var roles = new[] { GDUserRoles.Client, GDUserRoles.Admin, GDUserRoles.Courier };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role) { Id = Guid.NewGuid() });
            }
        }
    }
}

public static class AdminSeeder
{
    public static async Task SeedAdminAsync(UserManager<GDUser> userManager)
    {
        const string email = "admin@a";
        const string password = "a123";

        if (!userManager.Users.Any(u => u.Email == email))
        {
            var user = new GDUser { Email = email, UserName = email };
            await userManager.CreateAsync(user, password);
            await userManager.AddToRoleAsync(user, GDUserRoles.Admin);
        }
    }
}


public static class UserAndOrderSeeder
{
    private static readonly Random Random = new Random();

    public static async Task SeedUsersAndOrdersAsync(UserManager<GDUser> userManager, AppDbContext context)
    {
        var clients = new[]
        {
            new { Email = "client1@c", Password = "qwerty", Balance = Random.Next(1000, 2001) },
            new { Email = "client2@c", Password = "qwerty", Balance = Random.Next(1000, 2001) },
            new { Email = "client3@c", Password = "qwerty", Balance = Random.Next(1000, 2001) },
            new { Email = "client4@c", Password = "qwerty", Balance = Random.Next(1000, 2001) },
            new { Email = "client5@c", Password = "qwerty", Balance = Random.Next(1000, 2001) }
        };

        var couriers = new[]
        {
            new { Email = "courier1@c", Password = "qwerty" },
            new { Email = "courier2@c", Password = "qwerty" },
            new { Email = "courier3@c", Password = "qwerty" }
        };

        // Создание клиентов
        foreach (var client in clients)
        {
            if (!userManager.Users.Any(u => u.Email == client.Email))
            {
                var user = new GDUser { Email = client.Email, UserName = client.Email, Balance = client.Balance };
                await userManager.CreateAsync(user, client.Password);
                await userManager.AddToRoleAsync(user, GDUserRoles.Client);
            }
        }

        // Создание курьеров
        foreach (var courier in couriers)
        {
            if (!userManager.Users.Any(u => u.Email == courier.Email))
            {
                var user = new GDUser { Email = courier.Email, UserName = courier.Email, Balance = 0 };
                await userManager.CreateAsync(user, courier.Password);
                await userManager.AddToRoleAsync(user, GDUserRoles.Courier);
            }
        }

        // Список адресов
        var addresses = new[]
        {
            "Набережные Челны, ул Моторная, д 17",
            "Набережные Челны, пр-т Мира, д 42",
            "Набережные Челны, ул Хасана Туфана, д 12",
            "Набережные Челны, ул Машиностроительная, д 25",
            "Набережные Челны, ул Раскольникова, д 8"
        };

        // Создание заказов
        var products = await context.Products.ToListAsync();
        var users = await userManager.Users.Where(u => u.Email.StartsWith("client")).ToListAsync();

        foreach (var user in users)
        {
            // Генерация случайных координат в радиусе 100 км
            var (longitude, latitude) = GenerateRandomCoordinates(52.447772, 55.738159, 100);

            // Случайный выбор адреса
            var address = addresses[Random.Next(addresses.Length)];

            // Случайный выбор продуктов и их количества
            var orderItems = new List<OrderItem>();
            var totalPrice = 0d;

            // Выбираем случайное количество продуктов (от 1 до 5)
            int numberOfProducts = Random.Next(1, 6);
            for (int i = 0; i < numberOfProducts; i++)
            {
                var product = products[Random.Next(products.Count)];
                var amount = Random.Next(1, 6); // Случайное количество от 1 до 5
                orderItems.Add(new OrderItem { ProductId = product.Id, Amount = amount });
                totalPrice += product.Price * amount;
            }

            var order = new Order
            {
                CreatedAt = DateTime.UtcNow,
                Status = GDOrderStatuses.Waiting,
                ClientId = user.Id,
                PayMethod = GDPayMethods.Online,
                ToAddress = address,
                OrderItems = orderItems,
                TotalPrice = (double)totalPrice,
                TargetPosLong = longitude,
                TargetPosLati = latitude,
            };

            await context.Orders.AddAsync(order);
        }

        await context.SaveChangesAsync();
    }

    private static (double Longitude, double Latitude) GenerateRandomCoordinates(double centerLongitude, double centerLatitude, double radiusInKm)
    {
        // Генерация случайного расстояния и угла
        double radius = radiusInKm / 6371; // Переводим радиус в радианы
        double angle = Random.NextDouble() * 2 * Math.PI;
        double distance = Random.NextDouble() * radius;

        // Вычисление новых координат
        double newLatitude = centerLatitude + (distance * Math.Sin(angle));
        double newLongitude = centerLongitude + (distance * Math.Cos(angle)) / Math.Cos(centerLatitude * Math.PI / 180);

        return (newLongitude, newLatitude);
    }
}