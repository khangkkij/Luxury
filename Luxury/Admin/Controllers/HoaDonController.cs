using Luxury.Admin.Models;
using Luxury.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Luxury.Admin.Controllers
{
	[ApiController]
	[Route("api/admin/[controller]")]
    public class HoaDonController : Controller
    {
		private readonly LuxuryHotelContext _context;
		public HoaDonController(LuxuryHotelContext context)
		{
			_context = context;
		}
		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			var rawData = await _context.Hoadons
				// 1. Include Khách hàng
				.Include(h => h.MaDatPhongNavigation).ThenInclude(dp => dp.MaKhNavigation)

				// 2. Include Nhân viên -> VÀ LẤY THÊM CHỨC VỤ (SỬA Ở ĐÂY)
				.Include(h => h.MaDatPhongNavigation)
					.ThenInclude(dp => dp.MaNvNavigation)
						.ThenInclude(nv => nv.MaChucVuNavigation) // <--- THÊM DÒNG NÀY ĐỂ LẤY TÊN CHỨC VỤ

				// 3. Load Thanh toán (Giữ nguyên logic fix trước đó)
				.Include(h => h.Thanhtoans)
				.Include(h => h.MaDatPhongNavigation).ThenInclude(dp => dp.Thanhtoans)

				// 4. Load chi tiết phòng & dịch vụ (Giữ nguyên)
				.Include(h => h.MaDatPhongNavigation)
					.ThenInclude(dp => dp.Chitietdatphongs)
						.ThenInclude(ct => ct.MaPhongNavigation)
							.ThenInclude(p => p.MaBienThePhongNavigation)
				.Include(h => h.MaDatPhongNavigation)
					.ThenInclude(dp => dp.Chitietdatphongs)
						.ThenInclude(ct => ct.SudungDichvus)
							.ThenInclude(sd => sd.MaDichVuNavigation)

				.OrderByDescending(h => h.NgayTao)
				.ToListAsync();

			var result = rawData.Select(h => {
				var dp = h.MaDatPhongNavigation;

				// Logic lấy thanh toán (Giữ nguyên)
				var thanhToan = h.Thanhtoans.OrderByDescending(t => t.MaTt).FirstOrDefault();
				if (thanhToan == null && dp != null)
				{
					thanhToan = dp.Thanhtoans.OrderByDescending(t => t.MaTt).FirstOrDefault();
				}

				// Tính chi tiết phòng
				var listPhong = dp?.Chitietdatphongs.Select(ct =>
				{
					// THÊM .Date VÀO SAU Value ĐỂ ĐỒNG BỘ VỚI LOGIC CHECK-OUT
					int soNgay = (ct.NgayTra.HasValue && ct.NgayNhan.HasValue) ? (ct.NgayTra.Value.Date - ct.NgayNhan.Value.Date).Days : 0;
					if (soNgay <= 0) soNgay = 1;

					return new ChiTietPhongItem
					{
						SoPhong = ct.MaPhongNavigation?.SoPhong ?? "Unknown",
						LoaiPhong = ct.MaPhongNavigation?.MaBienThePhongNavigation?.TenBienThe ?? "Phòng",
						SoDem = soNgay,
						DonGia = ct.GiaDat ?? 0,
						ThanhTien = (ct.GiaDat ?? 0) * soNgay
					};
				}).ToList() ?? new List<ChiTietPhongItem>();

				// Tính chi tiết dịch vụ
				var listDichVu = dp?.Chitietdatphongs
					.SelectMany(ct => ct.SudungDichvus)
					.Select(sd => new ChiTietDichVuItem
					{
						TenDV = sd.MaDichVuNavigation?.TenDichVu ?? "Dịch vụ",
						SoLuong = 1, // Bạn có thể lấy sd.SoLuong nếu DB có cột này
						DonGia = sd.MaDichVuNavigation?.Gia ?? 0,
						ThanhTien = sd.ThanhTien ?? (sd.MaDichVuNavigation?.Gia ?? 0)
					}).ToList() ?? new List<ChiTietDichVuItem>();

				// ==========================================
				// LOGIC TÍNH PHỤ THU & PHÂN BỔ GIẢM GIÁ MỚI
				// ==========================================
				// Tính Phụ thu và Tiền cọc
				var tongTienPhong = listPhong.Sum(p => p.ThanhTien);
				var tongTienDichVu = listDichVu.Sum(d => d.ThanhTien);

				var phuThu = (h.TongTienGoc ?? 0) - tongTienPhong - tongTienDichVu;
				if (phuThu < 0) phuThu = 0;

				decimal tienCoc = 0;
				var listPrepaid = dp?.Thanhtoans.Where(t => t.TrangThai == "DaThanhToan" && t.LoaiThanhToan != "HoanTien" && t.MaHd == null).ToList();
				if (listPrepaid != null && listPrepaid.Any())
				{
					tienCoc = listPrepaid.Sum(t => t.SoTien ?? 0);
				}

				return new HoaDonDTO
				{
					MaHD = h.MaHd,
					NgayTao = h.NgayTao,
					TrangThai = thanhToan?.TrangThai ?? dp?.TrangThai ?? "Không rõ",
					TenKhachHang = dp?.MaKhNavigation?.HoVaTen ?? "Khách vãng lai",
					Sdt = dp?.MaKhNavigation?.Sdt ?? "",
					TenNhanVien = dp?.MaNvNavigation?.HoTenNv ?? "Không rõ",
					HinhAnhNhanVien = dp?.MaNvNavigation?.HinhAnh ?? "",
					ChucVuNhanVien = dp?.MaNvNavigation?.MaChucVuNavigation?.TenChucVu ?? "Không rõ",
					LoaiThanhToan = thanhToan?.LoaiThanhToan,

					TongTienGoc = h.TongTienGoc ?? 0,
					SoTienGiam = h.SoTienGiam ?? 0,
					TongTienPhaiTra = h.TongTienPhaiTra ?? 0,

					// Gán 3 biến mới vào DTO
					PhuThu = phuThu,    // <--- THÊM VÀO ĐÂY
					TienCoc = tienCoc,

					// Gán tiền cọc vào DTO để đưa lên Vu
					ChiTietPhong = listPhong,
					DichVu = listDichVu
				};
			});

			return Ok(result);
		}
	}
}
