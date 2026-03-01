using Luxury.Models;
using System.ComponentModel.DataAnnotations;

namespace Luxury.DTO.Sale
{
    public class SaleRequestDTO
    {
        public DateTime? NgayBatDau { get; set; } = DateTime.Now;

        public DateTime? NgayKetThuc { get; set; }

        public bool? TrangThai { get; set; } = true;

        public List<SaleDetailsDTO> SaleDetails { get; set; } = new List<SaleDetailsDTO>();
    }
    public class SaleDetailsDTO
    {
        public int Flashsaleid { get; set; }
        public int MaBienThePhong { get; set; }
        [Range(0, 100)]
        public int? PhanTramGiam { get; set; }
    }
}
