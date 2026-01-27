using Luxury.KhachHang.ViewModel;
using Luxury.Models;
using Microsoft.AspNetCore.Mvc;

namespace KhachSanLuxury.KhachHang.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThanhToanController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;

        private const string LoaiDatCoc = "DatCoc";
        private const string LoaiThanhToan = "ThanhToan";
        private const string TrangThaiChoXacNhan = "ChoXacNhan";
        private const string TrangThaiDaThanhToan = "DaThanhToan";

        public ThanhToanController(LuxuryHotelContext context)
        {
            _context = context;
        }

        [HttpPost("dat-coc")]
        public IActionResult DatCoc([FromBody] ThanhToanRequest request)
        {
            if (request.MaDatPhong == null || request.MaDatPhong <= 0)
            {
                return BadRequest("MaDatPhong không hợp lệ");
            }

            if (request.SoTien <= 0)
            {
                return BadRequest("Số tiền không hợp lệ");
            }

            var datPhongExists = _context.Datphongs.Any(x => x.MaDatPhong == request.MaDatPhong);
            if (!datPhongExists)
            {
                return NotFound("Không tìm thấy đặt phòng");
            }

            var thanhToan = new Thanhtoan
            {
                MaDatPhong = request.MaDatPhong,
                LoaiThanhToan = LoaiDatCoc,
                SoTien = request.SoTien,
                HinhThucThanhToan = request.HinhThucThanhToan,
                TrangThai = TrangThaiChoXacNhan
            };

            _context.Thanhtoans.Add(thanhToan);
            _context.SaveChanges();

            return Ok(new { message = "Tạo đặt cọc thành công", maTt = thanhToan.MaTt });
        }

        [HttpPost("thanh-toan")]
        public IActionResult ThanhToan([FromBody] ThanhToanRequest request)
        {
            if ((request.MaDatPhong == null || request.MaDatPhong <= 0) && (request.MaHd == null || request.MaHd <= 0))
            {
                return BadRequest("Cần cung cấp MaDatPhong hoặc MaHd");
            }

            if (request.SoTien <= 0)
            {
                return BadRequest("Số tiền không hợp lệ");
            }

            if (request.MaDatPhong != null)
            {
                var datPhongExists = _context.Datphongs.Any(x => x.MaDatPhong == request.MaDatPhong);
                if (!datPhongExists)
                {
                    return NotFound("Không tìm thấy đặt phòng");
                }
            }

            if (request.MaHd != null)
            {
                var hoaDonExists = _context.Hoadons.Any(x => x.MaHd == request.MaHd);
                if (!hoaDonExists)
                {
                    return NotFound("Không tìm thấy hóa đơn");
                }
            }

            var thanhToan = new Thanhtoan
            {
                MaDatPhong = request.MaDatPhong,
                MaHd = request.MaHd,
                LoaiThanhToan = LoaiThanhToan,
                SoTien = request.SoTien,
                HinhThucThanhToan = request.HinhThucThanhToan,
                TrangThai = TrangThaiChoXacNhan
            };

            _context.Thanhtoans.Add(thanhToan);
            _context.SaveChanges();

            return Ok(new { message = "Tạo thanh toán thành công", maTt = thanhToan.MaTt });
        }

        [HttpPut("xac-nhan/{maTt}")]
        public IActionResult XacNhanThanhToan(int maTt)
        {
            if (maTt <= 0)
            {
                return BadRequest("MaTt không hợp lệ");
            }

            var thanhToan = _context.Thanhtoans.FirstOrDefault(x => x.MaTt == maTt);
            if (thanhToan == null)
            {
                return NotFound("Không tìm thấy thanh toán");
            }

            if (thanhToan.TrangThai == TrangThaiDaThanhToan)
            {
                return Ok(new { message = "Thanh toán đã được xác nhận", maTt = thanhToan.MaTt });
            }

            thanhToan.TrangThai = TrangThaiDaThanhToan;

            if (thanhToan.MaDatPhong != null)
            {
                var datPhong = _context.Datphongs.FirstOrDefault(x => x.MaDatPhong == thanhToan.MaDatPhong);
                if (datPhong != null)
                {
                    datPhong.TrangThai = TrangThaiDaThanhToan;
                }
            }

            _context.SaveChanges();

            return Ok(new { message = "Xác nhận thanh toán thành công", maTt = thanhToan.MaTt });
        }
    }
}
