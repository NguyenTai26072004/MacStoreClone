// File: Controllers/SearchController.cs
using Ecommerce_WebApp.Data;
using Ecommerce_WebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class SearchController : Controller
{
    private readonly AppDbContext _db;

    public SearchController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string searchTerm, string sortBy = "newest")
    {
        var viewModel = new SearchVM { SearchTerm = searchTerm, SortBy = sortBy };

        if (!string.IsNullOrEmpty(searchTerm))
        {
            var productsQuery = _db.Products
                .Where(p => p.Name.ToLower().Contains(searchTerm.ToLower()))
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .AsNoTracking();

            // XỬ LÝ LOGIC SẮP XẾP
            switch (sortBy)
            {
                case "price_asc":
                    productsQuery = productsQuery.OrderBy(p => p.Variants.Min(v => v.Price));
                    break;
                case "price_desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.Variants.Min(v => v.Price));
                    break;
                case "newest":
                default:
                    productsQuery = productsQuery.OrderByDescending(p => p.Id);
                    break;
            }

            viewModel.Products = await productsQuery.ToListAsync();
        }
        else
        {
            viewModel.Products = new List<Product>();
        }

        return View(viewModel);
    }
}