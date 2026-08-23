using Microsoft.AspNetCore.Mvc;

namespace Vehical_Rental.Controllers
{
    public class VehicleController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
