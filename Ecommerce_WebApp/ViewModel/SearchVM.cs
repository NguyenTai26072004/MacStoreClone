// File: ViewModels/SearchVM.cs
using Ecommerce_WebApp.Models;

public class SearchVM
{
    public string SearchTerm { get; set; }
    public IEnumerable<Product> Products { get; set; }
    public string SortBy { get; set; }
}