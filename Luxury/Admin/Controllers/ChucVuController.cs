using Luxury.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Luxury.Admin.Controllers
{
    [ApiController]
    [Route("api/admin/[controller]")]
    public class ChucVuController : Controller
    {
        private readonly LuxuryHotelContext _context;
        public ChucVuController(LuxuryHotelContext context)
        {
            _context = context;
		}
		[HttpGet]
        public IActionResult GetData()
        {
            var data = _context.Chucvus.ToList();
            return Ok(data);
        }
    }
}
