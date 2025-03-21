using System.Text.Json.Serialization;

namespace GD.Api.DB.Models
{
    public class Product
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageValue { get; set; }
        public double Price { get; set; }
        public string Tags { get; set; }
        public int Amount { get; set; }
        public bool IsDeleted { get; set; }
        public List<Feedback> Feedbacks { get;set;}
    }

    public class Feedback
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }
        [JsonIgnore]
        public Product Product { get; set; }

        public int Stars { get; set; }
        
        public Guid ClientId { get; set; }
        [JsonIgnore]
        public GDUser Client { get; set; }
        
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}