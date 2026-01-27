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
				TrangThai = request.TrangThai ?? "Đang làm việc",
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
		// ==========================================
		// 3. PUT: Cập nhật nhân viên
		// ==========================================
		[HttpPut("{id}")]
		public async Task<IActionResult> Update(int id, [FromForm] NhanVienDTO request)
		{
			var nv = await _context.Nhanviens.FindAsync(id);
			if (nv == null) return NotFound(new { message = "Không tìm thấy nhân viên" });

			// Cập nhật thông tin cơ bản
			nv.HoTenNv = request.HoTenNv;
			nv.MaChucVu = request.MaChucVu;
			nv.TrangThai = request.TrangThai;
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
				// 1. Xóa ảnh cũ trên server (nếu có)
				if (!string.IsNullOrEmpty(nv.HinhAnh))
				{
					DeleteFileFromWwwRoot(nv.HinhAnh);
				}

				// 2. Lưu ảnh mới vào server và lấy tên file gán vào DB
				nv.HinhAnh = await SaveFileToWwwRoot(request.HinhAnh);
			}
			// Lưu ý: Nếu request.HinhAnh == null (người dùng không chọn ảnh mới)
			// thì nv.HinhAnh vẫn giữ nguyên giá trị string cũ trong Database.

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
			if (nv == null) return NotFound();

			// Xóa file ảnh vật lý trong wwwroot/images/staff
			if (!string.IsNullOrEmpty(nv.HinhAnh))
			{
				DeleteFileFromWwwRoot(nv.HinhAnh);
			}

			_context.Nhanviens.Remove(nv);
			await _context.SaveChangesAsync();

			return Ok(new { message = "Đã xóa thành công" });
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
