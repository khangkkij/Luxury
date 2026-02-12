using System;

namespace Luxury.Admin.ViewModel
{
    public class WalkInRequest
    {
        public string HoVaTen { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Cccd { get; set; }
        public string? Sdt { get; set; }
        public string? GioiTinh { get; set; }
        public int MaPhong { get; set; }
        public DateTime NgayNhan { get; set; }
        public DateTime NgayTra { get; set; }
        public int? MaNv { get; set; }
        public decimal? DepositAmount { get; set; }
        public string? DepositMethod { get; set; }
        public string? GhiChu { get; set; }
    }
}
