using System;
using System.Collections.Generic;

namespace Luxury.Models;

public partial class LoaiphongBienthe
{
    public int MaBienThePhong { get; set; }

    public int? MaLp { get; set; }

    public string? TenBienThe { get; set; }

    public string? LoaiGiuong { get; set; }

    public string? HuongNhin { get; set; }

    public int? SoNguoiToiDa { get; set; }

    public decimal? DienTich { get; set; }

    public virtual ICollection<ChitietFlashsale> ChitietFlashsales { get; set; } = new List<ChitietFlashsale>();

    public virtual ICollection<Hinhanh> Hinhanhs { get; set; } = new List<Hinhanh>();

    public virtual Loaiphong? MaLpNavigation { get; set; }

    public virtual ICollection<Phong> Phongs { get; set; } = new List<Phong>();

    public virtual ICollection<Thietlapgium> Thietlapgia { get; set; } = new List<Thietlapgium>();

    public virtual ICollection<Tienich> MaTis { get; set; } = new List<Tienich>();
}
