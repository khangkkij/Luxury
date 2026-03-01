using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.IO;
using Luxury.Models; // Đảm bảo namespace này khớp với file LuxuryHotelContext.cs của bạn

namespace Luxury.KhachHang.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HuyDatPhongController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;

        public HuyDatPhongController(LuxuryHotelContext context)
        {
            _context = context;
        }

        [HttpPost("ThucHienHuy")]
        public async Task<IActionResult> ThucHienHuy(int maDatPhong, [FromBody] CancelRequest request)
        {
            // 1. Tìm đơn đặt phòng kèm thông tin chi tiết và lịch sử thanh toán
            var order = await _context.Datphongs
                .Include(d => d.Chitietdatphongs)
                .Include(d => d.Thanhtoans)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);

            if (order == null) return NotFound(new { success = false, message = "Không tìm thấy đơn hàng!" });

            // 2. Lấy thông tin ngày tháng
            var chiTiet = order.Chitietdatphongs.FirstOrDefault();
            DateTime ngayDat = order.NgayDat ?? DateTime.Now;
            DateTime ngayNhan = chiTiet?.NgayNhan ?? DateTime.Now;

            // 3. Logic xác định số tiền thực tế khách đã chi trả (Cọc hoặc Trả hết)
            // Lấy bản ghi thanh toán mới nhất để làm căn cứ
            var banGhiThanhToan = order.Thanhtoans.OrderByDescending(t => t.MaTt).FirstOrDefault();

            decimal soTienThucTeDaTra = banGhiThanhToan?.SoTien ?? 0;
            string loaiHinhHuy = (banGhiThanhToan?.LoaiThanhToan == "DatCoc") ? "Hoàn tiền cọc" : "Hoàn tiền thanh toán";

            // 4. Tính toán số tiền hoàn dựa trên thời gian
            decimal soTienHoanTra = TinhSoTienHoan(ngayDat, ngayNhan, soTienThucTeDaTra);

            // 5. Lưu vào file Excel báo cáo
            try
            {
                LuuVaoExcel(maDatPhong, request, soTienHoanTra, loaiHinhHuy);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Lỗi hệ thống khi ghi file báo cáo." });
            }

            // 6. Cập nhật trạng thái đơn hàng trong SQL
            order.TrangThai = "Da huy";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                amount = soTienHoanTra,
                message = $"Hủy thành công. {loaiHinhHuy} dự kiến: {soTienHoanTra:N0}đ"
            });
        }

        private decimal TinhSoTienHoan(DateTime ngayDat, DateTime ngayNhan, decimal soTienGoc)
        {
            DateTime ngayHuy = DateTime.Now;
            double soNgayTuLucDat = (ngayHuy - ngayDat).TotalDays;
            double soNgayTruocCheckIn = (ngayNhan - ngayHuy).TotalDays;

            if (soNgayTuLucDat <= 1) return soTienGoc; // Trong ngày: 100%
            if (soNgayTruocCheckIn >= 7) return soTienGoc; // Trước 7 ngày: 100%
            if (soNgayTruocCheckIn >= 3) return soTienGoc * 0.7m; // Trước 3 ngày: 70%
            if (soNgayTruocCheckIn > 0) return soTienGoc * 0.5m; // Trước ngày nhận: 50%

            return 0;
        }

        private void LuuVaoExcel(int maDP, CancelRequest req, decimal tienHoan, string loaiHinh)
        {
            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Reports");
            string filePath = Path.Combine(folderPath, "DanhSachHuy.xlsx");

            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            using (var workbook = System.IO.File.Exists(filePath) ? new XLWorkbook(filePath) : new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Count > 0 ? workbook.Worksheets.Worksheet(1) : workbook.Worksheets.Add("DSHuyPhong");

                if (worksheet.LastRowUsed() == null)
                {
                    worksheet.Cell(1, 1).Value = "Mã Đơn";
                    worksheet.Cell(1, 2).Value = "Chủ TK";
                    worksheet.Cell(1, 3).Value = "STK";
                    worksheet.Cell(1, 4).Value = "Ngân Hàng";
                    worksheet.Cell(1, 5).Value = "Số Tiền Hoàn";
                    worksheet.Cell(1, 6).Value = "Loại Hình";
                    worksheet.Cell(1, 7).Value = "Trạng Thái";
                    worksheet.Cell(1, 8).Value = "Ngày Yêu Cầu";
                    worksheet.Row(1).Style.Font.Bold = true;
                }

                int nextRow = (worksheet.LastRowUsed()?.RowNumber() ?? 1) + 1;
                worksheet.Cell(nextRow, 1).Value = maDP;
                worksheet.Cell(nextRow, 2).Value = req.AccountName;
                worksheet.Cell(nextRow, 3).Value = req.AccountNumber;
                worksheet.Cell(nextRow, 4).Value = req.BankName;
                worksheet.Cell(nextRow, 5).Value = tienHoan;
                worksheet.Cell(nextRow, 6).Value = loaiHinh;
                worksheet.Cell(nextRow, 7).Value = "Chưa chuyển";
                worksheet.Cell(nextRow, 8).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                workbook.SaveAs(filePath);
            }
        }
    }

    public class CancelRequest
    {
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public string BankName { get; set; }
    }
}