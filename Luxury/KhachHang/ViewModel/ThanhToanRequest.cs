namespace Luxury.KhachHang.ViewModel
{
    public class ThanhToanRequest
    {
        public int? MaDatPhong { get; set; }
        public int? MaHd { get; set; }
        public decimal SoTien { get; set; } // Đây là số tiền khách phải trả sau khi đã giảm (nếu có)
        public string? HinhThucThanhToan { get; set; }
        public string? MaCode { get; set; } // Thêm thuộc tính này
    }
}