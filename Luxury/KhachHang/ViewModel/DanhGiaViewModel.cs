namespace Luxury.KhachHang.ViewModel
{
    public class DanhGiaViewModel
	{
		public int MaDg { get; set; }
		public string TenKhachHang { get; set; }
		public string Avatar { get; set; }
		public int SoSao { get; set; }
		public string NoiDung { get; set; }
		public DateTime NgayDanhGia { get; set; }
        public string? PhanHoi { get; internal set; }
    }
}
