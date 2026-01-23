using System;
using System.Collections.Generic;

namespace Luxury.Models;

public partial class Chucvu
{
    public int MaChucVu { get; set; }

    public string? TenChucVu { get; set; }

    public decimal? Luong { get; set; }

    public string? Role { get; set; }

    public bool? IsDelete { get; set; }

    public virtual ICollection<Nhanvien> Nhanviens { get; set; } = new List<Nhanvien>();
}
