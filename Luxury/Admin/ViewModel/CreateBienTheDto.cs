namespace Luxury.Admin.ViewModel
{
    public class CreateBienTheDto
    {
        public int MaLp { get; set; }
        public string? TenBienThe { get; set; }
        public string? LoaiGiuong { get; set; }
        public string? HuongNhin { get; set; }
        public int SoNguoiToiDa { get; set; }
        public decimal DienTich { get; set; }
        public decimal GiaNiemYet { get; set; }

        public List<int> TienIchIds { get; set; } = new();
        public List<IFormFile> Images { get; set; } = new();
        public List<string>? ImageUrls { get; set; }
    }


}
