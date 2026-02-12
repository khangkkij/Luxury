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
        public IActionResult GetAll ()
        {
            var sanpham = _context.Phongs.ToList();
			return Ok(sanpham);
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

				// Nếu có giảm giá FlashSale
				if (item.FlashSaleActive > 0)
				{
					giaBan = giaGoc * (1 - (decimal)item.FlashSaleActive / 100);
				}

				return new BestDealViewModel
				{
					MaLp = item.BienThe.MaBienThePhong, // Frontend dùng ID này để booking
														// Tên hiển thị: Ưu tiên tên biến thể, nếu null lấy tên loại
					TenLoai = !string.IsNullOrEmpty(item.BienThe.TenBienThe)
							  ? item.BienThe.TenBienThe
							  : item.BienThe.MaLpNavigation.TenLoai,

					DienTich = item.BienThe.DienTich,
					HuongNhin = item.BienThe.HuongNhin,
					LoaiGiuong = item.BienThe.LoaiGiuong,

					Gia = giaBan,       // Giá sau khi giảm (nếu có)
					GiaGoc = giaGoc,    // Giá gốc để so sánh

					HinhAnh = item.AnhDaiDien,
					MoTa = item.BienThe.MaLpNavigation.MoTa // Lấy mô tả từ bảng cha LOAIPHONG
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
