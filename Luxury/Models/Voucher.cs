using System;
using System.Collections.Generic;

namespace Luxury.Models;

public partial class Voucher
{
    public string MaCode { get; set; } = null!;

    public string? MoTa { get; set; }

    public int? PhanTramGiam { get; set; }

    public decimal? SoTienGiam { get; set; }

    public decimal? GiaToiThieu { get; set; }

    public DateTime? NgayBatDau { get; set; }

    public DateTime? NgayKetThuc { get; set; }

    public int? SoLuong { get; set; }

    public int? SoLuongDaDung { get; set; }

    public bool? TrangThai { get; set; }

    public virtual ICollection<Datphong> Datphongs { get; set; } = new List<Datphong>();

    public virtual ICollection<Khachhang> MaKhs { get; set; } = new List<Khachhang>();
}
