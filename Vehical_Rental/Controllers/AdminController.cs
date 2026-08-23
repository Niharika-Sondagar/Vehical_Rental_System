using Microsoft.AspNetCore.Mvc;

namespace Vehical_Rental.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
