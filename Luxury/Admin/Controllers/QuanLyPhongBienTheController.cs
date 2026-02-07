using Luxury.Admin.ViewModel;
using Luxury.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Luxury.Admin.Controllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    public class QuanLyPhongBienTheController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;
        public QuanLyPhongBienTheController(LuxuryHotelContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult GetAll([FromQuery] string? keyword, [FromQuery] int? loaiPhongId, [FromQuery] int? maLp, [FromQuery] int page = 1, [FromQuery] int pageSize = 9)
        {
            int tongBienThe = _context.LoaiphongBienthes.Count();
            int tongPhong = _context.Phongs.Count();
            int tongPhongTrong = _context.Phongs.Count(x => x.TrangThai == 1);
            int dangCoKhach = _context.Chitietdatphongs
                .Where(ct =>
                    ct.TrangThai == true &&
                    ct.NgayNhan <= DateTime.Now &&
                    (ct.NgayTra == null || ct.NgayTra >= DateTime.Now)
                )
                .Select(ct => ct.MaDatPhongNavigation.MaKh)
                .Distinct()
                .Count();

            var dsLoaiPhong = _context.Loaiphongs
                .Select(x => new
                {
                    x.MaLp,
                    x.TenLoai,
                    SoLuong = _context.LoaiphongBienthes.Count(bt => bt.MaLp == x.MaLp)
                }).ToList();
            var dsTienIch = _context.Tieniches.Where(x => x.TrangThai == true).Select(x => new
            {
                x.MaTi,
                x.TenTienIch,
                x.Icon
            }).ToList();

            var dsBienThe = _context.LoaiphongBienthes.Select(x => new
            {
                x.MaBienThePhong,
                x.TenBienThe
            }).ToList();

            var query = _context.LoaiphongBienthes
                .Include(x => x.Phongs)
                .Include(x => x.Hinhanhs)
                .Include(x => x.MaLpNavigation)
                .Include(x => x.Thietlapgia)
                .Include(x => x.TienichLoaiphongs)
                    .ThenInclude(tlp => tlp.Tienich)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(x => x.TenBienThe.Contains(keyword));
            }

            if (loaiPhongId.HasValue)
            {
                query = query.Where(x => x.MaLp == loaiPhongId.Value);
            }
            if (maLp.HasValue)
            {
                query = query.Where(x => x.MaLp == maLp.Value);
            }
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var data = query
                .OrderByDescending(x => x.MaBienThePhong)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.MaBienThePhong,
                    x.TenBienThe,
                    AnhDaiDien=_context.Hinhanhs.Where(a=>a.MaBienThePhong==x.MaBienThePhong).Select(a=>a.DuongDan).FirstOrDefault(),
                    dsAnh = x.Hinhanhs
                        .Select(h => h.DuongDan)
                        .ToList(),

                    x.LoaiGiuong,
                    x.SoNguoiLon,
                    x.SoTreEm,
                    x.DienTich,

                    TenLoai = x.MaLpNavigation.TenLoai,

                    GiaGoc = x.Thietlapgia
                        .Where(g => g.MacDinh == true)
                        .Select(g => g.Gia)
                        .FirstOrDefault(),

                    GiaBienDong = x.Thietlapgia
                        .Where(g => g.MacDinh == false)
                        .Select(g => g.Gia)
                        .FirstOrDefault(),

                    x.HuongNhin,

                    SoPhongCon = _context.Phongs.Count(p =>
                        p.MaBienThePhong == x.MaBienThePhong &&
                        p.TrangThai == 1
                    ),
                    TienIch = x.TienichLoaiphongs
                        .Select(tlp => new
                        {
                            tlp.Tienich.MaTi,
                            tlp.Tienich.TenTienIch,
                            tlp.Tienich.Icon
                        }).ToList(),

                    Phongs = x.Phongs.Select(p => new
                    {
                        p.MaPhong,
                        p.SoPhong,
                        p.TrangThai
                    }).ToList(),
                }).ToList();

            return Ok(new
            {
                dashboard = new
                {
                    TongBienThe = tongBienThe,
                    TongPhong = tongPhong,
                    TongPhongTrong = tongPhongTrong,
                    DangCoKhach = dangCoKhach,
                    DSLoaiPhong = dsLoaiPhong,
                    DSTienIch = dsTienIch,
                    DSBienThe = dsBienThe
                },
                pagination = new
                {
                    page,
                    pageSize,
                    totalItems,
                    totalPages
                },
                data
            });
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateBienTheDto dto)
        {
            var isExist = _context.LoaiphongBienthes
                .Any(x => x.MaLp == dto.MaLp && x.TenBienThe.Trim().ToLower() == dto.TenBienThe.Trim().ToLower() );

            if (isExist)
            {
                return BadRequest("Tên biến thể phòng đã tồn tại");
            }

            if (dto.Images == null || dto.Images.Count == 0)
                return BadRequest("Phải có ít nhất 1 hình ảnh");
            // 1. Tạo biến thể
            var bienThe = new LoaiphongBienthe
            {
                MaLp = dto.MaLp,
                TenBienThe = dto.TenBienThe,
                LoaiGiuong = dto.LoaiGiuong,
                HuongNhin = dto.HuongNhin,
                SoNguoiLon = dto.SoNguoiLon,
                SoTreEm = dto.SoTreEm,
                DienTich = dto.DienTich
            };

            _context.LoaiphongBienthes.Add(bienThe);
            await _context.SaveChangesAsync();

            // 2. Lưu giá mặc định
            _context.Thietlapgia.Add(new Thietlapgium
            {
                MaBienThePhong = bienThe.MaBienThePhong,
                Gia = dto.GiaNiemYet,
                MacDinh = true
            });

            // 3. Lưu tiện ích (N-N)
            foreach (var ti in dto.TienIchIds)
            {
                _context.TienichLoaiphongs.Add(new TienichLoaiphong
                {
                    MaTi = ti,
                    MaBienThePhong = bienThe.MaBienThePhong
                });
            }

            // 4. Lưu ảnh
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "variants");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            foreach (var file in dto.Images)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadPath, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                _context.Hinhanhs.Add(new Hinhanh
                {
                    MaBienThePhong = bienThe.MaBienThePhong,
                    DuongDan = $"uploads/variants/{fileName}"
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Tạo biến thể thành công" });
        }
        [HttpDelete("{id}")]
        public IActionResult DeleteBienThePhong(int id)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // 1. Kiểm tra biến thể
                var bienThe = _context.LoaiphongBienthes
                    .FirstOrDefault(x => x.MaBienThePhong == id);
                bool coPhongDangCoKhach = _context.Phongs
                    .Any(p =>
                        p.MaBienThePhong == id &&
                        p.Chitietdatphongs.Any(ct =>
                            ct.TrangThai == true &&
                            ct.NgayNhan <= DateTime.Now &&
                            (ct.NgayTra == null || ct.NgayTra >= DateTime.Now)
                        )
                    );
                if (coPhongDangCoKhach)
                {
                    return BadRequest("Không thể xóa biến thể vì đang có phòng được thuê");
                }
                if (bienThe == null)
                    return NotFound("Không tìm thấy biến thể phòng");

                // 2. Lấy danh sách phòng thuộc biến thể
                var dsPhong = _context.Phongs
                    .Where(p => p.MaBienThePhong == id)
                    .Select(p => p.MaPhong)
                    .ToList();

                // 3. Xóa đánh giá
                var danhGia = _context.Danhgia.Where(dg => dg.MaPhong.HasValue && dsPhong.Contains(dg.MaPhong.Value));

                _context.Danhgia.RemoveRange(danhGia);


                // 4. Lấy danh sách chi tiết đặt phòng
                var dsCTDP = _context.Chitietdatphongs
                    .Where(ct => ct.MaPhong.HasValue && dsPhong.Contains(ct.MaPhong.Value))
                    .Select(ct => ct.MaCtdp)
                    .ToList();


                // 5. Xóa sử dụng dịch vụ
                var suDungDV = _context.SudungDichvus
                    .Where(sd => sd.MaCtdp.HasValue && dsCTDP.Contains(sd.MaCtdp.Value));

                _context.SudungDichvus.RemoveRange(suDungDV);


                // 6. Xóa chi tiết đặt phòng
                var chiTietDP = _context.Chitietdatphongs
                    .Where(ct => ct.MaPhong.HasValue && dsPhong.Contains(ct.MaPhong.Value));

                _context.Chitietdatphongs.RemoveRange(chiTietDP);


                // 7. Xóa phòng
                var phong = _context.Phongs
                    .Where(p => p.MaBienThePhong == id);
                _context.Phongs.RemoveRange(phong);

                // 8. Xóa hình ảnh
                var hinhAnh = _context.Hinhanhs
                    .Where(h => h.MaBienThePhong == id);
                _context.Hinhanhs.RemoveRange(hinhAnh);

                // 9. Xóa thiết lập giá
                var gia = _context.Thietlapgia
                    .Where(g => g.MaBienThePhong == id);
                _context.Thietlapgia.RemoveRange(gia);

                // 10. Xóa tiện ích - loại phòng
                var tienIch = _context.TienichLoaiphongs
                    .Where(t => t.MaBienThePhong == id);
                _context.TienichLoaiphongs.RemoveRange(tienIch);

                // 11. Xóa chi tiết flash sale
                var flashSale = _context.ChitietFlashsales
                    .Where(f => f.MaBienThePhong == id);
                _context.ChitietFlashsales.RemoveRange(flashSale);

                // 12. Cuối cùng xóa biến thể
                _context.LoaiphongBienthes.Remove(bienThe);

                _context.SaveChanges();
                transaction.Commit();

                return Ok("Xóa biến thể phòng (xóa cứng) thành công");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest("Lỗi khi xóa biến thể: " + ex.Message);
            }
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var bienThe = await _context.LoaiphongBienthes
                .Include(x => x.TienichLoaiphongs)
                .Include(x => x.Thietlapgia)
                .Include(x => x.Hinhanhs)
                .FirstOrDefaultAsync(x => x.MaBienThePhong == id);

            if (bienThe == null)
                return NotFound("Không tìm thấy biến thể");

            var dto = new CreateBienTheDto
            {
                MaLp = bienThe.MaLp ?? 0,
                TenBienThe = bienThe.TenBienThe,
                LoaiGiuong = bienThe.LoaiGiuong,
                HuongNhin = bienThe.HuongNhin,
                SoNguoiLon = bienThe.SoNguoiLon ?? 0,
                SoTreEm = bienThe.SoTreEm ?? 0,
                DienTich = bienThe.DienTich ?? 0,

                GiaNiemYet = bienThe.Thietlapgia
                    .FirstOrDefault(x => x.MacDinh==true)?.Gia ?? 0,

                TienIchIds = bienThe.TienichLoaiphongs
                    .Select(x => x.MaTi)
                    .ToList(),

                ImageUrls = bienThe.Hinhanhs
                    .Select(x => $"{Request.Scheme}://{Request.Host}/{x.DuongDan}")
                    .ToList()
            };
            return Ok(dto);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] CreateBienTheDto dto)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var isExist = _context.LoaiphongBienthes.Any(x =>
                    x.MaLp == dto.MaLp &&
                    x.TenBienThe.Trim().ToLower() == dto.TenBienThe.Trim().ToLower()
                    && x.MaBienThePhong != id
                );
                if (isExist)
                {
                    return BadRequest("Tên biến thể phòng đã tồn tại");
                }
                //if (dto.Images == null || dto.Images.Count == 0)
                //    return BadRequest("Phải có ít nhất 1 hình ảnh");
                var bienThe = await _context.LoaiphongBienthes
                    .FirstOrDefaultAsync(x => x.MaBienThePhong == id);

                if (bienThe == null)
                    return NotFound("Không tìm thấy biến thể phòng");
                
                // 1. Update biến thể
                bienThe.MaLp = dto.MaLp;
                bienThe.TenBienThe = dto.TenBienThe;
                bienThe.LoaiGiuong = dto.LoaiGiuong;
                bienThe.HuongNhin = dto.HuongNhin;
                bienThe.SoNguoiLon = dto.SoNguoiLon;
                bienThe.SoTreEm = dto.SoTreEm;
                bienThe.DienTich = dto.DienTich;

                // 2. Update giá mặc định
                var giaMacDinh = await _context.Thietlapgia
                    .FirstOrDefaultAsync(g => g.MaBienThePhong == id && g.MacDinh==true);

                if (giaMacDinh != null)
                    giaMacDinh.Gia = dto.GiaNiemYet;
                else
                    _context.Thietlapgia.Add(new Thietlapgium
                    {
                        MaBienThePhong = id,
                        Gia = dto.GiaNiemYet,
                        MacDinh = true
                    });

                // 3. Update tiện ích
                var oldTienIch = _context.TienichLoaiphongs
                    .Where(t => t.MaBienThePhong == id);
                _context.TienichLoaiphongs.RemoveRange(oldTienIch);

                foreach (var ti in dto.TienIchIds)
                {
                    _context.TienichLoaiphongs.Add(new TienichLoaiphong
                    {
                        MaBienThePhong = id,
                        MaTi = ti
                    });
                }

                // 4. Upload ảnh mới (không xóa ảnh cũ)
                if (dto.Images != null && dto.Images.Any())
                {
                    var uploadPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "variants");

                    Directory.CreateDirectory(uploadPath);

                    foreach (var file in dto.Images)
                    {
                        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                        var filePath = Path.Combine(uploadPath, fileName);

                        using var stream = new FileStream(filePath, FileMode.Create);
                        await file.CopyToAsync(stream);

                        _context.Hinhanhs.Add(new Hinhanh
                        {
                            MaBienThePhong = id,
                            DuongDan = $"uploads/variants/{fileName}"
                        });
                    }
                }

                await _context.SaveChangesAsync();
                transaction.Commit();

                return Ok("Cập nhật biến thể phòng thành công");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest(ex.Message);
            }
        }
    }
}
