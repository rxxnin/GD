using GD.Api.DB;
using GD.Api;
using GD.Api.DB.Models;

public static class ProductSeeder
{
    public static async Task SeedProductsAsync(AppDbContext context)
    {
        var products = new[]
        {
            new Product
            {
                Name = "Филадельфия с зеленым луком",
                Description = "Охлажденный лосось, сливочный сыр, зеленый лучок, рис, нори",
                Price = 100,
                Tags = "роллы",
                ImageValue = Images.filadelfia,
                Amount = 199
            },
            new Product
            {
                Name = "Честер ролл",
                Description = "Копченая курочка, свежие томаты, сыр Чеддер, хрустящий лук фри(внутри)",
                Price = 200,
                Tags = "фастфуд",
                ImageValue = Images.chester,
                Amount = 199
            },
            new Product
            {
                Name = "Филадельфия с авокадо",
                Description = "Соевый соус, васаби и имбирь уже в комплекте. Может содержать косточки",
                Price = 300,
                Tags = "суши",
                ImageValue = Images.filadelfia,
                Amount = 199
            }
        };

        foreach (var product in products)
        {
            if (!context.Products.Any(p => p.Name == product.Name))
            {
                await context.Products.AddAsync(product);
            }
        }

        await context.SaveChangesAsync();
    }
}
