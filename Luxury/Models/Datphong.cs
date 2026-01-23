using System;
using System.Collections.Generic;

namespace Luxury.Models;

public partial class Datphong
{
    public int MaDatPhong { get; set; }

    public int? MaKh { get; set; }

    public int? MaNv { get; set; }

    public string? MaCode { get; set; }

    public DateTime? NgayDat { get; set; }

    public decimal? TongTienDuKien { get; set; }

    public string? TrangThai { get; set; }

    public virtual ICollection<Chitietdatphong> Chitietdatphongs { get; set; } = new List<Chitietdatphong>();

    public virtual ICollection<Danhgium> Danhgia { get; set; } = new List<Danhgium>();

    public virtual ICollection<Hoadon> Hoadons { get; set; } = new List<Hoadon>();

    public virtual Voucher? MaCodeNavigation { get; set; }

    public virtual Khachhang? MaKhNavigation { get; set; }

    public virtual Nhanvien? MaNvNavigation { get; set; }

    public virtual ICollection<Thanhtoan> Thanhtoans { get; set; } = new List<Thanhtoan>();
}
