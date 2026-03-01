using Luxury.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Luxury.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThongKeController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;
        public ThongKeController(LuxuryHotelContext context) { _context = context; }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(DateTime? fromDate, DateTime? toDate, string? searchCode, string? status)
        {
            var start = fromDate?.Date ?? new DateTime(2026, 1, 1);
            var end = toDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Now;

            var totalRevenue = await _context.Hoadons
                .Where(h => h.NgayTao >= start && h.NgayTao <= end)
                .SumAsync(h => h.TongTienPhaiTra) ?? 0;

            var revenueChart = await _context.Hoadons
                .Where(h => h.NgayTao >= start && h.NgayTao <= end)
                .GroupBy(h => new { h.NgayTao.Value.Year, h.NgayTao.Value.Month })
                .Select(g => new {
                    Label = $"Tháng {g.Key.Month}",
                    Total = (double)(g.Sum(x => x.TongTienPhaiTra) ?? 0),
                    SortKey = g.Key.Year * 100 + g.Key.Month
                })
                .OrderBy(x => x.SortKey).ToListAsync();

            var roomTypeStats = await _context.Chitietdatphongs
                .Include(ct => ct.MaPhongNavigation.MaBienThePhongNavigation.MaLpNavigation)
                .Where(ct => ct.NgayNhan >= start && ct.NgayNhan <= end)
                .GroupBy(ct => ct.MaPhongNavigation.MaBienThePhongNavigation.MaLpNavigation.TenLoai)
                .Select(g => new { Label = g.Key ?? "Khác", Count = g.Count() }).ToListAsync();

            var query = _context.Datphongs
                .Include(dp => dp.MaKhNavigation)
                .Include(dp => dp.Hoadons)
                .Where(dp => dp.NgayDat >= start && dp.NgayDat <= end)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchCode))
            {
                var clean = searchCode.ToUpper().Replace("#BK", "").Replace("HD", "").Trim();
                if (int.TryParse(clean, out int id))
                    query = query.Where(dp => dp.MaDatPhong == id || dp.Hoadons.Any(h => h.MaHd == id));
            }

            if (!string.IsNullOrEmpty(status) && status != "all")
                query = query.Where(dp => dp.TrangThai == status);

            var recentOrders = await query.OrderByDescending(dp => dp.NgayDat).Take(20)
                .Select(dp => new {
                    maDatPhong = dp.MaDatPhong,
                    maHd = dp.Hoadons.Select(h => h.MaHd).FirstOrDefault(),
                    tenKhach = dp.MaKhNavigation.HoVaTen,
                    ngayDat = dp.NgayDat,
                    tongTien = dp.Hoadons.Select(h => h.TongTienPhaiTra).FirstOrDefault() ?? dp.TongTienDuKien,
                    trangThai = dp.TrangThai
                }).ToListAsync();

            return Ok(new { summary = new { totalRevenue, totalOrders = await query.CountAsync() }, revenueChart, roomTypeStats, recentOrders });
        }

        // API CHI TIẾT ĐƠN HÀNG (Dùng cho Modal)
        [HttpGet("order-detail/{id}")]
        public async Task<IActionResult> GetOrderDetail(int id)
        {
            var data = await _context.Datphongs
                .Include(dp => dp.MaKhNavigation)
                .Include(dp => dp.Hoadons)
                .Include(dp => dp.Chitietdatphongs)
                    .ThenInclude(ct => ct.MaPhongNavigation.MaBienThePhongNavigation.MaLpNavigation)
                .Where(dp => dp.MaDatPhong == id)
                .Select(dp => new {
                    maDatPhong = dp.MaDatPhong,
                    hoVaTen = dp.MaKhNavigation.HoVaTen,
                    sdt = dp.MaKhNavigation.Sdt, // Sử dụng Sdt theo Model của bạn
                    tenLoai = dp.Chitietdatphongs.Select(ct => ct.MaPhongNavigation.MaBienThePhongNavigation.MaLpNavigation.TenLoai).FirstOrDefault() ?? "N/A",
                    soDem = dp.Chitietdatphongs.Count(),
                    tongTienPhaiTra = dp.Hoadons.Select(h => h.TongTienPhaiTra).FirstOrDefault() ?? dp.TongTienDuKien,
                    tongTienGoc = dp.TongTienDuKien,
                    soTienGiam = (dp.TongTienDuKien ?? 0) - (dp.Hoadons.Select(h => h.TongTienPhaiTra).FirstOrDefault() ?? dp.TongTienDuKien ?? 0)
                })
                .FirstOrDefaultAsync();

            if (data == null) return NotFound();
            return Ok(data);
        }
    }
}