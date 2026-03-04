using Luxury.KhachHang.ViewModel;
using Luxury.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Luxury.KhachHang.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class HomeController : Controller
    {
        private readonly LuxuryHotelContext _context;
        public HomeController(LuxuryHotelContext context)
        {
            _context = context;
		}
		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			var data = await _context.LoaiphongBienthes
				.Include(bt => bt.MaLpNavigation)
				.Include(bt => bt.Hinhanhs)
				.Include(bt => bt.Thietlapgia)
				.Include(bt => bt.Phongs) // Bắt buộc Include bảng Phongs để lấy số tầng
				.Select(bt => new
				{
					MaBienThePhong = bt.MaBienThePhong,
					TenBienThe = bt.TenBienThe,
					TenLoai = bt.MaLpNavigation != null ? bt.MaLpNavigation.TenLoai : "N/A",
					SoNguoiToiDa = (bt.SoNguoiLon ?? 0) + (bt.SoTreEm ?? 0),
					DienTich = bt.DienTich,
					AnhDaiDien = bt.Hinhanhs.FirstOrDefault() != null ? bt.Hinhanhs.FirstOrDefault().DuongDan : "",
					GiaGoc = bt.Thietlapgia.Where(g => g.MacDinh == true).Select(g => g.Gia).FirstOrDefault(),
				})
				.Take(3)
				.ToListAsync();

			return Ok(data);
		}
		[Route("/api/BestDeals")]
		[HttpGet]
		public async Task<ActionResult<IEnumerable<BestDealViewModel>>> GetBestDeals()
		{
			var today = DateTime.Now;

			// 1. Lấy danh sách các biến thể phòng
			// Kèm theo: Loại phòng (lấy tên, mô tả), Hình ảnh, Giá, FlashSale
			var query = _context.LoaiphongBienthes
				.Include(x => x.MaLpNavigation) // Để lấy Mô tả chung
				.Include(x => x.Hinhanhs)
				.Include(x => x.Thietlapgia)
				.Include(x => x.ChitietFlashsales)
					.ThenInclude(ct => ct.Flashsale)
				.AsQueryable();

			// 2. Logic lọc "Ưu đãi tốt nhất":
			// Ở đây mình ưu tiên lấy những phòng có tham gia FlashSale đang hoạt động
			// Hoặc bạn có thể chỉ lấy Top 5 phòng ngẫu nhiên nếu không có sale.

			var rawData = await query.Select(x => new
			{
				BienThe = x,
				// Lấy ảnh đầu tiên
				AnhDaiDien = x.Hinhanhs.FirstOrDefault().DuongDan,

				// Tìm giá đang áp dụng (ưu tiên giá theo ngày -> giá mặc định)
				GiaHienTai = x.Thietlapgia
					.Where(g => (g.NgayBatDau <= today && g.NgayKetThuc >= today) || g.MacDinh == true)
					.OrderByDescending(g => g.NgayBatDau) // Ưu tiên giá set gần nhất
					.Select(g => g.Gia)
					.FirstOrDefault(),

				// Kiểm tra xem có FlashSale nào đang active không
				FlashSaleActive = x.ChitietFlashsales
					.Where(fs => fs.Flashsale.TrangThai == true
							  && fs.Flashsale.NgayBatDau <= today
							  && fs.Flashsale.NgayKetThuc >= today)
					.Select(fs => fs.PhanTramGiam)
					.FirstOrDefault()
			}).ToListAsync();

			// 3. Xử lý dữ liệu sau khi lấy từ DB (Tính toán giá cuối cùng)
			var result = rawData.Select(item =>
			{
				decimal giaGoc = item.GiaHienTai ?? 0;
				decimal giaBan = giaGoc;

				if (item.FlashSaleActive > 0)
				{
					giaBan = giaGoc * (1 - (decimal)item.FlashSaleActive / 100);
				}

				return new BestDealViewModel
				{
					MaLp = item.BienThe.MaBienThePhong,
					TenLoai = !string.IsNullOrEmpty(item.BienThe.TenBienThe)
							  ? item.BienThe.TenBienThe
							  : item.BienThe.MaLpNavigation.TenLoai,
					Gia = giaBan,
					GiaGoc = giaGoc,
					// BẮT BUỘC THÊM DÒNG NÀY ĐỂ GỬI PHẦN TRĂM VỀ FRONTEND
					PhanTramGiam = item.FlashSaleActive.ToString(),
					HinhAnh = item.AnhDaiDien,
					MoTa = item.BienThe.MaLpNavigation.MoTa
				};
			})
			// Sắp xếp: Ưu tiên phòng có giảm giá lên đầu, sau đó đến giá thấp nhất
			.OrderByDescending(x => x.Gia < x.GiaGoc)
			.ThenBy(x => x.Gia)
			.Take(6) // Chỉ lấy 6 phòng tốt nhất
			.ToList();

			if (!result.Any())
			{
				return NotFound("Hiện không có phòng nào khả dụng.");
			}

			return Ok(result);
		}
	}
}
