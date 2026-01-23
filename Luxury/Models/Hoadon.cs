using System;
using System.Collections.Generic;

namespace Luxury.Models;

public partial class Hoadon
{
    public int MaHd { get; set; }

    public int? MaDatPhong { get; set; }

    public decimal? TongTienGoc { get; set; }

    public decimal? SoTienGiam { get; set; }

    public decimal? TongTienPhaiTra { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual Datphong? MaDatPhongNavigation { get; set; }

    public virtual ICollection<Thanhtoan> Thanhtoans { get; set; } = new List<Thanhtoan>();
}
