using Luxury.Admin.Models;
using Luxury.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Luxury.NhanVien.Controllers
{
	[ApiController]
	[Route("api/NhanVien/[controller]")]
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

				// Logic tính toán chi tiết (Giữ nguyên)
				var listPhong = dp?.Chitietdatphongs.Select(ct =>
				{
					int soNgay = (ct.NgayTra.HasValue && ct.NgayNhan.HasValue) ? (ct.NgayTra.Value - ct.NgayNhan.Value).Days : 0;
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

				var listDichVu = dp?.Chitietdatphongs
					.SelectMany(ct => ct.SudungDichvus)
					.Select(sd => new ChiTietDichVuItem
					{
						TenDV = sd.MaDichVuNavigation?.TenDichVu ?? "Dịch vụ",
						SoLuong = 1,
						DonGia = sd.MaDichVuNavigation?.Gia ?? 0,
						ThanhTien = sd.ThanhTien ?? (sd.MaDichVuNavigation?.Gia ?? 0)
					}).ToList() ?? new List<ChiTietDichVuItem>();

				return new HoaDonDTO
				{
					MaHD = h.MaHd,
					NgayTao = h.NgayTao,
					TrangThai = thanhToan?.TrangThai ?? dp?.TrangThai ?? "Không rõ",
					TenKhachHang = dp?.MaKhNavigation?.HoVaTen ?? "Khách vãng lai",
					Sdt = dp?.MaKhNavigation?.Sdt ?? "",

					// --- THÔNG TIN NHÂN VIÊN MỚI ---
					TenNhanVien = dp?.MaNvNavigation?.HoTenNv ?? "Không rõ",
					// Lấy hình ảnh (nếu null thì để chuỗi rỗng hoặc link ảnh mặc định)
					HinhAnhNhanVien = dp?.MaNvNavigation?.HinhAnh ?? "",
					// Lấy tên chức vụ từ bảng ChucVu (Dùng ? để tránh lỗi nếu null)
					ChucVuNhanVien = dp?.MaNvNavigation?.MaChucVuNavigation?.TenChucVu ?? "Không rõ",

					LoaiThanhToan = thanhToan?.LoaiThanhToan,
					TongTienGoc = h.TongTienGoc ?? 0,
					SoTienGiam = h.SoTienGiam ?? 0,
					TongTienPhaiTra = h.TongTienPhaiTra ?? 0,
					ChiTietPhong = listPhong,
					DichVu = listDichVu
				};
			});

			return Ok(result);
		}
	}
}
