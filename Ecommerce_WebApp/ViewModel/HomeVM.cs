using Ecommerce_WebApp.Models;
using System.Collections.Generic;

namespace Ecommerce_WebApp.ViewModels
{
    public class HomeVM
    {
        public IEnumerable<Product> NewProducts { get; set; }
        public IEnumerable<Product> MacBookProducts { get; set; }
        public IEnumerable<Product> IMacProducts { get; set; }
        public IEnumerable<Product> MacStudioProducts { get; set; }
        public IEnumerable<Product> MacMiniProducts { get; set; }
        public IEnumerable<Product> PhuKien { get; set; }


    }
}