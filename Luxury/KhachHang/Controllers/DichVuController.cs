using Luxury.DTO.DichVu;
using Luxury.DTO.OrderDichVu;
using Luxury.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Luxury.Services;
namespace Luxury.KhachHang.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DichVuController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IHubContext<hotelHub> _hubContext;


        public DichVuController(LuxuryHotelContext context, IWebHostEnvironment environment, IHubContext<hotelHub> hubContext)
        {
            _context = context;
            _environment = environment;
            _hubContext = hubContext;
        }
        private async Task<string> upLoadImage(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                throw new Exception("Chưa chọn hình ảnh");
            if (imageFile.Length > 5 * 1024 * 1024)
                throw new Exception("Ảnh kông được vượt quá 5MB");
            var ext = Path.GetExtension(imageFile.FileName).ToLower();
            if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext))
                throw new Exception("Chỉ cho phép file ảnh");
            var fileName = Guid.NewGuid() + ext;
            var folder = "wwwroot/anhDichVu";
            var path = Path.Combine(folder, fileName);
            Directory.CreateDirectory(folder);
            using var stream = new FileStream(path, FileMode.Create);
            await imageFile.CopyToAsync(stream);
            return fileName;
        }
        private void DeleteImage(string imageName)
        {
            if (string.IsNullOrEmpty(imageName)) return;

            var path = Path.Combine("wwwroot/anhDichVu", imageName);
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var ds = await _context.Dichvus
       
                .Select(dv => new
                {
                    dv.MaDichVu,
                    dv.TenDichVu,
                    dv.Gia,
                    dv.HinhAnh,
                    dv.TrangThai
                }).ToListAsync();
            return Ok(ds);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAllById(int id)
        {
            var DichVu = await _context.Dichvus.Where(dv => dv.MaDichVu == id)        
                .Select(dv => new
                {
                    dv.MaDichVu,
                    dv.TenDichVu,
                    dv.Gia,
                    dv.HinhAnh,
                    dv.TrangThai
                }).FirstOrDefaultAsync();
            if (DichVu == null) return NotFound("Không tìm thấy dịch vụ");
            return Ok(DichVu);
        }
        [HttpPost]
        public async Task<IActionResult> AddDichVu([FromForm] dichVuRequestDTO newDichVu)
        {
            try
            {
                var service = new Dichvu
                {
                    TenDichVu = newDichVu.TenDichVu,
                    Gia = newDichVu.Gia,
                    TrangThai = newDichVu.TrangThai = true
                };
                if(newDichVu.upLoadImage != null)
                {
                    service.HinhAnh = await upLoadImage(newDichVu.upLoadImage);
                }
                _context.Dichvus.Add(service);
                await _context.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("reload_Service");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok();
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDichVu([FromForm]dichVuRequestDTO update, int id)
        {
            try
            {
                var dichVu = await _context.Dichvus.FindAsync(id);
                if (dichVu == null) return NotFound("Không tìm thấy dịch vụ");

                dichVu.TenDichVu = update.TenDichVu ?? dichVu.TenDichVu;
                dichVu.Gia = update.Gia ?? dichVu.Gia;
                if (update.upLoadImage != null)
                {
                    DeleteImage(dichVu.HinhAnh);
                    dichVu.HinhAnh = await upLoadImage(update.upLoadImage);
                }
                dichVu.TrangThai = update.TrangThai ?? dichVu.TrangThai;

                await _context.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("reload_Service");
                var result = new dichVuResponseDTO
                {
                    MaDichVu = dichVu.MaDichVu,
                    TenDichVu = dichVu.TenDichVu,
                    Gia = dichVu.Gia.GetValueOrDefault(),
                    HinhAnh = dichVu.HinhAnh,
                    TrangThai = dichVu.TrangThai.GetValueOrDefault()
                };
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDichVu(int id)
        {
            try
            {
                var dichVu = await _context.Dichvus.FindAsync(id);
                if (dichVu == null)
                {
                    return NotFound();
                }
                _context.Dichvus.Remove(dichVu);
                await _context.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("reload_Service");
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        // remote order service

        [HttpPost("dat-dich-vu")]
        public async Task<IActionResult> orderDichVu(OrderDichVuRequestDTO order)
        {
            try
            {
                int finalMaCtdp = 0;

           
                if (order.MaCtdp.HasValue && order.MaCtdp > 0)
                {
                    var ctdp = await _context.Chitietdatphongs
                        .FirstOrDefaultAsync(x => x.MaCtdp == order.MaCtdp && x.TrangThai == true);

                    if (ctdp == null) return BadRequest("Mã CTĐP không hợp lệ hoặc phòng đã trả!");
                    finalMaCtdp = ctdp.MaCtdp;
                }
                else if (order.MaPhong.HasValue && order.MaPhong > 0)
                {
                    var ctdp = await _context.Chitietdatphongs
                        .FirstOrDefaultAsync(x => x.MaPhong == order.MaPhong && x.TrangThai == true);

                    if (ctdp == null) return BadRequest("Lỗi: Phòng này hiện tại không có khách!");
                    finalMaCtdp = ctdp.MaCtdp;
                }
                else
                {
                    return BadRequest("Thiếu dữ liệu: Phải truyền MaCtdp (đối với Web) hoặc MaPhong (đối với App)!");
                }

                foreach (var dichVu in order.DichVus)
                {
                    var service = await _context.Dichvus.FirstOrDefaultAsync(x => x.MaDichVu == dichVu.MaDichVu && x.TrangThai == true);

                    if (service == null)
                        return BadRequest("Dịch vụ không tồn tại hoặc đã ngưng sử dụng");

                    var sdDichVu = new SudungDichvu
                    {
                        MaCtdp = finalMaCtdp,
                        MaDichVu = dichVu.MaDichVu,
                        SoLuong = dichVu.SoLuong,
                        ThanhTien = service.Gia * dichVu.SoLuong,
                        NgaySuDung = DateTime.Now,
                        TrangThai = 0
                    };
                    _context.SudungDichvus.Add(sdDichVu);
                }

                await _context.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("reload_Service");
                return Ok(new { Message = "Đặt dịch vụ thành công" });

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("quan-ly-dat-dich-vu")]
        public async Task<IActionResult> allServiceUsing( int? maPhong)
        {
            try
            {

                var query = _context.Chitietdatphongs
            .Where(ct => ct.TrangThai == true)
            .Join(_context.Phongs, ct => ct.MaPhong, p => p.MaPhong, (ct, p) => new { ct, p })
            .Join(_context.Datphongs, x => x.ct.MaDatPhong, dp => dp.MaDatPhong, (x, dp) => new { x.ct, x.p, dp })
            .Join(_context.Khachhangs, x => x.dp.MaKh, kh => kh.MaKh, (x, kh) => new { x.ct, x.p, x.dp, kh })
            .Join(_context.LoaiphongBienthes, x => x.p.MaBienThePhong, bt => bt.MaBienThePhong, (x, bt) => new { x.ct, x.p, x.dp, x.kh, bt })
            .Join(_context.Loaiphongs, x => x.bt.MaLp, lp => lp.MaLp, (x, lp) => new { x.ct, x.p, x.dp, x.kh, x.bt, lp })
            .AsQueryable();
                if (maPhong.HasValue)
                {
                    query = query.Where(x => x.p.MaPhong == maPhong.Value);
                }

                var result = await query.Select(x => new orderDichVuResponseDTO
                {
                    MaCtdp = x.ct.MaCtdp,
                    TenKhach = x.kh.HoVaTen,
                    SoPhong = x.p.SoPhong, 
                    TenLoai = x.lp.TenLoai,
                    TenBienThe = x.bt.TenBienThe,

                    DichVus = _context.SudungDichvus
                        .Where(sd => sd.MaCtdp == x.ct.MaCtdp)
                        .Join(_context.Dichvus, sd => sd.MaDichVu, dv => dv.MaDichVu, (sd, dv) => new DichVuPhongItemDTO
                        {
                            MaSd = sd.MaSd,
                            TenDichVu = dv.TenDichVu,
                            SoLuong = sd.SoLuong ?? 0,
                            ThanhTien = sd.ThanhTien ?? 0,
                            NgaySuDung = sd.NgaySuDung ?? DateTime.Now,
                            TrangThai = sd.TrangThai ?? 0
                        })
                        .OrderByDescending(d => d.NgaySuDung)
                        .ToList()
                })
                .ToListAsync();

                foreach (var room in result)
                {
                    room.TongTienDichVu = room.DichVus
                                              .Where(d => d.TrangThai != 3)
                                              .Sum(d => d.ThanhTien);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("sua-dich-vu")]
        public async Task<IActionResult> updateOrderDichVu([FromForm] int? newStatus, [FromForm] int? id, [FromForm] int? newSoluong, [FromForm] int? mactdp)
        {
            try
            {
                if (id.HasValue && mactdp.HasValue)
                    return BadRequest("Chỉ được truyền id hoặc mactdp, không được cả hai");

                if (mactdp.HasValue && newStatus.HasValue)
                {
                    var items = await _context.SudungDichvus
                        .Where(x => x.MaCtdp == mactdp.Value )
                        .ToListAsync();

                    if (!items.Any())
                        return NotFound("Không có dịch vụ nào cần cập nhật");

                    foreach (var item in items)
                    {
                        item.TrangThai = newStatus.Value;
                    }

                    await _context.SaveChangesAsync();
                    await _hubContext.Clients.All.SendAsync("reload_Service");

                    return Ok(new
                    {
                        Message = "Cập nhật toàn bộ đơn thành công",
                        SoLuongMon = items.Count
                    });
                }
                if (id.HasValue)
                {
                    var sdDichVu = await _context.SudungDichvus.FindAsync(id.Value);
                    if (sdDichVu == null)
                        return NotFound("Không tìm thấy dịch vụ đã đặt");

                    if (sdDichVu.TrangThai != 0 && sdDichVu.TrangThai != 1)
                        return BadRequest("Món này đã hoàn thành, không thể chỉnh sửa.");

                    if (newSoluong.HasValue)
                    {
                        if (newSoluong.Value <= 0)
                        {
                            _context.SudungDichvus.Remove(sdDichVu);
                            await _context.SaveChangesAsync();
                            await _hubContext.Clients.All.SendAsync("reload_Service");

                            return Ok(new
                            {
                                Message = "Xoá dịch vụ đã đặt thành công"
                            });
                        }

                        sdDichVu.SoLuong = newSoluong.Value;

                        var giaDV = await _context.Dichvus
                            .Where(dv => dv.MaDichVu == sdDichVu.MaDichVu)
                            .Select(dv => dv.Gia)
                            .FirstOrDefaultAsync();

                        sdDichVu.ThanhTien = giaDV * newSoluong.Value;
                    }

                    if (newStatus.HasValue)
                    {
                        sdDichVu.TrangThai = newStatus.Value;
                    }

                    await _context.SaveChangesAsync();
                    await _hubContext.Clients.All.SendAsync("reload_Service");
                    return Ok(new
                    {
                        Message = "Cập nhật dịch vụ thành công",
                        Data = new
                        {
                            sdDichVu.MaSd,
                            sdDichVu.MaDichVu,
                            sdDichVu.MaCtdp,
                            sdDichVu.SoLuong,
                            sdDichVu.ThanhTien,
                            sdDichVu.NgaySuDung,
                            sdDichVu.TrangThai
                        }
                    });
                }

                return BadRequest("Phải truyền 1 món hoặc cả đơn");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("xoa-dich-vu/{id}")]
        public async Task<IActionResult> deleteDichhVu(int id)
        {
            var sdDichVu = await _context.SudungDichvus.FindAsync(id);
            if (sdDichVu == null) return NotFound("Không tìm thấy dịch vụ đã đặt");

            if (sdDichVu.TrangThai != 0)
            {
                return BadRequest("Món này đã được chế biến, không thể xoá.");
            }
            _context.SudungDichvus.Remove(sdDichVu);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("reload_Service");
            return Ok(new
            {
                Message = "Xoá dịch vụ đã đặt thành công"
            });
        }
    }
}