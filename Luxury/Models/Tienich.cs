using System;
using System.Collections.Generic;

namespace Luxury.Models;

public partial class Tienich
{
    public int MaTi { get; set; }

    public string? TenTienIch { get; set; }

    public string? LoaiTienIch { get; set; }

    public string? Icon { get; set; }

    public bool? TrangThai { get; set; }

    public virtual ICollection<LoaiphongBienthe> MaBienThePhongs { get; set; } = new List<LoaiphongBienthe>();
}
