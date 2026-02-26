namespace Luxury.Admin.ViewModel
{
    public class CreateBienTheDto
    {
        public int MaLp { get; set; }
        public string? TenBienThe { get; set; }
        public string? LoaiGiuong { get; set; }
        public string? HuongNhin { get; set; }
        public int SoNguoiLon { get; set; }
        public int SoTreEm { get; set; }
        public decimal DienTich { get; set; }
        public decimal GiaNiemYet { get; set; }
        public decimal GiaCuoiTuan { get; set; }
        public List<string> RemovedImages { get; set; }
        public List<int> TienIchIds { get; set; } = new();
        public List<IFormFile> Images { get; set; } = new();
        public List<string>? ImageUrls { get; set; }
        public List<PriceRangeDto> PriceRanges { get; set; }
    }
}
