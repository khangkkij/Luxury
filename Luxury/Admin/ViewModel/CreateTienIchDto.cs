namespace Luxury.Admin.ViewModel
{
    public class CreateTienIchDto
    {
        public string? TenTienIch { get; set; }

        public string? LoaiTienIch { get; set; }

        public IFormFile? Icon { get; set; }

        public bool? TrangThai { get; set; }
    }
}
