using Luxury.Admin.ViewModel;
using Luxury.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Luxury.Admin.Controllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    public class QuanLyPhongController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;
        public QuanLyPhongController(LuxuryHotelContext context)
        {
            _context = context;
        }
        [HttpPost]
        public async Task<IActionResult> Create(CreatePhongDto dto)
        {
            // check trùng số phòng
            if (_context.Phongs.Any(p => p.SoPhong == dto.SoPhong))
                return BadRequest("Số phòng đã tồn tại");

            var phong = new Phong
            {
                SoPhong = dto.SoPhong,
                MaBienThePhong = dto.MaBienThePhong,
                TrangThai = dto.TrangThai
            };

            _context.Phongs.Add(phong);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Thêm phòng thành công",
                phong.MaPhong
            });
        }
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var phong = _context.Phongs
                    .FirstOrDefault(p => p.MaPhong == id);

                if (phong == null)
                    return NotFound("Không tìm thấy phòng");
                if (phong.TrangThai == 2)
                    return BadRequest("Phòng đang có khách, không thể xóa");

                // 1️⃣ Xóa đánh giá
                var danhGia = _context.Danhgia
                    .Where(dg => dg.MaPhong.HasValue && dg.MaPhong.Value == id);
                _context.Danhgia.RemoveRange(danhGia);

                // 2️⃣ Xóa chi tiết đặt phòng
                var chiTietDP = _context.Chitietdatphongs
                    .Where(ct => ct.MaPhong.HasValue && ct.MaPhong.Value == id);
                _context.Chitietdatphongs.RemoveRange(chiTietDP);

                // 3️⃣ Xóa phòng
                _context.Phongs.Remove(phong);

                _context.SaveChanges();
                transaction.Commit();

                return Ok("Xóa phòng thành công");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest("Lỗi khi xóa phòng: " + ex.Message);
            }
        }
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var phong= _context.Phongs.FirstOrDefault(x=>x.MaPhong==id);
            if (phong == null) return NotFound();
            return Ok(phong);
        }
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] UpdatePhongDto dto)
        {
            var phong = _context.Phongs
                .Include(p => p.Chitietdatphongs)
                .FirstOrDefault(p => p.MaPhong == id);
            if (phong == null)
                return NotFound("Không tìm thấy phòng");
            bool dangCoKhach = phong.Chitietdatphongs
                .Any(ct => ct.TrangThai == true);

            if (dangCoKhach)
                return BadRequest("Không thể sửa trạng thái khi phòng đang có khách");
            // chỉ cho sửa các field cho phép
            if (dto.SoPhong != null)
                phong.SoPhong = dto.SoPhong;

            if (dto.TrangThai.HasValue)
                phong.TrangThai = dto.TrangThai;

            _context.SaveChanges();

            return Ok(new
            {
                message = "Cập nhật phòng thành công",
                data = phong
            });
        }
        [HttpGet("{id}/detail")]
        public IActionResult GetDetail(int id)
        {
            var phong = _context.Phongs
                .Include(p => p.MaBienThePhongNavigation)
                    .ThenInclude(bt => bt.Thietlapgia)
                .Include(p => p.Chitietdatphongs)
                    .ThenInclude(ct => ct.MaDatPhongNavigation)
                        .ThenInclude(dp => dp.MaKhNavigation)
                .FirstOrDefault(p => p.MaPhong == id);

            if (phong == null)
                return NotFound("Không tìm thấy phòng");

            var bookingDangO = phong.Chitietdatphongs
                .FirstOrDefault(ct =>
                    ct.TrangThai == true &&
                    ct.NgayNhan <= DateTime.Now &&
                    (ct.NgayTra == null || ct.NgayTra >= DateTime.Now)
                );

            return Ok(new
            {
                maPhong = phong.MaPhong,
                soPhong = phong.SoPhong,
                trangThai = phong.TrangThai,

                tenBienThe = phong.MaBienThePhongNavigation?.TenBienThe,
                gia = phong.MaBienThePhongNavigation?
                        .Thietlapgia
                        .Where(x => x.MacDinh == true)
                        .Select(x => x.Gia)
                        .FirstOrDefault(),

                khachDangThue = bookingDangO == null ? null : new
                {
                    tenKhach = bookingDangO.MaDatPhongNavigation?.MaKhNavigation?.HoVaTen,
                    dienThoai = bookingDangO.MaDatPhongNavigation?.MaKhNavigation?.Sdt,
                    checkIn = bookingDangO.NgayNhan,
                    checkOut = bookingDangO.NgayTra
                }
            });
        }
    }
}
