namespace Luxury.DTO.OrderDichVu
{
    public class orderDichVuResponseDTO
    {
        public string TenLoai { get;set; }
        public string TenBienThe { get; set; }
        public string SoPhong { get; set; }
        public int MaCtdp { get; set; }
        public string TenKhach { get; set; }
        public decimal TongTienDichVu { get; set; }

        public List<DichVuPhongItemDTO> DichVus { get; set; } = new();
       
    }

    public class DichVuPhongItemDTO
    {
        public int MaSd { get; set; }
        public string TenDichVu { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien { get; set; }
        public DateTime NgaySuDung { get; set; }
        public string ThoiGianDaDat
        {
            get
            {
                var timeSpan = DateTime.Now - NgaySuDung;

                if (timeSpan.TotalMinutes < 1)
                    return "Vừa xong";

                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes} phút trước";

                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours} giờ trước";

                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays} ngày trước";

                return NgaySuDung.ToString("dd/MM/yyyy HH:mm");
            }
        }
        public int TrangThai { get; set; } 
    }
}
