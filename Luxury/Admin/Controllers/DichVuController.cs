using Luxury.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Luxury.Admin.Controllers
{
    [ApiController]
    [Route("api/admin/[controller]")]
	public class DichVuController : Controller
    {
        private readonly LuxuryHotelContext _context;
        public DichVuController (LuxuryHotelContext context)
        {
            _context = context;
		}
        [HttpGet]
		public async Task<IActionResult> Index()
        {
            var dichVus = await _context.Dichvus.ToListAsync();
            return Ok(dichVus);
        }
    }
}
