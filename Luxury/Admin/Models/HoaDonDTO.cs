namespace Luxury.Admin.Models
{
	public class HoaDonDTO
	{
		public int MaHD { get; set; }
		public DateTime? NgayTao { get; set; }
		public string TrangThai { get; set; }

		// Thông tin chung
		public string TenKhachHang { get; set; }
		public string Sdt { get; set; } // Đổi thành Sdt cho khớp mẫu của bạn
		public string TenNhanVien { get; set; }

		public string HinhAnhNhanVien { get; set; } = string.Empty;

		public string ChucVuNhanVien { get; set; } = string.Empty;
		public string LoaiThanhToan { get; set; }

		// TIỀN BẠC (Khớp với DB Hoadon)
		public decimal TongTienGoc { get; set; }
		public decimal SoTienGiam { get; set; }
		public decimal TongTienPhaiTra { get; set; }

		public decimal PhuThu { get; set; }
		public decimal TienGiamVoucher { get; set; }
		public decimal TienGiamFlashSale { get; set; }

		public decimal TienCoc { get; set; }

		// DANH SÁCH 1: CHI TIẾT PHÒNG
		public List<ChiTietPhongItem> ChiTietPhong { get; set; }

		// DANH SÁCH 2: DỊCH VỤ
		public List<ChiTietDichVuItem> DichVu { get; set; }
	}

	public class ChiTietPhongItem
	{
		public string SoPhong { get; set; }
		public string LoaiPhong { get; set; }
		public int SoDem { get; set; }
		public decimal DonGia { get; set; }
		public decimal ThanhTien { get; set; }
	}

	public class ChiTietDichVuItem
	{
		public string TenDV { get; set; }
		public int SoLuong { get; set; }
		public decimal DonGia { get; set; }
		public decimal ThanhTien { get; set; }
	}
}
