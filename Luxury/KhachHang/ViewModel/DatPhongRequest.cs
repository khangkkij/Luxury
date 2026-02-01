using System;
using System.Collections.Generic;

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
        public int? SoNguoiLon { get; set; }
        public int? SoTreEm { get; set; }
        public List<DatPhongItemRequest>? Items { get; set; }
    }

    public class DatPhongItemRequest
    {
        public int MaBienThePhong { get; set; }
        public decimal GiaDat { get; set; }
        public int SoLuong { get; set; } = 1;
    }
}
