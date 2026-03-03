using Luxury.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Luxury.Admin.Controllers
{
    [ApiController]
    [Route("api/admin/[controller]")]
    public class KhachHangController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;

        public KhachHangController(LuxuryHotelContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var customers = await _context.Khachhangs.ToListAsync();
            return Ok(customers);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var customer = await _context.Khachhangs.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            return Ok(customer);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Khachhang customer)
        {
            if (customer == null)
            {
                return BadRequest();
            }

            if (customer.NgayTao == null)
            {
                customer.NgayTao = DateTime.Now;
            }

            if (customer.TrangThai == null)
            {
                customer.TrangThai = true;
            }

            _context.Khachhangs.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = customer.MaKh }, customer);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Khachhang customer)
        {
            if (customer == null || id != customer.MaKh)
            {
                return BadRequest();
            }

            var existing = await _context.Khachhangs.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.HoVaTen = customer.HoVaTen;
            existing.Email = customer.Email;
            existing.Cccd = customer.Cccd;
            existing.Sdt = customer.Sdt;
            existing.GioiTinh = customer.GioiTinh;
            existing.HinhAnh = customer.HinhAnh;
            existing.TrangThai = customer.TrangThai;
            if (customer.NgayTao != null)
            {
                existing.NgayTao = customer.NgayTao;
            }
            if (!string.IsNullOrWhiteSpace(customer.MatKhau))
            {
                existing.MatKhau = customer.MatKhau;
            }

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _context.Khachhangs.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            _context.Khachhangs.Remove(customer);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
