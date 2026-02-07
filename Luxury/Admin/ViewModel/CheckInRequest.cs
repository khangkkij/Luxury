namespace Luxury.Admin.ViewModel
{
    public class CheckInRequest
    {
        public int MaDatPhong { get; set; }
        public int MaPhong { get; set; }
        public int? MaNv { get; set; }
        public int? MaCtdp { get; set; }
        public decimal? DepositAmount { get; set; }
        public string? DepositMethod { get; set; }
    }
}
