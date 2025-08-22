namespace Ecommerce_WebApp.Models
{
    // File: Models/CartItem.cs
    public class CartItem
    {
        public int VariantId { get; set; }
        public string ProductName { get; set; }
        public string VariantDescription { get; set; } 
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public decimal SubTotal => Quantity * Price;
    }

}
