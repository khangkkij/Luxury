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
			// BƯỚC 1: TRUY VẤN VÀ CHỈ LẤY CÁC TRƯỜNG CẦN THIẾT (Bypass lỗi cột TrangThai)
			var rawData = await _context.Hoadons
				.OrderByDescending(h => h.NgayTao)
				.Select(h => new
				{
					// Thông tin Hóa đơn
					h.MaHd,
					h.NgayTao,
					h.TongTienGoc,
					h.SoTienGiam,
					h.TongTienPhaiTra,

					// Lấy bản ghi Thanh Toán mới nhất (của Hóa đơn hoặc Đặt phòng)
					ThanhToan = h.Thanhtoans.OrderByDescending(t => t.MaTt).FirstOrDefault()
							 ?? h.MaDatPhongNavigation.Thanhtoans.OrderByDescending(t => t.MaTt).FirstOrDefault(),

					// Lấy Tiền cọc (Tính tổng ngay dưới DB cho nhẹ)
					TienCoc = h.MaDatPhongNavigation != null
							  ? h.MaDatPhongNavigation.Thanhtoans
								  .Where(t => t.TrangThai == "DaThanhToan" && t.LoaiThanhToan != "HoanTien" && t.MaHd == null)
								  .Sum(t => (decimal?)(t.SoTien ?? 0)) ?? 0
							  : 0,

					// Lấy thông tin Đặt phòng, Khách hàng, Nhân viên, Chức vụ
					TrangThaiDatPhong = h.MaDatPhongNavigation.TrangThai,
					TenKhachHang = h.MaDatPhongNavigation.MaKhNavigation.HoVaTen,
					SdtKhachHang = h.MaDatPhongNavigation.MaKhNavigation.Sdt,
					TenNhanVien = h.MaDatPhongNavigation.MaNvNavigation.HoTenNv, // Hoặc thuộc tính tên NV của bạn
					HinhAnhNhanVien = h.MaDatPhongNavigation.MaNvNavigation.HinhAnh,
					TenChucVu = h.MaDatPhongNavigation.MaNvNavigation.MaChucVuNavigation.TenChucVu, // Lấy tên chức vụ ở đây

					// Lấy danh sách Chi tiết phòng
					ChiTietPhongs = h.MaDatPhongNavigation.Chitietdatphongs.Select(ct => new
					{
						ct.NgayNhan,
						ct.NgayTra,
						ct.GiaDat,
						SoPhong = ct.MaPhongNavigation.SoPhong,
						LoaiPhong = ct.MaPhongNavigation.MaBienThePhongNavigation.TenBienThe
					}).ToList(),

					// Lấy danh sách Dịch vụ
					DichVus = h.MaDatPhongNavigation.Chitietdatphongs
						.SelectMany(ct => ct.SudungDichvus)
						.Select(sd => new
						{
							TenDV = sd.MaDichVuNavigation.TenDichVu,
							DonGia = sd.MaDichVuNavigation.Gia,
							ThanhTien = sd.ThanhTien
						}).ToList()
				})
				.ToListAsync();

			// BƯỚC 2: MAP SANG DTO TRONG BỘ NHỚ THEO ĐÚNG LOGIC CỦA BẠN
			var result = rawData.Select(x => {

				// 1. Tính chi tiết phòng
				var listPhong = x.ChiTietPhongs.Select(ct => {
					int soNgay = (ct.NgayTra.HasValue && ct.NgayNhan.HasValue)
								 ? (ct.NgayTra.Value.Date - ct.NgayNhan.Value.Date).Days
								 : 0;
					if (soNgay <= 0) soNgay = 1;

					return new ChiTietPhongItem
					{
						SoPhong = ct.SoPhong ?? "Unknown",
						LoaiPhong = ct.LoaiPhong ?? "Phòng",
						SoDem = soNgay,
						DonGia = ct.GiaDat ?? 0,
						ThanhTien = (ct.GiaDat ?? 0) * soNgay
					};
				}).ToList();

				// 2. Tính chi tiết dịch vụ
				var listDichVu = x.DichVus.Select(sd => new ChiTietDichVuItem
				{
					TenDV = sd.TenDV ?? "Dịch vụ",
					SoLuong = 1,
					DonGia = sd.DonGia ?? 0,
					ThanhTien = sd.ThanhTien ?? (sd.DonGia ?? 0)
				}).ToList();

				// 3. Tính Phụ thu
				var tongTienPhong = listPhong.Sum(p => p.ThanhTien);
				var tongTienDichVu = listDichVu.Sum(d => d.ThanhTien);

				var phuThu = (x.TongTienGoc ?? 0) - tongTienPhong - tongTienDichVu;
				if (phuThu < 0) phuThu = 0;

				// Trả về DTO cuối cùng
				return new HoaDonDTO
				{
					MaHD = x.MaHd,
					NgayTao = x.NgayTao,
					TrangThai = x.ThanhToan?.TrangThai ?? x.TrangThaiDatPhong ?? "Không rõ",
					TenKhachHang = x.TenKhachHang ?? "Khách vãng lai",
					Sdt = x.SdtKhachHang ?? "",
					TenNhanVien = x.TenNhanVien ?? "Không rõ",
					HinhAnhNhanVien = x.HinhAnhNhanVien ?? "",
					ChucVuNhanVien = x.TenChucVu ?? "Không rõ",
					LoaiThanhToan = x.ThanhToan?.LoaiThanhToan,

					TongTienGoc = x.TongTienGoc ?? 0,
					SoTienGiam = x.SoTienGiam ?? 0,
					TongTienPhaiTra = x.TongTienPhaiTra ?? 0,

					PhuThu = phuThu,
					TienCoc = x.TienCoc,

					ChiTietPhong = listPhong,
					DichVu = listDichVu
				};
			});

			return Ok(result);
		}
	}
}