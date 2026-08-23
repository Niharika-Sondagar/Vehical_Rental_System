using Microsoft.AspNetCore.Mvc;

namespace Vehical_Rental.Controllers
{
    public class BookingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
