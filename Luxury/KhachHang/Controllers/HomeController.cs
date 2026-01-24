using Luxury.Models;
using Microsoft.AspNetCore.Mvc;

namespace Luxury.KhachHang.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class HomeController : Controller
    {
        private readonly LuxuryHotelContext _context;
        public HomeController(LuxuryHotelContext context)
        {
            _context = context;
		}
        [HttpGet]
        public IActionResult GetAll ()
        {
            var sanpham = _context.Phongs.ToList();
			return Ok(sanpham);
		}
    }
}
