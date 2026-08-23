using Microsoft.AspNetCore.Mvc;

namespace Vehical_Rental.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
