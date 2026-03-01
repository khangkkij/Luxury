using System.ComponentModel.DataAnnotations;

namespace Luxury.DTO.OrderDichVu
{
    public class OrderDichVuRequestDTO
    {
        public int? MaPhong { get; set; }
        public int? MaCtdp { get; set; }

        public List<OrderDichVuItemDTO> DichVus { get; set; } = new();
    }

    public class OrderDichVuItemDTO
    {
        public int MaDichVu { get; set; }
        public int SoLuong { get; set; }
    }

}

