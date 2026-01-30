using Luxury.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Luxury.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    public class VoucherController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;

        // Định nghĩa các giới hạn cứng
        private const decimal MAX_MONEY_LIMIT = 10_000_000; // 10 triệu VNĐ
        private const int MAX_QUANTITY_LIMIT = 10000;      // Tối đa 10.000 Voucher

        public VoucherController(LuxuryHotelContext context) { _context = context; }

        // 1. LẤY DANH SÁCH
        [HttpGet]
        public async Task<IActionResult> GetVouchers()
        {
            var data = await _context.Vouchers
                .AsNoTracking()
                .OrderByDescending(v => v.NgayBatDau)
                .ToListAsync();
            return Ok(data);
        }

        // 2. TẠO MỚI (CREATE)
        [HttpPost]
        public async Task<IActionResult> CreateVoucher([FromBody] Voucher model)
        {
            string error = ValidateVoucherData(model);
            if (error != null) return BadRequest(new { message = error });

            // Kiểm tra trùng mã (Trim và ToUpper để đồng bộ)
            string cleanCode = model.MaCode.Trim().ToUpper();
            if (await _context.Vouchers.AnyAsync(v => v.MaCode == cleanCode))
                return BadRequest(new { message = "Mã Voucher này đã tồn tại trên hệ thống." });

            model.MaCode = cleanCode;
            model.SoLuongDaDung = 0;
            model.SoTienGiam = model.SoTienGiam ?? 0;
            model.GiaToiThieu = model.GiaToiThieu ?? 0;

            _context.Vouchers.Add(model);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Tạo mới voucher thành công!" });
        }

        // 3. CẬP NHẬT (UPDATE)
        [HttpPut("{code}")]
        public async Task<IActionResult> Update(string code, [FromBody] Voucher model)
        {
            var voucher = await _context.Vouchers.FindAsync(code);
            if (voucher == null) return NotFound(new { message = "Không tìm thấy mã voucher." });

            string error = ValidateVoucherData(model);
            if (error != null) return BadRequest(new { message = error });

            // Cập nhật các trường cho phép
            voucher.MoTa = model.MoTa;
            voucher.PhanTramGiam = model.PhanTramGiam;
            voucher.SoTienGiam = model.SoTienGiam ?? 0;
            voucher.GiaToiThieu = model.GiaToiThieu ?? 0;
            voucher.NgayBatDau = model.NgayBatDau;
            voucher.NgayKetThuc = model.NgayKetThuc;
            voucher.SoLuong = model.SoLuong;
            voucher.TrangThai = model.TrangThai;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Cập nhật dữ liệu thành công!" });
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, new { message = "Lỗi xung đột dữ liệu hệ thống." });
            }
        }

        // 4. XÓA (DELETE)
        [HttpDelete("{code}")]
        public async Task<IActionResult> Delete(string code)
        {
            var voucher = await _context.Vouchers.FindAsync(code);
            if (voucher == null) return NotFound(new { message = "Voucher không tồn tại." });

            // Kiểm tra ràng buộc lịch sử sử dụng
            bool isUsed = await _context.Datphongs.AnyAsync(d => d.MaCode == code);
            if (isUsed) return BadRequest(new { message = "Voucher này đã có lịch sử sử dụng, không thể xóa vĩnh viễn!" });

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa voucher khỏi hệ thống." });
        }

        // HÀM VALIDATE DỮ LIỆU TẬP TRUNG
        private string ValidateVoucherData(Voucher model)
        {
            if (string.IsNullOrWhiteSpace(model.MaCode)) return "Mã Voucher không được để trống.";

            // 1. Chặn mã có dấu, khoảng trắng, ký tự đặc biệt (Chỉ cho phép A-Z và 0-9)
            var regex = new Regex(@"^[a-zA-Z0-9]+$");
            if (!regex.IsMatch(model.MaCode))
                return "Mã Voucher không được có dấu, khoảng trắng hoặc ký tự đặc biệt.";

            // 2. Validate Số lượng (Tối đa 10.000)
            if (model.SoLuong <= 0) return "Số lượng phát hành phải lớn hơn 0.";
            if (model.SoLuong > MAX_QUANTITY_LIMIT)
                return $"Số lượng phát hành không được vượt quá {MAX_QUANTITY_LIMIT:N0}.";

            // 3. Validate Giảm giá: KHÔNG cho phép nhập cả hai loại cùng lúc
            bool hasPercent = (model.PhanTramGiam ?? 0) > 0;
            bool hasCash = (model.SoTienGiam ?? 0) > 0;

            if (hasPercent && hasCash)
                return "Chỉ được chọn một loại giảm giá (Phần trăm hoặc Tiền mặt), không được nhập cả hai.";

            if (!hasPercent && !hasCash)
                return "Vui lòng nhập mức giảm giá theo phần trăm hoặc tiền mặt.";

            if (model.PhanTramGiam < 0 || model.PhanTramGiam > 100) return "Phần trăm giảm phải từ 0-100%.";
            if (model.SoTienGiam < 0) return "Số tiền giảm không được là số âm.";

            // 4. Validate Ngày tháng
            if (model.NgayBatDau == null || model.NgayKetThuc == null) return "Vui lòng chọn thời gian hiệu lực cho Voucher.";
            if (model.NgayKetThuc < model.NgayBatDau) return "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.";

            // 5. Validate Giới hạn tiền mặt (Chống tràn số)
            if (model.SoTienGiam > MAX_MONEY_LIMIT) return "Số tiền giảm vượt quá giới hạn (Tối đa 10 Triệu).";
            if (model.GiaToiThieu > MAX_MONEY_LIMIT) return "Giá tối thiểu vượt quá giới hạn (Tối đa 10 Triệu).";

            return null;
        }
    }
}