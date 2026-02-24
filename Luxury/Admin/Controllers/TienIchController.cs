using Luxury.Admin.ViewModel;
using Luxury.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Luxury.Admin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TienIchController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;
        public TienIchController(LuxuryHotelContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.Tieniches.ToListAsync();
            return Ok(data);
        }
        [HttpGet("{id}/chitiet")]
        public async Task<IActionResult> GetById(int id)
        {
            var lp = await _context.Tieniches.Where(t => t.MaTi == id)
                .Select(t => new
                {
                    t.MaTi,
                    t.TenTienIch,
                    t.TrangThai,
                    t.LoaiTienIch,
                    t.Icon,
                    dsBienTheLienQuan = t.TienichLoaiphongs
                        .Select(x => x.LoaiphongBienthe.TenBienThe)
                        .Distinct()
                        .ToList()
                })
            .FirstOrDefaultAsync();
            if (lp == null) return NotFound();
            return Ok(lp);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateTienIchDto dto)
        {
            //string? iconPath = null;
            //var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "IconTienIch");

            //if (!Directory.Exists(uploadPath))
            //    Directory.CreateDirectory(uploadPath);
            //if (dto.Icon != null)
            //{
            //    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.Icon.FileName)}";
            //    var filePath = Path.Combine(uploadPath, fileName);

            //    using var stream = new FileStream(filePath, FileMode.Create);
            //    await dto.Icon.CopyToAsync(stream);

            //    iconPath = $"uploads/IconTienIch/{fileName}";
            //}
            var tienIch = new Tienich
            {
                TenTienIch = dto.TenTienIch,
                LoaiTienIch = dto.LoaiTienIch,
                TrangThai = dto.TrangThai,
                Icon = dto.Icon
            };
            _context.Tieniches.Add(tienIch);
            await _context.SaveChangesAsync();
            return Ok(tienIch);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] CreateTienIchDto dto)
        {
            var tienIch = await _context.Tieniches.FindAsync(id);
            if (tienIch == null) return NotFound("Không tìm thấy tiện ích");
            tienIch.TenTienIch = dto.TenTienIch;
            tienIch.LoaiTienIch = dto.LoaiTienIch;
            tienIch.TrangThai = dto.TrangThai;
            //var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "IconTienIch");
            //if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
            //if (dto.Icon != null)
            //{
            //    if (!string.IsNullOrEmpty(tienIch.Icon))
            //    {
            //        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", tienIch.Icon);
            //        if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);
            //    }
            //    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(tienIch.Icon)}";
            //    var filePath = Path.Combine(uploadPath, fileName);
            //    using var stream = new FileStream(filePath, FileMode.Create);
            //    await dto.Icon.CopyToAsync(stream);
            //    tienIch.Icon = $"uploads/IconTienIch/{fileName}";
            //}
            tienIch.Icon = dto.Icon;
            await _context.SaveChangesAsync();
            return Ok(tienIch);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var tienIch=await _context.Tieniches.FindAsync(id);
            if (tienIch == null) return NotFound("Không tìm thấy tiện ích");
            var dangDuocSuDung = await _context.TienichLoaiphongs.AnyAsync(x => x.MaTi == id);
            if (dangDuocSuDung)
            {
                return BadRequest(new
                {
                    message = "Không thể xóa tiện ích vì đang được sử dụng cho các biến thể phòng"
                });
            }
            if (!string.IsNullOrEmpty(tienIch.Icon))
            {
                var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", tienIch.Icon);
                if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);
            }
            _context.Tieniches.Remove(tienIch);
            await _context.SaveChangesAsync();
            return Ok("Xóa tiện ích thành công");
        }
    }
}
