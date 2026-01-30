using System;

namespace Luxury.KhachHang.ViewModel
{
    public class DatPhongRequest
    {
        public int? MaKh { get; set; }
        public int MaBienThePhong { get; set; }
        public DateTime NgayNhan { get; set; }
        public DateTime NgayTra { get; set; }
        public decimal GiaDat { get; set; }
        public int SoLuong { get; set; } = 1;
    }
}
