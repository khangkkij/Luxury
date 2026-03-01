namespace Luxury.DTO.DichVu
{
    public class dichVuRequestDTO
    {
        public string? TenDichVu { get; set; }
        public decimal? Gia { get; set; }
        public IFormFile? upLoadImage { get; set; }
        public bool? TrangThai { get; set; }
    }
}
