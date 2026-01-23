using System;
using System.Collections.Generic;

namespace Luxury.Models;

public partial class Hinhanh
{
    public int MaHa { get; set; }

    public int? MaBienThePhong { get; set; }

    public string? DuongDan { get; set; }

    public virtual LoaiphongBienthe? MaBienThePhongNavigation { get; set; }
}
