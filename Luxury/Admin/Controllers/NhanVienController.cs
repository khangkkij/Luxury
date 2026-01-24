using Luxury.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Luxury.Admin.Controllers
{
    [ApiController]
    [Route("api/admin/[controller]")]
	public class NhanVienController : ControllerBase
	{
        private readonly LuxuryHotelContext _context;
        public NhanVienController (LuxuryHotelContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var nhanviens = await _context.Nhanviens.ToListAsync();
			return Ok(nhanviens);
		}

	}
}
