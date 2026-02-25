namespace Luxury.Admin.ViewModel
{
    public class PriceRangeDto
    {
        public int MaThietLapGia { get; set; }
        public int MaBienThePhong {  get; set; }
        public string LoaiGia { get; set; }
        public string MoTa { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public decimal Gia { get; set; }
        public bool MacDinh {  get; set; }
    }
}
