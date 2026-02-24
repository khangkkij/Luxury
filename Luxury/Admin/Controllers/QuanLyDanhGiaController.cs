using Luxury.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Luxury.Admin.Controllers
{
    //[Authorize(Roles = "Admin,NhanVien")]
    [Route("api/[controller]")]
    [ApiController]
    public class QuanLyDanhGiaController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;

        public QuanLyDanhGiaController(LuxuryHotelContext context)
        {
            _context = context;
        }
        [HttpGet("danh-sach")]
        public async Task<IActionResult> GetTatCaDanhGia(int page = 1, int pageSize = 10)
        {
            var query = _context.Danhgia
                .Include(dg => dg.MaDatPhongNavigation)
                    .ThenInclude(dp => dp.MaKhNavigation)
                .Include(dg => dg.MaDatPhongNavigation)
                    .ThenInclude(dp => dp.MaBienThePhongNavigation)
                .OrderByDescending(dg => dg.NgayDanhGia);

            // Tính tổng số bản ghi để Frontend biết có bao nhiêu trang
            var totalItems = await query.CountAsync();

            // Lấy dữ liệu phân trang
            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(dg => new {
                    dg.MaDg,
                    TenKhachHang = dg.MaDatPhongNavigation.MaKhNavigation.HoVaTen,
                    TenPhong = dg.MaDatPhongNavigation.MaBienThePhongNavigation.TenBienThe,
                    dg.SoSao,
                    dg.NoiDung,
                    dg.HinhAnh,
                    dg.NgayDanhGia,
                    dg.PhanHoi,
                    dg.NgayPhanHoi,
                    dg.TrangThai
                })
                .ToListAsync();

            return Ok(new { totalItems, data });
        }

        // 2. API Phản hồi đánh giá của khách
        [HttpPut("phan-hoi/{id}")]
        public async Task<IActionResult> PhanHoi(int id, [FromBody] PhanHoiRequest request)
        {
            var danhGia = await _context.Danhgia.FindAsync(id);
            if (danhGia == null) return NotFound("Không tìm thấy đánh giá");

            danhGia.PhanHoi = request.NoiDungPhanHoi;
            danhGia.NgayPhanHoi = DateTime.Now;

            _context.Entry(danhGia).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã gửi phản hồi thành công!" });
        }

        // 3. Xóa đánh giá (Nếu vi phạm tiêu chuẩn)
        [HttpDelete("xoa/{id}")]
        public async Task<IActionResult> XoaDanhGia(int id)
        {
            var dg = await _context.Danhgia.FindAsync(id);
            if (dg == null) return NotFound();

            // Xóa ảnh vật lý nếu có
            if (!string.IsNullOrEmpty(dg.HinhAnh))
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/reviews", dg.HinhAnh);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }

            _context.Danhgia.Remove(dg);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }
    public class PhanHoiRequest
    {
        public string NoiDungPhanHoi { get; set; }
    }
}
