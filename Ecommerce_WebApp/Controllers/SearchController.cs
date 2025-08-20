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

    public async Task<IActionResult> Index(string searchTerm)
    {
        var viewModel = new SearchVM { SearchTerm = searchTerm };

        if (!string.IsNullOrEmpty(searchTerm))
        {
            // Tìm sản phẩm có Name chứa searchTerm (không phân biệt hoa thường)
            viewModel.Products = await _db.Products
                .Where(p => p.Name.ToLower().Contains(searchTerm.ToLower()))
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .AsNoTracking()
                .ToListAsync();
        }
        else
        {
            viewModel.Products = new List<Product>();
        }

        return View(viewModel);
    }
}