using Luxury.DTO.Sale;
using Luxury.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Luxury.KhachHang.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SaleController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;
        public SaleController(LuxuryHotelContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllSale()
        {
            var Sale = await _context.Flashsales.Where(x => x.TrangThai != false || x.NgayKetThuc > DateTime.Now).ToListAsync();
            bool status = false;
            var dateNow = DateTime.Now;
            foreach (var s in Sale)
            {
                if (s.NgayKetThuc < dateNow && s.TrangThai != false)
                {
                    s.TrangThai = false;
                    status = true;
                }
                else if (s.NgayBatDau <= dateNow && s.NgayKetThuc >= dateNow)
                {
                    if (s.TrangThai == null)
                    {
                        s.TrangThai = true;
                        status = true;
                    }
                }
                else if (s.NgayBatDau > dateNow && s.TrangThai != null)
                {
                    s.TrangThai = null;
                    status = true;
                }
            }
            if (status)
            {
                await _context.SaveChangesAsync();
            }
            var dataSale = _context.ChitietFlashsales
                     .Join(_context.Flashsales,
                         ct => ct.Flashsaleid,
                         fs => fs.Flashsaleid,
                         (ct, fs) => new { ct, fs })

                     .Join(_context.LoaiphongBienthes,
                         x => x.ct.MaBienThePhong,
                         bt => bt.MaBienThePhong,
                         (x, bt) => new { x.ct, x.fs, bt })

                     .Join(_context.Loaiphongs,
                         x => x.bt.MaLp,
                         lp => lp.MaLp,
                         (x, lp) => new { x.ct, x.fs, x.bt, lp })

                     .Join(_context.Thietlapgia,
                         x => x.bt.MaBienThePhong,
                         gia => gia.MaBienThePhong,
                         (x, gia) => new
                         {
                             x.fs,
                             x.ct,
                             x.bt,
                             x.lp,
                             gia
                         })
                     .GroupBy(x => new
                     {
                         x.fs.Flashsaleid,
                         x.fs.NgayBatDau,
                         x.fs.NgayKetThuc,
                         x.fs.TrangThai
                     })

                     .Select(g => new SaleResponseDTOcs
                     {
                         Flashsaleid = g.Key.Flashsaleid,
                         NgayBatDau = g.Key.NgayBatDau,
                         NgayKetThuc = g.Key.NgayKetThuc,
                         TrangThai = g.Key.TrangThai,
                         TongSoPhong = g.Count().ToString(),
                     })
                     .ToList();

            return Ok(dataSale);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSalEById(int id)
        {
            try
            {
                var dataSale = _context.ChitietFlashsales
                    .Join(_context.Flashsales,
                        ct => ct.Flashsaleid,
                        fs => fs.Flashsaleid,
                        (ct, fs) => new { ct, fs })
                    .Where(x => x.fs.Flashsaleid == id)
                    .Join(_context.LoaiphongBienthes,
                        x => x.ct.MaBienThePhong,
                        bt => bt.MaBienThePhong,
                        (x, bt) => new { x.ct, x.fs, bt })
                    .Join(_context.Loaiphongs,
                        x => x.bt.MaLp,
                        lp => lp.MaLp,
                        (x, lp) => new { x.ct, x.fs, x.bt, lp })
                    .Join(_context.Thietlapgia,
                        x => x.bt.MaBienThePhong,
                        gia => gia.MaBienThePhong,
                        (x, gia) => new { x.fs, x.ct, x.bt, x.lp, gia })
                    .GroupBy(x => new
                    {
                        x.fs.Flashsaleid,
                        x.fs.NgayBatDau,
                        x.fs.NgayKetThuc,
                        x.fs.TrangThai
                    })
                    .Select(g => new SaleResponseDTOcs
                    {
                        Flashsaleid = g.Key.Flashsaleid,
                        NgayBatDau = g.Key.NgayBatDau,
                        NgayKetThuc = g.Key.NgayKetThuc,
                        TrangThai = g.Key.TrangThai,
                        TongSoPhong = g.Count().ToString(),
                        ChiTietSales = g.Select(x => new SaleDetailResponseDTO
                        {
                            PhanTramGiam = x.ct.PhanTramGiam,
                            TenLoai = x.lp.TenLoai,
                            TenBienThe = x.bt.TenBienThe,
                            GiaGoc = x.gia.Gia,
                            GiaSauGiam = x.gia.Gia * (1 - (x.ct.PhanTramGiam ?? 0) / 100m)
                        }).ToList()
                    })
                    .FirstOrDefault();

                if (dataSale == null) return NotFound("Không tìm thấy Sale này.");

                return Ok(dataSale);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> CreateSale([FromBody] SaleRequestDTO addSale)
        {
            try
            {
                var objSale = new Flashsale
                {
                    NgayBatDau = addSale.NgayBatDau,
                    NgayKetThuc = addSale.NgayKetThuc,
                    TrangThai = addSale.TrangThai,
                };
                _context.Flashsales.Add(objSale);
                await _context.SaveChangesAsync();

                var chiTietSale = addSale.SaleDetails.Select(x => new ChitietFlashsale
                {
                    Flashsaleid = objSale.Flashsaleid,
                    MaBienThePhong = x.MaBienThePhong,
                    PhanTramGiam = x.PhanTramGiam
                }).ToList();
                _context.ChitietFlashsales.AddRange(chiTietSale);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Tạo flash sale thành công"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSale(int id, [FromBody] SaleRequestDTO updateDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var saleExisting = await _context.Flashsales
                    .FirstOrDefaultAsync(x => x.Flashsaleid == id);

                if (saleExisting == null)
                    return NotFound(new { Message = "Không tìm thấy chương trình khuyến mãi." });


                if (updateDto.NgayBatDau >= updateDto.NgayKetThuc)
                    return BadRequest("Ngày kết thúc phải lớn hơn ngày bắt đầu.");

                if (updateDto.SaleDetails == null || !updateDto.SaleDetails.Any())
                    return BadRequest("Danh sách phòng áp dụng không được để trống.");

                if (updateDto.SaleDetails
                    .GroupBy(x => x.MaBienThePhong)
                    .Any(g => g.Count() > 1))
                {
                    return BadRequest("Danh sách phòng bị trùng lặp.");
                }

                if (updateDto.SaleDetails.Any(x => x.PhanTramGiam <= 0 || x.PhanTramGiam > 100))
                    return BadRequest("Mức giảm giá không hợp lệ (1-100%).");

                saleExisting.NgayBatDau = updateDto.NgayBatDau;
                saleExisting.NgayKetThuc = updateDto.NgayKetThuc;

                if (updateDto.TrangThai.HasValue)
                    saleExisting.TrangThai = updateDto.TrangThai.Value;

                await _context.ChitietFlashsales
                    .Where(x => x.Flashsaleid == id)
                    .ExecuteDeleteAsync();

                var newDetails = updateDto.SaleDetails.Select(x => new ChitietFlashsale
                {
                    Flashsaleid = id,
                    MaBienThePhong = x.MaBienThePhong,
                    PhanTramGiam = x.PhanTramGiam
                });

                await _context.ChitietFlashsales.AddRangeAsync(newDetails);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    Message = "Cập nhật FlashSale thành công"
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Lỗi hệ thống khi cập nhật FlashSale");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSale(int id)
        {
            try
            {
                var sale = await _context.Flashsales.FindAsync(id);
                if (sale == null)
                {
                    return NotFound(new { Message = "Flash sale không tồn tại" });
                }
                var chiTietSales = _context.ChitietFlashsales.Where(ct => ct.Flashsaleid == id);
                _context.ChitietFlashsales.RemoveRange(chiTietSales);
                _context.Flashsales.Remove(sale);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Xóa flash sale thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }

}

