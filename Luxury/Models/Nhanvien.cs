using System;
using System.Collections.Generic;

namespace Luxury.Models;

public partial class Nhanvien
{
    public int MaNv { get; set; }

    public int? MaChucVu { get; set; }

    public string? HoTenNv { get; set; }

    public string? GioiTinh { get; set; }

    public int? NamSinh { get; set; }

    public string? Email { get; set; }

    public string? DiaChi { get; set; }

    public string? HinhAnh { get; set; }

    public string? TenDangNhap { get; set; }

    public string? MatKhau { get; set; }

    public DateOnly? NgayVaoLam { get; set; }

    public string? TrangThai { get; set; }

    public virtual ICollection<Datphong> Datphongs { get; set; } = new List<Datphong>();

    public virtual Chucvu? MaChucVuNavigation { get; set; }
}
