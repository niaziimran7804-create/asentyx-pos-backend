using Microsoft.AspNetCore.Mvc;

namespace POS.Api.Controllers
{
    public class ReceptingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
