using System.ComponentModel.DataAnnotations;

namespace Luxury.DTO.Sale
{
    public class SaleResponseDTOcs
    {
        public int Flashsaleid { get; set; }
        public DateTime? NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public string? TongSoPhong { get; set; }
        public bool? TrangThai { get; set; }
        public List<SaleDetailResponseDTO> ChiTietSales { get; set; } = new List<SaleDetailResponseDTO>();
    }
    public class SaleDetailResponseDTO
    {
        public int? PhanTramGiam { get; set; }
        public string? TenLoai { get; set; }
        public string? TenBienThe { get; set; }
     
        public decimal? GiaGoc { get; set; }    
        public decimal? GiaSauGiam { get; set; }
    }
}
