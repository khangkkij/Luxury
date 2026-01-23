using System;
using System.Collections.Generic;

namespace Luxury.Models;

public partial class SudungDichvu
{
    public int MaSd { get; set; }

    public int? MaDichVu { get; set; }

    public int? MaCtdp { get; set; }

    public int? SoLuong { get; set; }

    public DateTime? NgaySuDung { get; set; }

    public decimal? ThanhTien { get; set; }

    public virtual Chitietdatphong? MaCtdpNavigation { get; set; }

    public virtual Dichvu? MaDichVuNavigation { get; set; }
}
