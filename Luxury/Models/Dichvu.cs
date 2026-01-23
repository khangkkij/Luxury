using System;
using System.Collections.Generic;

namespace Luxury.Models;

public partial class Dichvu
{
    public int MaDichVu { get; set; }

    public string? TenDichVu { get; set; }

    public decimal? Gia { get; set; }

    public string? HinhAnh { get; set; }

    public bool? TrangThai { get; set; }

    public virtual ICollection<SudungDichvu> SudungDichvus { get; set; } = new List<SudungDichvu>();
}
