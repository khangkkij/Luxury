using System;
using System.Collections.Generic;

namespace Luxury.Models;

public partial class Chitietdatphong
{
    public int MaCtdp { get; set; }

    public int? MaDatPhong { get; set; }

    public int? MaPhong { get; set; }

    public decimal? GiaDat { get; set; }

    public DateTime? NgayNhan { get; set; }

    public DateTime? NgayTra { get; set; }

    public int? SoNguoiLon { get; set; }

    public int? SoTreEm { get; set; }

    public bool? TrangThai { get; set; }

    public virtual Datphong? MaDatPhongNavigation { get; set; }

    public virtual Phong? MaPhongNavigation { get; set; }

    public virtual ICollection<SudungDichvu> SudungDichvus { get; set; } = new List<SudungDichvu>();
}
