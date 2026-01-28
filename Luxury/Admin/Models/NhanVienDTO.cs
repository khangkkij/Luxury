namespace Luxury.Admin.Models
{
    public class NhanVienDTO
    {
		// Các trường này map với Form nhập liệu bên Vue
		public string HoTenNv { get; set; } = null!;
		public int? MaChucVu { get; set; }
		public string? TenDangNhap { get; set; }
		public string? MatKhau { get; set; } // Chỉ gửi lên khi tạo mới hoặc đổi pass
		public string? TrangThai { get; set; }
		public string? Email { get; set; }
		public string? DiaChi { get; set; }
		public string? GioiTinh { get; set; }
		public int? NamSinh { get; set; }
		public DateOnly? NgayVaoLam { get; set; }

		// QUAN TRỌNG: Bên Vue gửi file, nên ở đây phải hứng bằng IFormFile
		// Controller sẽ chuyển nó thành string tên file để lưu vào Entity sau
		public IFormFile? HinhAnh { get; set; }
	}
}
