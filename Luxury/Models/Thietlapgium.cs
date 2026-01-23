using System;
using System.Collections.Generic;

namespace Luxury.Models;

public partial class Thietlapgium
{
    public int MaThietLap { get; set; }

    public int? MaBienThePhong { get; set; }

    public decimal? Gia { get; set; }

    public DateTime? NgayBatDau { get; set; }

    public DateTime? NgayKetThuc { get; set; }

    public string? LoaiGia { get; set; }

    public string? MoTa { get; set; }

    public bool? MacDinh { get; set; }

    public virtual LoaiphongBienthe? MaBienThePhongNavigation { get; set; }
}
