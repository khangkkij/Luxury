using Luxury.Models;
using Luxury.Admin.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Luxury.Admin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoaiPhongController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;
        public LoaiPhongController(LuxuryHotelContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.Loaiphongs.ToListAsync();
            return Ok(data);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var lp = await _context.Loaiphongs.FindAsync(id);
            if (lp == null) return NotFound();
            return Ok(lp);
        }
        [HttpPost]
        public async Task<IActionResult> Create(CreateLoaiPhongDto dto)
        {
            var lp = new Loaiphong
            {
                TenLoai = dto.TenLoai,
                MoTa = dto.MoTa
            };

            _context.Loaiphongs.Add(lp);
            await _context.SaveChangesAsync();

            return Ok(lp);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateLoaiPhongDto dto)
        {
            var lp = await _context.Loaiphongs.FindAsync(id);
            if (lp == null) return NotFound();

            lp.TenLoai = dto.TenLoai;
            lp.MoTa = dto.MoTa;

            await _context.SaveChangesAsync();
            return Ok(lp);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var lp = await _context.Loaiphongs.FindAsync(id);
            if (lp == null) return NotFound();
            if (_context.LoaiphongBienthes.Any(x => x.MaLp == id))
            {
                return BadRequest("Không thể xóa vì loại phòng đang được sử dụng");
            }

            _context.Loaiphongs.Remove(lp);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
