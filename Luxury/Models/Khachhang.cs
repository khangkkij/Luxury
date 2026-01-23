using System;
using System.Collections.Generic;

namespace Luxury.Models;

public partial class Khachhang
{
    public int MaKh { get; set; }

    public string? HoVaTen { get; set; }

    public string? Email { get; set; }

    public string? Cccd { get; set; }

    public string? Sdt { get; set; }

    public string? GioiTinh { get; set; }

    public string? MatKhau { get; set; }

    public DateTime? NgayTao { get; set; }

    public string? HinhAnh { get; set; }

    public bool? TrangThai { get; set; }

    public virtual ICollection<Datphong> Datphongs { get; set; } = new List<Datphong>();

    public virtual ICollection<Voucher> MaCodes { get; set; } = new List<Voucher>();
}
