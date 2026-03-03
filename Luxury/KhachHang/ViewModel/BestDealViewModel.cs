using System.Reflection.Metadata;

namespace Luxury.KhachHang.ViewModel
{
	public class BestDealViewModel
	{
		public int MaLp { get; set; } // Thực tế map với MaBienThePhong
		public string TenLoai { get; set; }
		public decimal? DienTich { get; set; }
		public string HuongNhin { get; set; }
		public string LoaiGiuong { get; set; }
		public decimal Gia { get; set; }
		public decimal? GiaGoc { get; set; } // Để hiển thị gạch ngang nếu cần
		public string HinhAnh { get; set; }
		public string MoTa { get; set; }

		public string PhanTramGiam { get; set; } // Thêm trường này để hiển thị % giảm giá nếu có

	}
}
