using System;
using System.Collections.Generic;

namespace Luxury.Models;

public partial class Phong
{
    public int MaPhong { get; set; }

    public int? MaBienThePhong { get; set; }

    public string? SoPhong { get; set; }

    public int? TrangThai { get; set; }

    public virtual ICollection<Chitietdatphong> Chitietdatphongs { get; set; } = new List<Chitietdatphong>();

    public virtual ICollection<Danhgium> Danhgia { get; set; } = new List<Danhgium>();

    public virtual LoaiphongBienthe? MaBienThePhongNavigation { get; set; }
}
