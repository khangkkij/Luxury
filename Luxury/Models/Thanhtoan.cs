using System;
using System.Collections.Generic;

namespace Luxury.Models;

public partial class Thanhtoan
{
    public int MaTt { get; set; }

    public int? MaDatPhong { get; set; }

    public int? MaHd { get; set; }

    public string? LoaiThanhToan { get; set; }

    public decimal? SoTien { get; set; }

    public string? HinhThucThanhToan { get; set; }

    public string? TrangThai { get; set; }

    public virtual Datphong? MaDatPhongNavigation { get; set; }

    public virtual Hoadon? MaHdNavigation { get; set; }
}
