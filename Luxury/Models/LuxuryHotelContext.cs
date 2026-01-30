using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Luxury.Models;

public partial class LuxuryHotelContext : DbContext
{
    public LuxuryHotelContext()
    {
    }

    public LuxuryHotelContext(DbContextOptions<LuxuryHotelContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChitietFlashsale> ChitietFlashsales { get; set; }

    public virtual DbSet<Chitietdatphong> Chitietdatphongs { get; set; }

    public virtual DbSet<Chucvu> Chucvus { get; set; }

    public virtual DbSet<Danhgium> Danhgia { get; set; }

    public virtual DbSet<Datphong> Datphongs { get; set; }

    public virtual DbSet<Dichvu> Dichvus { get; set; }

    public virtual DbSet<Flashsale> Flashsales { get; set; }

    public virtual DbSet<Hinhanh> Hinhanhs { get; set; }

    public virtual DbSet<Hoadon> Hoadons { get; set; }

    public virtual DbSet<Khachhang> Khachhangs { get; set; }

    public virtual DbSet<Loaiphong> Loaiphongs { get; set; }

    public virtual DbSet<LoaiphongBienthe> LoaiphongBienthes { get; set; }

    public virtual DbSet<Nhanvien> Nhanviens { get; set; }

    public virtual DbSet<Phong> Phongs { get; set; }

    public virtual DbSet<Refeshtoken> Refeshtokens { get; set; }

    public virtual DbSet<SudungDichvu> SudungDichvus { get; set; }

    public virtual DbSet<Thanhtoan> Thanhtoans { get; set; }

    public virtual DbSet<Thietlapgium> Thietlapgia { get; set; }

    public virtual DbSet<Tienich> Tieniches { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public DbSet<TienichLoaiphong> TienichLoaiphongs { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChitietFlashsale>(entity =>
        {
            entity.HasKey(e => new { e.Flashsaleid, e.MaBienThePhong }).HasName("PK__CHITIET___51C1090F7689105C");

            entity.ToTable("CHITIET_FLASHSALE");

            entity.Property(e => e.Flashsaleid).HasColumnName("FLASHSALEID");
            entity.Property(e => e.PhanTramGiam).HasColumnName("phanTramGiam");

            entity.HasOne(d => d.Flashsale).WithMany(p => p.ChitietFlashsales)
                .HasForeignKey(d => d.Flashsaleid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CHITIET_F__FLASH__6E01572D");

            entity.HasOne(d => d.MaBienThePhongNavigation).WithMany(p => p.ChitietFlashsales)
                .HasForeignKey(d => d.MaBienThePhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CHITIET_F__MaBie__6EF57B66");
        });

        modelBuilder.Entity<TienichLoaiphong>(entity =>
        {
            entity.HasKey(e => new { e.MaTi, e.MaBienThePhong });

            entity.ToTable("TIENICH_LOAIPHONG");

            entity.HasOne(d => d.Tienich)
                  .WithMany(p => p.TienichLoaiphongs)
                  .HasForeignKey(d => d.MaTi);

            entity.HasOne(d => d.LoaiphongBienthe)
                  .WithMany(p => p.TienichLoaiphongs)
                  .HasForeignKey(d => d.MaBienThePhong);
        });



        modelBuilder.Entity<Chitietdatphong>(entity =>
        {
            entity.HasKey(e => e.MaCtdp).HasName("PK__CHITIETD__FD2652E631E6F19A");

            entity.ToTable("CHITIETDATPHONG");

            entity.Property(e => e.MaCtdp).HasColumnName("maCTDP");
            entity.Property(e => e.GiaDat)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("giaDat");
            entity.Property(e => e.MaDatPhong).HasColumnName("maDatPhong");
            entity.Property(e => e.MaPhong).HasColumnName("maPhong");
            entity.Property(e => e.NgayNhan)
                .HasColumnType("datetime")
                .HasColumnName("ngayNhan");
            entity.Property(e => e.NgayTra)
                .HasColumnType("datetime")
                .HasColumnName("ngayTra");
            entity.Property(e => e.TrangThai).HasColumnName("trangThai");

            entity.HasOne(d => d.MaDatPhongNavigation).WithMany(p => p.Chitietdatphongs)
                .HasForeignKey(d => d.MaDatPhong)
                .HasConstraintName("FK__CHITIETDA__maDat__5BE2A6F2");

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.Chitietdatphongs)
                .HasForeignKey(d => d.MaPhong)
                .HasConstraintName("FK__CHITIETDA__maPho__5CD6CB2B");
        });

        modelBuilder.Entity<Chucvu>(entity =>
        {
            entity.HasKey(e => e.MaChucVu).HasName("PK__CHUCVU__6E42BCD9E80BD777");

            entity.ToTable("CHUCVU");

            entity.Property(e => e.MaChucVu).HasColumnName("maChucVu");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("isDelete");
            entity.Property(e => e.Luong).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Role).HasMaxLength(50);
            entity.Property(e => e.TenChucVu)
                .HasMaxLength(100)
                .HasColumnName("tenChucVu");
        });

        modelBuilder.Entity<Danhgium>(entity =>
        {
            entity.HasKey(e => e.MaDg).HasName("PK__DANHGIA__7A3EF40E99FD76BC");

            entity.ToTable("DANHGIA");

            entity.Property(e => e.MaDg).HasColumnName("maDG");
            entity.Property(e => e.HinhAnh).HasColumnName("hinhAnh");
            entity.Property(e => e.MaDatPhong).HasColumnName("maDatPhong");
            entity.Property(e => e.MaPhong).HasColumnName("maPhong");
            entity.Property(e => e.NgayDanhGia)
                .HasColumnType("datetime")
                .HasColumnName("ngayDanhGia");
            entity.Property(e => e.NgayPhanHoi)
                .HasColumnType("datetime")
                .HasColumnName("ngayPhanHoi");
            entity.Property(e => e.NoiDung).HasColumnName("noiDung");
            entity.Property(e => e.PhanHoi).HasColumnName("phanHoi");
            entity.Property(e => e.SoSao).HasColumnName("soSao");
            entity.Property(e => e.TrangThai).HasColumnName("trangThai");

            entity.HasOne(d => d.MaDatPhongNavigation).WithMany(p => p.Danhgia)
                .HasForeignKey(d => d.MaDatPhong)
                .HasConstraintName("FK__DANHGIA__maDatPh__75A278F5");

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.Danhgia)
                .HasForeignKey(d => d.MaPhong)
                .HasConstraintName("FK__DANHGIA__maPhong__76969D2E");
        });

        modelBuilder.Entity<Datphong>(entity =>
        {
            entity.HasKey(e => e.MaDatPhong).HasName("PK__DATPHONG__67884E4FAEDBEF20");

            entity.ToTable("DATPHONG");

            entity.Property(e => e.MaDatPhong).HasColumnName("maDatPhong");
            entity.Property(e => e.MaBienThePhong).HasColumnName("maBienThePhong");
            entity.Property(e => e.MaCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("maCode");
            entity.Property(e => e.MaKh).HasColumnName("maKH");
            entity.Property(e => e.MaNv).HasColumnName("maNV");
            entity.Property(e => e.NgayDat)
                .HasColumnType("datetime")
                .HasColumnName("ngayDat");
            entity.Property(e => e.TongTienDuKien)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("tongTienDuKien");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasColumnName("trangThai");

            entity.HasOne(d => d.MaBienThePhongNavigation).WithMany(p => p.Datphongs)
                .HasForeignKey(d => d.MaBienThePhong)
                .HasConstraintName("FK__DATPHONG__maBien__5812160E");

            entity.HasOne(d => d.MaCodeNavigation).WithMany(p => p.Datphongs)
                .HasForeignKey(d => d.MaCode)
                .HasConstraintName("FK__DATPHONG__maCode__59063A47");

            entity.HasOne(d => d.MaKhNavigation).WithMany(p => p.Datphongs)
                .HasForeignKey(d => d.MaKh)
                .HasConstraintName("FK__DATPHONG__maKH__571DF1D5");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.Datphongs)
                .HasForeignKey(d => d.MaNv)
                .HasConstraintName("FK__DATPHONG__maNV__5812160E");
        });

        modelBuilder.Entity<Dichvu>(entity =>
        {
            entity.HasKey(e => e.MaDichVu).HasName("PK__DICHVU__C0E6DE8FE67B520F");

            entity.ToTable("DICHVU");

            entity.Property(e => e.Gia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TenDichVu).HasMaxLength(255);
            entity.Property(e => e.TrangThai).HasColumnName("trangThai");
        });

        modelBuilder.Entity<Flashsale>(entity =>
        {
            entity.HasKey(e => e.Flashsaleid).HasName("PK__FLASHSAL__607DA57D89C348AB");

            entity.ToTable("FLASHSALE");

            entity.Property(e => e.Flashsaleid).HasColumnName("FLASHSALEID");
            entity.Property(e => e.NgayBatDau)
                .HasColumnType("datetime")
                .HasColumnName("ngayBatDau");
            entity.Property(e => e.NgayKetThuc)
                .HasColumnType("datetime")
                .HasColumnName("ngayKetThuc");
            entity.Property(e => e.TrangThai).HasColumnName("trangThai");
        });

        modelBuilder.Entity<Hinhanh>(entity =>
        {
            entity.HasKey(e => e.MaHa).HasName("PK__HINHANH__7A3E148314956900");

            entity.ToTable("HINHANH");

            entity.Property(e => e.MaHa).HasColumnName("maHA");
            entity.Property(e => e.DuongDan).HasColumnName("duongDan");

            entity.HasOne(d => d.MaBienThePhongNavigation).WithMany(p => p.Hinhanhs)
                .HasForeignKey(d => d.MaBienThePhong)
                .HasConstraintName("FK__HINHANH__MaBienT__5165187F");
        });

        modelBuilder.Entity<Hoadon>(entity =>
        {
            entity.HasKey(e => e.MaHd).HasName("PK__HOADON__7A3E14866A9568EB");

            entity.ToTable("HOADON");

            entity.Property(e => e.MaHd).HasColumnName("maHD");
            entity.Property(e => e.MaDatPhong).HasColumnName("maDatPhong");
            entity.Property(e => e.NgayTao)
                .HasColumnType("datetime")
                .HasColumnName("ngayTao");
            entity.Property(e => e.SoTienGiam)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("soTienGiam");
            entity.Property(e => e.TongTienGoc)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("tongTienGoc");
            entity.Property(e => e.TongTienPhaiTra)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("tongTienPhaiTra");

            entity.HasOne(d => d.MaDatPhongNavigation).WithMany(p => p.Hoadons)
                .HasForeignKey(d => d.MaDatPhong)
                .HasConstraintName("FK__HOADON__maDatPho__5FB337D6");
        });

        modelBuilder.Entity<Khachhang>(entity =>
        {
            entity.HasKey(e => e.MaKh).HasName("PK__KHACHHAN__7A3ECFE496E36A65");

            entity.ToTable("KHACHHANG");

            entity.Property(e => e.MaKh).HasColumnName("maKH");
            entity.Property(e => e.Cccd)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("CCCD");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.GioiTinh)
                .HasMaxLength(10)
                .HasColumnName("gioiTinh");
            entity.Property(e => e.HinhAnh).HasColumnName("hinhAnh");
            entity.Property(e => e.HoVaTen)
                .HasMaxLength(255)
                .HasColumnName("hoVaTen");
            entity.Property(e => e.MatKhau)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("matKhau");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("ngayTao");
            entity.Property(e => e.Sdt)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("SDT");
            entity.Property(e => e.TrangThai).HasColumnName("trangThai");

            entity.HasMany(d => d.MaCodes).WithMany(p => p.MaKhs)
                .UsingEntity<Dictionary<string, object>>(
                    "KhachhangVoucher",
                    r => r.HasOne<Voucher>().WithMany()
                        .HasForeignKey("MaCode")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__KHACHHANG__maCod__72C60C4A"),
                    l => l.HasOne<Khachhang>().WithMany()
                        .HasForeignKey("MaKh")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__KHACHHANG___maKH__71D1E811"),
                    j =>
                    {
                        j.HasKey("MaKh", "MaCode").HasName("PK__KHACHHAN__C958E6AA0DD97ED0");
                        j.ToTable("KHACHHANG_VOUCHER");
                        j.IndexerProperty<int>("MaKh").HasColumnName("maKH");
                        j.IndexerProperty<string>("MaCode")
                            .HasMaxLength(50)
                            .IsUnicode(false)
                            .HasColumnName("maCode");
                    });
        });

        modelBuilder.Entity<Loaiphong>(entity =>
        {
            entity.HasKey(e => e.MaLp).HasName("PK__LOAIPHON__7A3EB71D521F494B");

            entity.ToTable("LOAIPHONG");

            entity.Property(e => e.MaLp).HasColumnName("maLP");
            entity.Property(e => e.MoTa).HasColumnName("moTa");
            entity.Property(e => e.TenLoai)
                .HasMaxLength(100)
                .HasColumnName("tenLoai");
        });

        modelBuilder.Entity<LoaiphongBienthe>(entity =>
        {
            entity.HasKey(e => e.MaBienThePhong).HasName("PK__LOAIPHON__1BCAC72202FBF370");

            entity.ToTable("LOAIPHONG_BIENTHE");

            entity.Property(e => e.DienTich).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.HuongNhin).HasMaxLength(100);
            entity.Property(e => e.LoaiGiuong).HasMaxLength(100);
            entity.Property(e => e.MaLp).HasColumnName("maLP");
            entity.Property(e => e.TenBienThe).HasMaxLength(255);

            entity.HasOne(d => d.MaLpNavigation).WithMany(p => p.LoaiphongBienthes)
                .HasForeignKey(d => d.MaLp)
                .HasConstraintName("FK__LOAIPHONG___maLP__4BAC3F29");
        });

        modelBuilder.Entity<Nhanvien>(entity =>
        {
            entity.HasKey(e => e.MaNv).HasName("PK__NHANVIEN__7A3EC7D568522DED");

            entity.ToTable("NHANVIEN");

            entity.Property(e => e.MaNv).HasColumnName("maNV");
            entity.Property(e => e.DiaChi)
                .HasMaxLength(255)
                .HasColumnName("diaChi");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.GioiTinh)
                .HasMaxLength(10)
                .HasColumnName("gioiTinh");
            entity.Property(e => e.HinhAnh).HasColumnName("hinhAnh");
            entity.Property(e => e.HoTenNv)
                .HasMaxLength(255)
                .HasColumnName("hoTenNV");
            entity.Property(e => e.MaChucVu).HasColumnName("maChucVu");
            entity.Property(e => e.MatKhau)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("matKhau");
            entity.Property(e => e.NamSinh).HasColumnName("namSinh");
            entity.Property(e => e.NgayVaoLam).HasColumnName("ngayVaoLam");
            entity.Property(e => e.TenDangNhap)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("tenDangNhap");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasColumnName("trangThai");

            entity.HasOne(d => d.MaChucVuNavigation).WithMany(p => p.Nhanviens)
                .HasForeignKey(d => d.MaChucVu)
                .HasConstraintName("FK__NHANVIEN__maChuc__45F365D3");
        });

        modelBuilder.Entity<Phong>(entity =>
        {
            entity.HasKey(e => e.MaPhong).HasName("PK__PHONG__4CD55E10FD5FE388");

            entity.ToTable("PHONG");

            entity.Property(e => e.MaPhong).HasColumnName("maPhong");
            entity.Property(e => e.SoPhong)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("soPhong");
            entity.Property(e => e.TrangThai).HasColumnName("trangThai");

            entity.HasOne(d => d.MaBienThePhongNavigation).WithMany(p => p.Phongs)
                .HasForeignKey(d => d.MaBienThePhong)
                .HasConstraintName("FK__PHONG__MaBienThe__4E88ABD4");
        });

        modelBuilder.Entity<Refeshtoken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__REFESHTO__3214EC070806D5B3");

            entity.ToTable("REFESHTOKEN");

            entity.Property(e => e.ExpiredAt).HasColumnType("datetime");
            entity.Property(e => e.IssuedAt).HasColumnType("datetime");
            entity.Property(e => e.Token).IsUnicode(false);
        });

        modelBuilder.Entity<SudungDichvu>(entity =>
        {
            entity.HasKey(e => e.MaSd).HasName("PK__SUDUNG_D__7A227A7633DBFC54");

            entity.ToTable("SUDUNG_DICHVU");

            entity.Property(e => e.MaSd).HasColumnName("maSD");
            entity.Property(e => e.MaCtdp).HasColumnName("maCTDP");
            entity.Property(e => e.NgaySuDung).HasColumnType("datetime");
            entity.Property(e => e.ThanhTien)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("thanhTien");

            entity.HasOne(d => d.MaCtdpNavigation).WithMany(p => p.SudungDichvus)
                .HasForeignKey(d => d.MaCtdp)
                .HasConstraintName("FK__SUDUNG_DI__maCTD__6754599E");

            entity.HasOne(d => d.MaDichVuNavigation).WithMany(p => p.SudungDichvus)
                .HasForeignKey(d => d.MaDichVu)
                .HasConstraintName("FK__SUDUNG_DI__MaDic__66603565");
        });

        modelBuilder.Entity<Thanhtoan>(entity =>
        {
            entity.HasKey(e => e.MaTt).HasName("PK__THANHTOA__7A226247D2E29228");

            entity.ToTable("THANHTOAN");

            entity.Property(e => e.MaTt).HasColumnName("maTT");
            entity.Property(e => e.HinhThucThanhToan)
                .HasMaxLength(100)
                .HasColumnName("hinhThucThanhToan");
            entity.Property(e => e.LoaiThanhToan)
                .HasMaxLength(100)
                .HasColumnName("loaiThanhToan");
            entity.Property(e => e.MaDatPhong).HasColumnName("maDatPhong");
            entity.Property(e => e.MaHd).HasColumnName("maHD");
            entity.Property(e => e.SoTien)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("soTien");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasColumnName("trangThai");

            entity.HasOne(d => d.MaDatPhongNavigation).WithMany(p => p.Thanhtoans)
                .HasForeignKey(d => d.MaDatPhong)
                .HasConstraintName("FK__THANHTOAN__maDat__628FA481");

            entity.HasOne(d => d.MaHdNavigation).WithMany(p => p.Thanhtoans)
                .HasForeignKey(d => d.MaHd)
                .HasConstraintName("FK__THANHTOAN__maHD__6383C8BA");
        });

        modelBuilder.Entity<Thietlapgium>(entity =>
        {
            entity.HasKey(e => e.MaThietLap).HasName("PK__THIETLAP__6F8C7C94E0B785D2");

            entity.ToTable("THIETLAPGIA");

            entity.Property(e => e.Gia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LoaiGia).HasMaxLength(50);
            entity.Property(e => e.MacDinh).HasColumnName("macDinh");
            entity.Property(e => e.MoTa).HasColumnName("moTa");
            entity.Property(e => e.NgayBatDau)
                .HasColumnType("datetime")
                .HasColumnName("ngayBatDau");
            entity.Property(e => e.NgayKetThuc)
                .HasColumnType("datetime")
                .HasColumnName("ngayKetThuc");

            entity.HasOne(d => d.MaBienThePhongNavigation).WithMany(p => p.Thietlapgia)
                .HasForeignKey(d => d.MaBienThePhong)
                .HasConstraintName("FK__THIETLAPG__MaBie__5441852A");
        });

        modelBuilder.Entity<Tienich>(entity =>
        {
            entity.HasKey(e => e.MaTi).HasName("PK__TIENICH__7A226250A950B1B9");

            entity.ToTable("TIENICH");

            entity.Property(e => e.MaTi).HasColumnName("maTI");
            entity.Property(e => e.LoaiTienIch)
                .HasMaxLength(100)
                .HasColumnName("loaiTienIch");
            entity.Property(e => e.TenTienIch)
                .HasMaxLength(255)
                .HasColumnName("tenTienIch");
            entity.Property(e => e.TrangThai).HasColumnName("trangThai");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.MaCode).HasName("PK__VOUCHER__366294EAD8C3BA63");

            entity.ToTable("VOUCHER");

            entity.Property(e => e.MaCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("maCode");
            entity.Property(e => e.GiaToiThieu)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("giaToiThieu");
            entity.Property(e => e.MoTa).HasColumnName("moTa");
            entity.Property(e => e.NgayBatDau)
                .HasColumnType("datetime")
                .HasColumnName("ngayBatDau");
            entity.Property(e => e.NgayKetThuc)
                .HasColumnType("datetime")
                .HasColumnName("ngayKetThuc");
            entity.Property(e => e.PhanTramGiam).HasColumnName("phanTramGiam");
            entity.Property(e => e.SoLuong).HasColumnName("soLuong");
            entity.Property(e => e.SoLuongDaDung).HasColumnName("soLuongDaDung");
            entity.Property(e => e.SoTienGiam)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("soTienGiam");
            entity.Property(e => e.TrangThai).HasColumnName("trangThai");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
