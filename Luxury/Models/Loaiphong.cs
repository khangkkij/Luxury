using System;
using System.Collections.Generic;

namespace Luxury.Models;

public partial class Loaiphong
{
    public int MaLp { get; set; }

    public string? TenLoai { get; set; }

    public string? MoTa { get; set; }

    public virtual ICollection<LoaiphongBienthe> LoaiphongBienthes { get; set; } = new List<LoaiphongBienthe>();
}
