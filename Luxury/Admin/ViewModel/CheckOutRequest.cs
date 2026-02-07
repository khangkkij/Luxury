using System.Collections.Generic;

namespace Luxury.Admin.ViewModel
{
    public class CheckOutRequest
    {
        public int MaDatPhong { get; set; }
        public int? MaPhong { get; set; }
        public int? MaCtdp { get; set; }
        public int? MaNv { get; set; }
        public decimal? ExtraFee { get; set; }
        public string? VoucherCode { get; set; }
        public string? HinhThucThanhToan { get; set; }
        public DateTime? ThoiGianTraThucTe { get; set; }
        public List<ServiceUsageRequest>? ExtraServices { get; set; }
    }

    public class ServiceUsageRequest
    {
        public int MaDichVu { get; set; }
        public int SoLuong { get; set; }
    }
}
