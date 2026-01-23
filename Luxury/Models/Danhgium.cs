using System;
using System.Collections.Generic;

namespace Luxury.Models;

public partial class Danhgium
{
    public int MaDg { get; set; }

    public int? MaDatPhong { get; set; }

    public int? MaPhong { get; set; }

    public int? SoSao { get; set; }

    public DateTime? NgayDanhGia { get; set; }

    public string? NoiDung { get; set; }

    public string? PhanHoi { get; set; }

    public DateTime? NgayPhanHoi { get; set; }

    public string? HinhAnh { get; set; }

    public int? TrangThai { get; set; }

    public virtual Datphong? MaDatPhongNavigation { get; set; }

    public virtual Phong? MaPhongNavigation { get; set; }
}
