using Ecommerce_WebApp.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Ecommerce_WebApp.ViewModels
{
    public class CheckoutVM
    {

        public ShoppingCart ShoppingCart { get; set; } = new ShoppingCart();


        [ValidateNever] 
        public OrderHeader OrderHeader { get; set; } = new OrderHeader();

        public decimal ShippingFee { get; set; } = 0; 

        [Display(Name = "Hình thức thanh toán")]
        [Required(ErrorMessage = "Vui lòng chọn hình thức thanh toán.")]
        public string PaymentMethod { get; set; } = "COD"; 
    }
}