using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce_WebApp.Controllers
{
    public class ProductDetailController : Controller
    {
        // GET: ProductDetail
        public ActionResult Index()
        {
            return View();
        }

        // GET: ProductDetail/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ProductDetail/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ProductDetail/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ProductDetail/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ProductDetail/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ProductDetail/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ProductDetail/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
