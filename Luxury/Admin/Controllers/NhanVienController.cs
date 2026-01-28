using Luxury.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Luxury.Admin.Models;

namespace Luxury.Admin.Controllers
{
    [ApiController]
    [Route("api/admin/[controller]")]
    public class NhanVienController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;
		private readonly IWebHostEnvironment _env;
		public NhanVienController(LuxuryHotelContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var nhanviens = await _context.Nhanviens.ToListAsync();
            return Ok(nhanviens);
        }

		// =============================================
		// 2. POST: Thêm mới
		// =============================================
		[HttpPost]
		public async Task<IActionResult> Create([FromForm] NhanVienDTO request)
		{
			// Kiểm tra trùng tên đăng nhập
			if (await _context.Nhanviens.AnyAsync(u => u.TenDangNhap == request.TenDangNhap))
			{
				return BadRequest(new { message = "Tên đăng nhập đã tồn tại!" });
			}

			// A. Khởi tạo Entity Nhanvien (Model gốc)
			var nv = new Nhanvien
			{
				HoTenNv = request.HoTenNv,
				MaChucVu = request.MaChucVu,
				TenDangNhap = request.TenDangNhap,
				MatKhau = BCrypt.Net.BCrypt.HashPassword(request.MatKhau), // (Nhớ mã hóa password nếu cần)
				TrangThai = request.TrangThai ?? "Hoạt động",
				Email = request.Email,
				DiaChi = request.DiaChi,
				GioiTinh = request.GioiTinh,
				NamSinh = request.NamSinh,
				// Nếu không chọn ngày thì lấy ngày hiện tại
				NgayVaoLam = request.NgayVaoLam ?? DateOnly.FromDateTime(DateTime.Now)
			};

			// B. Xử lý ảnh: IFormFile (DTO) -> String (Entity)
			if (request.HinhAnh != null)
			{
				// Hàm SaveFileToWwwRoot trả về TÊN FILE (String)
				// Lưu tên file này vào cột HinhAnh (String) của Entity
				nv.HinhAnh = await SaveFileToWwwRoot(request.HinhAnh);
			}

			_context.Nhanviens.Add(nv);
			await _context.SaveChangesAsync();

			return Ok(nv);
		}
		[HttpPut("{id}")]
		public async Task<IActionResult> Update(int id, [FromForm] NhanVienDTO request)
		{
			var nv = await _context.Nhanviens.FindAsync(id);
			if (nv == null) return NotFound(new { message = "Không tìm thấy nhân viên" });

			// =======================================================================
			// LOGIC BẢO VỆ ADMIN CUỐI CÙNG (Đã tối ưu)
			// =======================================================================

			// Bước 1: Kiểm tra xem nhân viên hiện tại có phải là Admin đang hoạt động không?
			// (Giả sử MaChucVu == 1 là Admin)
			bool isCurrentActiveAdmin = (nv.MaChucVu == 1 && nv.TrangThai == "Hoạt động");

			if (isCurrentActiveAdmin)
			{
				// Bước 2: Kiểm tra xem Request gửi lên có thay đổi nguy hiểm không?
				// Nguy hiểm là: Đổi sang chức vụ khác (Khác 1) HOẶC Đổi sang trạng thái khác (Khác Hoạt động)
				bool willLoseAdminRights = (request.MaChucVu != 1) || (request.TrangThai != "Hoạt động");

				if (willLoseAdminRights)
				{
					// Bước 3: Nếu có hành động nguy hiểm, mới đi đếm số lượng
					var activeAdminCount = await _context.Nhanviens
						.CountAsync(x => x.MaChucVu == 1 && x.TrangThai == "Hoạt động");

					if (activeAdminCount <= 1)
					{
						return BadRequest(new { message = "Không thể đổi chức vụ hoặc khóa tài khoản Admin cuối cùng đang hoạt động!" });
					}
				}
			}
			// =======================================================================

			// Cập nhật thông tin (Code cũ giữ nguyên)
			nv.HoTenNv = request.HoTenNv;
			nv.MaChucVu = request.MaChucVu; // Nếu qua được ải trên thì gán thoải mái
			nv.TrangThai = request.TrangThai; // Nếu qua được ải trên thì gán thoải mái
			nv.Email = request.Email;
			nv.DiaChi = request.DiaChi;
			nv.GioiTinh = request.GioiTinh;
			nv.NamSinh = request.NamSinh;

			if (request.NgayVaoLam.HasValue) nv.NgayVaoLam = request.NgayVaoLam;

			if (!string.IsNullOrEmpty(request.MatKhau))
			{
				nv.MatKhau = BCrypt.Net.BCrypt.HashPassword(request.MatKhau);
			}

			// Xử lý ảnh cập nhật
			if (request.HinhAnh != null)
			{
				if (!string.IsNullOrEmpty(nv.HinhAnh))
				{
					DeleteFileFromWwwRoot(nv.HinhAnh);
				}
				nv.HinhAnh = await SaveFileToWwwRoot(request.HinhAnh);
			}

			await _context.SaveChangesAsync();
			return Ok(nv);
		}

		// ==========================================
		// 4. DELETE: Xóa nhân viên
		// ==========================================
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			var nv = await _context.Nhanviens.FindAsync(id);
			if (nv == null) return NotFound(new { message = "Không tìm thấy nhân viên" });

			// =================================================================
			// LỚP BẢO VỆ 1: KHÔNG ĐƯỢC XÓA ADMIN HOẠT ĐỘNG CUỐI CÙNG
			// =================================================================
			// Giả sử MaChucVu == 1 là Admin
			if (nv.MaChucVu == 1 && nv.TrangThai == "Hoạt động")
			{
				// Đếm xem còn bao nhiêu ông Admin đang sống
				var activeAdminCount = await _context.Nhanviens
					.CountAsync(x => x.MaChucVu == 1 && x.TrangThai == "Hoạt động");

				if (activeAdminCount <= 1)
				{
					return BadRequest(new { message = "Không thể xóa tài khoản Quản trị viên (Admin) cuối cùng đang hoạt động!" });
				}
			}

			// =================================================================
			// LỚP BẢO VỆ 2: KIỂM TRA RÀNG BUỘC DỮ LIỆU (SOFT DELETE)
			// =================================================================
			// Kiểm tra xem nhân viên này đã từng lập đơn đặt phòng nào chưa
			bool hasTransaction = await _context.Datphongs.AnyAsync(dp => dp.MaNv == id);

			if (hasTransaction)
			{
				// === PA1: SOFT DELETE (XÓA MỀM) ===
				// Thay vì xóa, ta chuyển sang trạng thái "Đã nghỉ việc"
				nv.TrangThai = "Đã nghỉ việc";

				// (Optional) Xóa mật khẩu hoặc đổi chuỗi ngẫu nhiên để họ không login được nữa
				nv.MatKhau = null;

				await _context.SaveChangesAsync();

				return Ok(new { message = "Nhân viên này đã có dữ liệu giao dịch. Hệ thống đã chuyển trạng thái sang 'Đã nghỉ việc' để bảo toàn lịch sử." });
			}

			// =================================================================
			// LỚP BẢO VỆ 3: XÓA VĨNH VIỄN (HARD DELETE)
			// =================================================================
			// Chỉ chạy xuống đây nếu nhân viên "trắng tinh" (chưa làm gì cả)

			// 1. Xóa ảnh trong folder wwwroot (dọn rác server)
			if (!string.IsNullOrEmpty(nv.HinhAnh))
			{
				DeleteFileFromWwwRoot(nv.HinhAnh);
			}

			// 2. Xóa row trong Database
			_context.Nhanviens.Remove(nv);
			await _context.SaveChangesAsync();

			return Ok(new { message = "Đã xóa vĩnh viễn hồ sơ nhân viên." });
		}

		// ==========================================
		// 5. Khóa / Mở khóa nhân viên
		// ==========================================
		[HttpPut("toggle-status/{id}")]
		public async Task<IActionResult> ToggleStatus(int id)
		{
			// 1. Tìm nhân viên cần thao tác trước (Clean Code: Fail fast)
			var nv = await _context.Nhanviens.FindAsync(id);
			if (nv == null) return NotFound(new { message = "Không tìm thấy nhân viên" });

			// 2. LOGIC KIỂM TRA ADMIN CUỐI CÙNG
			// Chỉ cần kiểm tra khi:
			// - Người đó là Admin (MaChucVu == 1)
			// - VÀ Hành động hiện tại là KHÓA (Trạng thái đang là "Hoạt động")
			// (Nếu đang Mở khóa thì không cần kiểm tra gì cả, cho qua luôn)
			if (nv.MaChucVu == 1 && nv.TrangThai == "Hoạt động")
			{
				// Đếm trực tiếp trong DB, không cần lôi cả list về (Tối ưu hiệu năng)
				var activeAdminCount = await _context.Nhanviens
					.CountAsync(x => x.MaChucVu == 1 && x.TrangThai == "Hoạt động");

				// Nếu số lượng <= 1 thì đó chính là ông này -> Chặn
				if (activeAdminCount <= 1)
				{
					return BadRequest(new { message = "Không thể khoá tài khoản Admin cuối cùng đang hoạt động." });
				}
			}

			// 3. Thực hiện đổi trạng thái (Toggle)
			nv.TrangThai = nv.TrangThai == "Hoạt động" ? "Bị khoá" : "Hoạt động";

			// Lưu và trả về
			await _context.SaveChangesAsync();
			return Ok(nv);
		}

		// ==========================================
		// HÀM PHỤ TRỢ (Lưu/Xóa File Vật Lý)
		// ==========================================

		// Hàm này nhận file, lưu vào ổ cứng, trả về STRING tên file
		private async Task<string> SaveFileToWwwRoot(IFormFile file)
		{
			var originalName = Path.GetFileName(file.FileName);
			var uniqueFileName = $"{Guid.NewGuid()}_{originalName}";

			// --- SỬA ĐOẠN NÀY ---
			// Kiểm tra nếu WebRootPath bị null thì dùng ContentRootPath + "wwwroot"
			string rootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");

			// Đường dẫn lưu: wwwroot/images/NhanVien
			var uploadsFolder = Path.Combine(rootPath, "images", "NhanVien");
			// --------------------

			// Tự tạo thư mục nếu chưa có
			if (!Directory.Exists(uploadsFolder))
			{
				Directory.CreateDirectory(uploadsFolder);
			}

			var filePath = Path.Combine(uploadsFolder, uniqueFileName);

			using (var stream = new FileStream(filePath, FileMode.Create))
			{
				await file.CopyToAsync(stream);
			}

			return uniqueFileName;
		}

		private void DeleteFileFromWwwRoot(string fileName)
		{
			try
			{
				var filePath = Path.Combine(_env.WebRootPath, "images", "NhanVien", fileName);
				if (System.IO.File.Exists(filePath))
				{
					System.IO.File.Delete(filePath);
				}
			}
			catch { /* Bỏ qua lỗi xóa file */ }
		}
	}
}
