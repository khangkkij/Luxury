using Luxury.Admin.ViewModel;
using Luxury.Helpers;
using Luxury.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Luxury.Admin.Controllers
{
    [Authorize]
    [Route("api/admin/[controller]")]
    [Route("api/nhanvien/[controller]")]
    [ApiController]
    public class CheckInOutController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;


        private const string TrangThaiChoNhanPhong = "ChoNhanPhong";
        private const string TrangThaiDangO = "DangO";
        private const string TrangThaiTraPhong = "TraPhong";
        private const string TrangThaiDaHuy = "DaHuy";
        private const string TrangThaiDaThanhToan = "DaThanhToan";
        private const string TrangThaiChoXacNhan = "ChoXacNhan";
        private const string LoaiThanhToan = "ThanhToan";
        private const string LoaiTamUng = "TamUng";

        public CheckInOutController(LuxuryHotelContext context, IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _configuration = configuration;
            _scopeFactory = scopeFactory; // Gán biến
        }


        // 1. Thay thế hàm GetBookingsReadyForCheckIn cũ bằng hàm này
        [HttpGet("bookings-ready")]
        public IActionResult GetBookingsReadyForCheckIn()
        {
            var normalizedReadyStatus = TrangThaiChoNhanPhong.ToLowerInvariant();

            // Thêm .Trim() vào đây để tìm kiếm chính xác hơn
            var raw = _context.Datphongs
                .Include(x => x.MaKhNavigation)
                .Include(x => x.Chitietdatphongs)
                .Where(x => ((x.TrangThai ?? string.Empty).Trim().ToLower()) == normalizedReadyStatus)
                .OrderByDescending(x => x.NgayDat)
                .ToList();

            var bookings = raw.Select(x =>
            {
                var pendingRooms = x.Chitietdatphongs.Count(ct => ct.TrangThai == true && ct.MaPhong == null);
                var roomsQuery = _context.Phongs
                    .Include(p => p.MaBienThePhongNavigation)
                    .Where(p => p.TrangThai == 1);

                if (x.MaBienThePhong.HasValue)
                {
                    roomsQuery = roomsQuery.Where(p => p.MaBienThePhong == x.MaBienThePhong);
                }

                var availableRooms = roomsQuery.Select(p => new
                {
                    maPhong = p.MaPhong,
                    soPhong = p.SoPhong,
                    maBienThePhong = p.MaBienThePhong,
                    tenBienThe = p.MaBienThePhongNavigation != null ? p.MaBienThePhongNavigation.TenBienThe : null
                }).ToList();

                var chiTiet = x.Chitietdatphongs.FirstOrDefault();

                // --- MỚI: Tính tổng tiền khách đã cọc/thanh toán trước ---
                var daThanhToan = CalculateEffectivePrepaid(x.MaDatPhong);
                // --------------------------------------------------------

                return new
                {
                    maDatPhong = x.MaDatPhong,
                    tenKhach = x.MaKhNavigation?.HoVaTen,
                    sdt = x.MaKhNavigation?.Sdt,
                    ngayNhan = chiTiet?.NgayNhan,
                    ngayTra = chiTiet?.NgayTra,
                    soPhongCanGan = pendingRooms,
                    phongTrong = availableRooms,
                    // Trả về số tiền đã cọc để hiển thị lên Vue
                    daThanhToan = daThanhToan,
                    // Fallback key để tương thích nhiều bản frontend
                    tienCoc = daThanhToan,
                    tongTienDuKien = x.TongTienDuKien,
                    chiTietDatPhongs = x.Chitietdatphongs
                        .Where(ct => ct.TrangThai == true)
                        .Select(ct => new
                        {
                            maCtdp = ct.MaCtdp,
                            maPhong = ct.MaPhong,
                            ngayNhan = ct.NgayNhan,
                            ngayTra = ct.NgayTra
                        })
                        .ToList()
                };
            }).ToList();

            return Ok(bookings);
        }

        // 2. Thay thế hàm CheckIn cũ bằng hàm này (Đã fix lỗi string space)
        [HttpPost("check-in")]
        public IActionResult CheckIn([FromBody] CheckInRequest request)
        {
            if (request.MaDatPhong <= 0 || request.MaPhong <= 0)
            {
                return BadRequest("MaDatPhong hoặc MaPhong không hợp lệ");
            }

            var datPhong = _context.Datphongs
                .Include(x => x.Chitietdatphongs)
                .FirstOrDefault(x => x.MaDatPhong == request.MaDatPhong);

            if (datPhong == null)
            {
                return NotFound("Không tìm thấy đặt phòng");
            }

            // --- FIX LỖI Ở ĐÂY: Dùng Trim() và so sánh không phân biệt hoa thường ---
            var trangThaiHienTai = (datPhong.TrangThai ?? "").Trim();
            bool isReady = string.Equals(trangThaiHienTai, TrangThaiChoNhanPhong, StringComparison.OrdinalIgnoreCase);
            bool isStaying = string.Equals(trangThaiHienTai, TrangThaiDangO, StringComparison.OrdinalIgnoreCase);

            if (!isReady && !isStaying)
            {
                return BadRequest($"Đặt phòng chưa sẵn sàng check-in (Trạng thái: {trangThaiHienTai})");
            }
            // ------------------------------------------------------------------------

            var phong = _context.Phongs.FirstOrDefault(x => x.MaPhong == request.MaPhong);
            if (phong == null)
            {
                return NotFound("Không tìm thấy phòng");
            }

            if (phong.TrangThai != 1)
            {
                return BadRequest("Phòng không trống hoặc chưa dọn dẹp");
            }

            var chiTiet = request.MaCtdp != null
                ? datPhong.Chitietdatphongs.FirstOrDefault(x => x.MaCtdp == request.MaCtdp)
                : datPhong.Chitietdatphongs.FirstOrDefault(x => x.TrangThai == true && x.MaPhong == null);

            if (chiTiet == null)
            {
                return BadRequest("Không còn chi tiết đặt phòng để gán phòng");
            }

            chiTiet.MaPhong = phong.MaPhong;
            chiTiet.TrangThai = true;
            chiTiet.NgayNhan = DateTime.Now;
            datPhong.TrangThai = TrangThaiDangO;
            datPhong.MaNv = request.MaNv ?? datPhong.MaNv;
            phong.TrangThai = 2;

            if ((request.DepositAmount ?? 0) > 0 && !IsOnlineMethod(request.DepositMethod))
            {
                var tongDaDongTruoc = _context.Thanhtoans
                    .Where(t => t.MaDatPhong == datPhong.MaDatPhong &&
                                t.TrangThai == TrangThaiDaThanhToan &&
                                t.MaHd == null &&
                                t.LoaiThanhToan != "HoanTien")
                    .Sum(t => t.SoTien ?? 0);

                var soTienThuThem = Math.Max(0, (request.DepositAmount ?? 0) - tongDaDongTruoc);
                if (soTienThuThem > 0)
                {
                    var tamUng = new Thanhtoan
                    {
                        MaDatPhong = datPhong.MaDatPhong,
                        LoaiThanhToan = LoaiTamUng,
                        SoTien = soTienThuThem,
                        HinhThucThanhToan = request.DepositMethod ?? "cash",
                        TrangThai = TrangThaiDaThanhToan
                    };
                    _context.Thanhtoans.Add(tamUng);
                }
            }

            _context.SaveChanges();

            return Ok(new
            {
                message = "Check-in thành công",
                maDatPhong = datPhong.MaDatPhong,
                maPhong = phong.MaPhong
            });
        }

        [HttpPost("walk-in")]
        public IActionResult WalkIn([FromBody] WalkInRequest request)
        {
            if (request.MaPhong <= 0)
            {
                return BadRequest("MaPhong không hợp lệ");
            }

            if (request.NgayTra.Date <= request.NgayNhan.Date)
            {
                return BadRequest("Ngày trả phải sau ngày nhận");
            }

            var phong = _context.Phongs
                .Include(p => p.MaBienThePhongNavigation)
                    .ThenInclude(bt => bt.Thietlapgia)
                .FirstOrDefault(p => p.MaPhong == request.MaPhong);

            if (phong == null)
            {
                return NotFound("Không tìm thấy phòng");
            }

            if (phong.TrangThai != 1)
            {
                return BadRequest("Phòng không trống hoặc chưa dọn dẹp");
            }

            var khach = FindOrCreateCustomer(request);

            var giaMacDinh = phong.MaBienThePhongNavigation?
                .Thietlapgia
                .Where(x => x.MacDinh == true)
                .Select(x => x.Gia)
                .FirstOrDefault() ?? 0;

            var soDem = (request.NgayTra.Date - request.NgayNhan.Date).Days;
            if (soDem <= 0)
            {
                return BadRequest("Số đêm không hợp lệ");
            }

            var tongTienDuKien = giaMacDinh * soDem;

            var datPhong = new Datphong
            {
                MaKh = khach.MaKh,
                MaBienThePhong = phong.MaBienThePhong,
                MaNv = request.MaNv,
                NgayDat = DateTime.Now,
                TongTienDuKien = tongTienDuKien,
                TrangThai = TrangThaiDangO
            };
            _context.Datphongs.Add(datPhong);
            _context.SaveChanges();

            var chiTiet = new Chitietdatphong
            {
                MaDatPhong = datPhong.MaDatPhong,
                MaPhong = phong.MaPhong,
                GiaDat = giaMacDinh,
                NgayNhan = DateTime.Now,
                NgayTra = request.NgayTra,
                SoNguoiLon = 1,
                SoTreEm = 0,
                TrangThai = true
            };
            _context.Chitietdatphongs.Add(chiTiet);

            phong.TrangThai = 2;

            var hoaDon = new Hoadon
            {
                MaDatPhong = datPhong.MaDatPhong,
                TongTienGoc = tongTienDuKien,
                SoTienGiam = 0,
                TongTienPhaiTra = tongTienDuKien,
                NgayTao = DateTime.Now
            };
            _context.Hoadons.Add(hoaDon);

            if ((request.DepositAmount ?? 0) > 0 && !IsOnlineMethod(request.DepositMethod))
            {
                var tongDaDongTruoc = _context.Thanhtoans
                    .Where(t => t.MaDatPhong == datPhong.MaDatPhong &&
                                t.TrangThai == TrangThaiDaThanhToan &&
                                t.MaHd == null &&
                                t.LoaiThanhToan != "HoanTien")
                    .Sum(t => t.SoTien ?? 0);

                var soTienThuThem = Math.Max(0, (request.DepositAmount ?? 0) - tongDaDongTruoc);
                if (soTienThuThem > 0)
                {
                    var tamUng = new Thanhtoan
                    {
                        MaDatPhong = datPhong.MaDatPhong,
                        LoaiThanhToan = LoaiTamUng,
                        SoTien = soTienThuThem,
                        HinhThucThanhToan = request.DepositMethod ?? "cash",
                        TrangThai = TrangThaiDaThanhToan
                    };
                    _context.Thanhtoans.Add(tamUng);
                }
            }

            _context.SaveChanges();

            return Ok(new
            {
                message = "Tạo walk-in & check-in thành công",
                maDatPhong = datPhong.MaDatPhong,
                maPhong = phong.MaPhong
            });
        }

        [HttpGet("rooms-occupied")]
        public IActionResult GetOccupiedRooms()
        {
            var raw = _context.Phongs
                .Include(p => p.Chitietdatphongs)
                    .ThenInclude(ct => ct.MaDatPhongNavigation)
                        .ThenInclude(dp => dp.MaKhNavigation)
                .Where(p => p.TrangThai == 2)
                .ToList();

            var rooms = raw.Select(p =>
            {
                var booking = p.Chitietdatphongs.FirstOrDefault(ct => ct.TrangThai == true);
                return new
                {
                    maPhong = p.MaPhong,
                    soPhong = p.SoPhong,
                    maDatPhong = booking?.MaDatPhong,
                    tenKhach = booking?.MaDatPhongNavigation?.MaKhNavigation?.HoVaTen,
                    sdt = booking?.MaDatPhongNavigation?.MaKhNavigation?.Sdt,
                    ngayNhan = booking?.NgayNhan,
                    ngayTra = booking?.NgayTra
                };
            }).ToList();

            return Ok(rooms);
        }

        [HttpGet("rooms-available")]
        public IActionResult GetAvailableRooms([FromQuery] int? maBienThePhong = null)
        {
            var query = _context.Phongs
                .Include(p => p.MaBienThePhongNavigation)
                .Where(p => p.TrangThai == 1);

            if (maBienThePhong.HasValue)
            {
                query = query.Where(p => p.MaBienThePhong == maBienThePhong.Value);
            }

            var rooms = query
                .Select(p => new
                {
                    maPhong = p.MaPhong,
                    soPhong = p.SoPhong,
                    maBienThePhong = p.MaBienThePhong,
                    tenBienThe = p.MaBienThePhongNavigation != null ? p.MaBienThePhongNavigation.TenBienThe : null
                })
                .ToList();

            return Ok(rooms);
        }

        [HttpGet("checkout-detail/{maDatPhong}")]
        public IActionResult GetCheckOutDetail(int maDatPhong)
        {
            if (maDatPhong <= 0) return BadRequest("MaDatPhong không hợp lệ");

            var datPhong = _context.Datphongs
                .Include(x => x.MaKhNavigation)
                .Include(x => x.Chitietdatphongs).ThenInclude(ct => ct.MaPhongNavigation).ThenInclude(p => p.MaBienThePhongNavigation).ThenInclude(bt => bt.Thietlapgia)
                .Include(x => x.Chitietdatphongs).ThenInclude(ct => ct.SudungDichvus).ThenInclude(sd => sd.MaDichVuNavigation)
                .FirstOrDefault(x => x.MaDatPhong == maDatPhong);

            if (datPhong == null) return NotFound("Không tìm thấy đặt phòng");

            var chiTiet = datPhong.Chitietdatphongs.FirstOrDefault(ct => ct.TrangThai == true);
            if (chiTiet == null) return NotFound("Không có chi tiết đặt phòng đang ở");

            var giaMacDinh = chiTiet.MaPhongNavigation?.MaBienThePhongNavigation?.Thietlapgia
                .Where(x => x.MacDinh == true).Select(x => x.Gia).FirstOrDefault();

            var giaDat = chiTiet.GiaDat ?? giaMacDinh ?? 0;
            var ngayNhan = chiTiet.NgayNhan ?? DateTime.Now;
            var ngayTra = chiTiet.NgayTra ?? DateTime.Now;
            var soDem = Math.Max(1, (ngayTra.Date - ngayNhan.Date).Days);

            // --- SỬA: Lấy tổng tiền đã thanh toán (bất kể là Cọc hay Thanh toán 100%) ---
            // Loại trừ khoản hoàn tiền cũ
            var daThanhToan = CalculateEffectivePrepaid(maDatPhong);
            // -------------------------------------------------------------------------

            return Ok(new
            {
                maDatPhong = datPhong.MaDatPhong,
                maPhong = chiTiet.MaPhong,
                soPhong = chiTiet.MaPhongNavigation?.SoPhong,
                tenKhach = datPhong.MaKhNavigation?.HoVaTen,
                sdt = datPhong.MaKhNavigation?.Sdt,
                ngayNhan = ngayNhan,
                ngayTra = ngayTra,
                soDem = soDem,
                giaDat = giaDat,
                tienCoc = daThanhToan, // Trả về tổng tiền đã đóng vào biến tienCoc cho FE dùng
                dichVu = chiTiet.SudungDichvus.Select(sd => new
                {
                    maDichVu = sd.MaDichVu,
                    tenDichVu = sd.MaDichVuNavigation?.TenDichVu,
                    soLuong = sd.SoLuong,
                    donGia = sd.MaDichVuNavigation?.Gia,
                    thanhTien = sd.ThanhTien
                }).ToList()
            });
        }

        [HttpPost("preview-voucher")]
        public IActionResult PreviewVoucher([FromBody] VoucherPreviewRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.VoucherCode))
            {
                return Ok(new { soTienGiam = 0, message = "Chưa nhập voucher." });
            }

            var discount = PreviewVoucherDiscount(request.VoucherCode, request.TongTienGoc, out var message);
            return Ok(new { soTienGiam = discount, message });
        }
        private int LayMaNvTuToken()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return 0;
            return int.Parse(userIdClaim.Value);
        }
        [HttpPost("check-out")]
        public IActionResult CheckOut([FromBody] CheckOutRequest request)
        {
            if (request.MaDatPhong <= 0) return BadRequest("MaDatPhong không hợp lệ");

            int maNvHienTai = LayMaNvTuToken();

            var datPhong = _context.Datphongs
                .Include(x => x.Chitietdatphongs).ThenInclude(ct => ct.MaPhongNavigation)
                .Include(x => x.Chitietdatphongs).ThenInclude(ct => ct.SudungDichvus).ThenInclude(sd => sd.MaDichVuNavigation)
                .Include(x => x.Hoadons)
                .FirstOrDefault(x => x.MaDatPhong == request.MaDatPhong);

            if (datPhong == null) return NotFound("Không tìm thấy đặt phòng");
            datPhong.MaNv = maNvHienTai != 0 ? maNvHienTai : datPhong.MaNv;
            var chiTiet = ResolveCheckOutDetail(datPhong, request);
            if (chiTiet == null) return BadRequest("Không tìm thấy chi tiết đặt phòng phù hợp");
            if (chiTiet.MaPhongNavigation == null) return BadRequest("Chi tiết đặt phòng chưa được gán phòng");

            // 1. Cập nhật dịch vụ phụ
            if (request.ExtraServices != null && request.ExtraServices.Count > 0)
            {
                foreach (var item in request.ExtraServices)
                {
                    if (item.MaDichVu <= 0 || item.SoLuong <= 0) continue;
                    var dichVu = _context.Dichvus.FirstOrDefault(x => x.MaDichVu == item.MaDichVu && x.TrangThai == true);
                    if (dichVu == null) return BadRequest($"Không tìm thấy dịch vụ {item.MaDichVu}");

                    chiTiet.SudungDichvus.Add(new SudungDichvu
                    {
                        MaCtdp = chiTiet.MaCtdp,
                        MaDichVu = dichVu.MaDichVu,
                        SoLuong = item.SoLuong,
                        NgaySuDung = DateTime.Now,
                        ThanhTien = (dichVu.Gia ?? 0) * item.SoLuong
                    });
                }
                _context.SaveChanges();
            }

            // 2. Tính toán tiền
            var giaDat = chiTiet.GiaDat ?? 0;
            var ngayNhan = chiTiet.NgayNhan ?? DateTime.Now;
            var thoiGianTra = request.ThoiGianTraThucTe ?? DateTime.Now;
            // Đồng bộ với frontend: tiền phòng tính theo ngày trả dự kiến, giờ trả thực tế chỉ để tính phụ thu trễ.
            var ngayTraTinhTien = chiTiet.NgayTra ?? thoiGianTra;
            var soDem = Math.Max(1, (ngayTraTinhTien.Date - ngayNhan.Date).Days);

            var tienPhong = giaDat * soDem;
            var phuThuGio = thoiGianTra.TimeOfDay > new TimeSpan(12, 0, 0) ? Math.Round(giaDat * 0.3m) : 0;
            var tienDichVu = chiTiet.SudungDichvus.Sum(sd => sd.ThanhTien ?? ((sd.MaDichVuNavigation?.Gia ?? 0) * (sd.SoLuong ?? 0)));
            var phuThu = (request.ExtraFee ?? 0) + phuThuGio;

            var tongTienGoc = tienPhong + tienDichVu + phuThu;

            // 2. Trừ Voucher
            var soTienGiam = ApplyVoucher(datPhong, request.VoucherCode, tongTienGoc, out var voucherMessage);

            // 3. Lấy TỔNG TIỀN KHÁCH ĐÃ ĐƯA (Cọc + TT trước)
            var daThanhToan = CalculateEffectivePrepaid(datPhong.MaDatPhong);

            var suDungTienCoc = request.SuDungTienCoc ?? true;

            // Chỉ cho phép cọc/tiền đã thanh toán trước trừ TỐI ĐA bằng tiền phòng.
            // Phần dịch vụ & phụ thu sẽ luôn phải thanh toán riêng.
            // ĐOẠN MỚI: Trừ thẳng vào tổng hóa đơn
            decimal tienDaDongApDung = 0;
            if (suDungTienCoc && daThanhToan > 0)
            {
                tienDaDongApDung = daThanhToan; // Lấy toàn bộ số tiền khách đã đóng
            }

            // Tổng hóa đơn (gồm phòng + DV + phụ thu) - (Giảm giá + Toàn bộ tiền đã đóng)
            var tongTienPhaiTra = tongTienGoc - soTienGiam - tienDaDongApDung;

            // --- TÍNH TIỀN THỪA SHOW RA GIAO DIỆN (KHÔNG LƯU DB) ---
            decimal soTienHoan = 0;

            // Nếu tiền phải trả bị ÂM (tức là cọc nhiều hơn tổng tiền)
            if (tongTienPhaiTra < 0)
            {
                soTienHoan = Math.Abs(tongTienPhaiTra); // Bắt lấy số tiền thừa để gửi lên Frontend
                tongTienPhaiTra = 0;                    // Đưa số cần thu thêm về 0 để không tạo giao dịch thu
            }
            // --------------------------------------------------------

            var hoaDon = datPhong.Hoadons.FirstOrDefault();
            if (hoaDon == null)
            {
                hoaDon = new Hoadon { MaDatPhong = datPhong.MaDatPhong, NgayTao = DateTime.Now };
                _context.Hoadons.Add(hoaDon);
                _context.SaveChanges();
            }

            hoaDon.TongTienGoc = tongTienGoc;
            hoaDon.SoTienGiam = soTienGiam;

            // LƯU Ý: Lưu tổng giá trị thực tế của tờ hóa đơn (Chưa trừ cọc) để in ra không bị sai lệch 0đ
            hoaDon.TongTienPhaiTra = tongTienGoc - soTienGiam;

            datPhong.TrangThai = datPhong.Chitietdatphongs.Any(ct => ct.TrangThai == true && ct.MaCtdp != chiTiet.MaCtdp)
                ? TrangThaiDangO : TrangThaiTraPhong;
            datPhong.MaNv = request.MaNv ?? datPhong.MaNv;

            chiTiet.TrangThai = false;
            // Đã comment dòng dưới để hóa đơn in đúng số đêm
            // chiTiet.NgayTra = thoiGianTra; 
            chiTiet.MaPhongNavigation.TrangThai = 3;

            // 4. Tạo thanh toán CHỈ KHI khách còn thiếu tiền
            if (tongTienPhaiTra > 0 && !IsOnlineMethod(request.HinhThucThanhToan))
            {
                var thanhToan = new Thanhtoan
                {
                    MaDatPhong = datPhong.MaDatPhong,
                    MaHd = hoaDon.MaHd,
                    LoaiThanhToan = LoaiThanhToan,
                    SoTien = tongTienPhaiTra, // Đây là số tiền thu thêm
                    HinhThucThanhToan = request.HinhThucThanhToan ?? "cash",
                    TrangThai = TrangThaiDaThanhToan
                };
                _context.Thanhtoans.Add(thanhToan);
            }

            _context.SaveChanges();
            // =============== THÊM MỚI ĐOẠN NÀY ===============
            // Lấy ID phòng để cập nhật ngầm
            int maPhongCanDonDep = chiTiet.MaPhongNavigation.MaPhong;

            // Chạy ngầm một Task không chờ (Fire-and-forget)
            _ = Task.Run(async () =>
            {
                // Đợi 30 phút (Nếu muốn test nhanh, bạn có thể đổi thành TimeSpan.FromSeconds(30))
                await Task.Delay(TimeSpan.FromMinutes(30));

                // Phải tạo Scope mới vì Request hiện tại đã kết thúc
                using (var scope = _scopeFactory.CreateScope())
                {
                    var scopedContext = scope.ServiceProvider.GetRequiredService<LuxuryHotelContext>();
                    var phong = await scopedContext.Phongs.FirstOrDefaultAsync(p => p.MaPhong == maPhongCanDonDep);

                    // Kiểm tra nếu phòng vẫn đang ở trạng thái 3 (Đang dọn dẹp) thì mới chuyển về 1 (Trống)
                    if (phong != null && phong.TrangThai == 3)
                    {
                        phong.TrangThai = 1;
                        await scopedContext.SaveChangesAsync();
                    }
                }
            });
            return Ok(new
            {
                message = "Check-out thành công",
                maDatPhong = datPhong.MaDatPhong,
                maNvXacNhan = maNvHienTai,
                maHd = hoaDon.MaHd,
                tongTienGoc,
                soTienGiam,
                tienCoc = daThanhToan,
                tienCocApDung = tienDaDongApDung,
                tongTienPhaiTra = tongTienPhaiTra, // Trả về số Lễ tân cần thu thêm
                soTienHoan = soTienHoan,           // Trả về số Tiền thừa thối lại
                voucherMessage
            });
        }

        [HttpPost("checkout-payment")]
        public async Task<IActionResult> CheckoutPayment([FromBody] CheckoutPaymentRequest request)
        {
            if (request.MaDatPhong <= 0)
            {
                return BadRequest("MaDatPhong không hợp lệ");
            }

            if (request.SoTien <= 0)
            {
                return BadRequest("Số tiền không hợp lệ");
            }

            var method = NormalizePaymentMethod(request.HinhThucThanhToan);
            if (string.IsNullOrWhiteSpace(method))
            {
                return BadRequest("Thiếu HinhThucThanhToan.");
            }

            var datPhongExists = _context.Datphongs.Any(x => x.MaDatPhong == request.MaDatPhong);
            if (!datPhongExists)
            {
                return NotFound("Không tìm thấy đặt phòng");
            }

            var hoaDon = _context.Hoadons
                .Where(x => x.MaDatPhong == request.MaDatPhong)
                .OrderByDescending(x => x.NgayTao)
                .FirstOrDefault();

            var thanhToan = new Thanhtoan
            {
                MaDatPhong = request.MaDatPhong,
                MaHd = hoaDon?.MaHd,
                LoaiThanhToan = LoaiThanhToan,
                SoTien = request.SoTien,
                HinhThucThanhToan = request.HinhThucThanhToan,
                TrangThai = TrangThaiChoXacNhan
            };

            _context.Thanhtoans.Add(thanhToan);
            _context.SaveChanges();

            if (IsOnlineMethod(method))
            {
                try
                {
                    var redirectUrl = await CreatePaymentRedirectUrl(method, thanhToan.MaTt, request.SoTien);
                    if (string.IsNullOrEmpty(redirectUrl))
                    {
                        return StatusCode(400, $"Không tạo được URL thanh toán ({method}).");
                    }
                    return Ok(new { message = "Tạo thanh toán thành công", maTt = thanhToan.MaTt, redirectUrl });
                }
                catch (Exception ex)
                {
                    return StatusCode(400, ex.Message);
                }
            }

            thanhToan.TrangThai = TrangThaiDaThanhToan;
            _context.SaveChanges();

            return Ok(new { message = "Tạo thanh toán thành công", maTt = thanhToan.MaTt });
        }

        [HttpPost("checkin-payment")]
        public async Task<IActionResult> CheckInPayment([FromBody] CheckoutPaymentRequest request)
        {
            if (request.MaDatPhong <= 0)
            {
                return BadRequest("MaDatPhong không hợp lệ");
            }

            if (request.SoTien <= 0)
            {
                return BadRequest("Số tiền không hợp lệ");
            }

            var method = NormalizePaymentMethod(request.HinhThucThanhToan);
            if (string.IsNullOrWhiteSpace(method))
            {
                return BadRequest("Thiếu HinhThucThanhToan.");
            }

            var datPhongExists = _context.Datphongs.Any(x => x.MaDatPhong == request.MaDatPhong);
            if (!datPhongExists)
            {
                return NotFound("Không tìm thấy đặt phòng");
            }

            var thanhToan = new Thanhtoan
            {
                MaDatPhong = request.MaDatPhong,
                LoaiThanhToan = LoaiTamUng,
                SoTien = request.SoTien,
                HinhThucThanhToan = request.HinhThucThanhToan,
                TrangThai = TrangThaiChoXacNhan
            };

            _context.Thanhtoans.Add(thanhToan);
            _context.SaveChanges();

            if (IsOnlineMethod(method))
            {
                try
                {
                    var redirectUrl = await CreatePaymentRedirectUrl(method, thanhToan.MaTt, request.SoTien);
                    if (string.IsNullOrEmpty(redirectUrl))
                    {
                        return StatusCode(400, $"Không tạo được URL thanh toán ({method}).");
                    }
                    return Ok(new { message = "Tạo thanh toán thành công", maTt = thanhToan.MaTt, redirectUrl });
                }
                catch (Exception ex)
                {
                    return StatusCode(400, ex.Message);
                }
            }

            thanhToan.TrangThai = TrangThaiDaThanhToan;
            _context.SaveChanges();

            return Ok(new { message = "Tạo thanh toán thành công", maTt = thanhToan.MaTt });
        }

        [HttpGet("vnpay-callback")]
        public IActionResult VnPayCallback()
        {
            var response = _configuration.GetSection("VnPay");
            var vnPayData = Request.Query;
            var vnPayLibrary = new VnPayLibrary();
            foreach (var s in vnPayData)
            {
                if (!string.IsNullOrEmpty(s.Key) && s.Key.StartsWith("vnp_"))
                {
                    vnPayLibrary.AddResponseData(s.Key, s.Value);
                }
            }

            string vnpSecureHash = vnPayData["vnp_SecureHash"];
            bool checkSignature = vnPayLibrary.ValidateSignature(vnpSecureHash, response["HashSecret"]);
            if (!checkSignature)
            {
                return Redirect(BuildFrontendUrl("/admin/check-out?status=fail"));
            }

            string responseCode = vnPayLibrary.GetResponseData("vnp_ResponseCode");
            string txnRef = vnPayLibrary.GetResponseData("vnp_TxnRef");
            if (responseCode == "00" && int.TryParse(txnRef, out var maTt))
            {
                MarkPaymentSuccess(maTt);
                return Redirect(BuildFrontendUrl("/admin/check-out?status=success"));
            }

            return Redirect(BuildFrontendUrl("/admin/check-out?status=fail"));
        }

        [HttpGet("momo-callback")]
        public IActionResult MomoCallback()
        {
            var resultCode = Request.Query["resultCode"].ToString();
            var orderId = Request.Query["orderId"].ToString();

            if (resultCode == "0" && TryParseMomoOrderId(orderId, out var maTt))
            {
                MarkPaymentSuccess(maTt);
                return Redirect(BuildFrontendUrl("/admin/check-out?status=success"));
            }

            return Redirect(BuildFrontendUrl("/admin/check-out?status=fail"));
        }

        [HttpPost("momo-ipn")]
        public async Task<IActionResult> MomoIpn()
        {
            var momoConfig = _configuration.GetSection("Momo");
            string accessKey = momoConfig["AccessKey"];
            string secretKey = momoConfig["SecretKey"];

            MomoNotifyRequest payload = null;
            try
            {
                payload = await JsonSerializer.DeserializeAsync<MomoNotifyRequest>(
                    Request.Body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch
            {
                return BadRequest(new { resultCode = 1, message = "Invalid payload" });
            }

            if (payload == null || string.IsNullOrEmpty(payload.Signature))
            {
                return BadRequest(new { resultCode = 1, message = "Missing signature" });
            }

            string rawHash = BuildMomoNotifyRawHash(payload, accessKey);
            string expectedSignature = MomoLibrary.ComputeHmacSha256(rawHash, secretKey);
            if (!string.Equals(expectedSignature, payload.Signature, StringComparison.OrdinalIgnoreCase))
            {
                return new JsonResult(new { resultCode = 1, message = "Invalid signature" });
            }

            if (payload.ResultCode == "0" && TryParseMomoOrderId(payload.OrderId, out var maTt))
            {
                MarkPaymentSuccess(maTt);
            }

            return new JsonResult(new { resultCode = 0, message = "OK" });
        }

        private Chitietdatphong? ResolveCheckOutDetail(Datphong datPhong, CheckOutRequest request)
        {
            if (request.MaCtdp != null)
            {
                return datPhong.Chitietdatphongs.FirstOrDefault(ct => ct.MaCtdp == request.MaCtdp);
            }

            if (request.MaPhong != null)
            {
                return datPhong.Chitietdatphongs.FirstOrDefault(ct => ct.MaPhong == request.MaPhong && ct.TrangThai == true);
            }

            return datPhong.Chitietdatphongs.FirstOrDefault(ct => ct.TrangThai == true);
        }

        private decimal CalculateEffectivePrepaid(int? maDatPhong)
        {
            if (maDatPhong == null || maDatPhong <= 0)
            {
                return 0;
            }

            var prepaids = _context.Thanhtoans
                .Where(x => x.MaDatPhong == maDatPhong &&
                            x.TrangThai == TrangThaiDaThanhToan &&
                            x.LoaiThanhToan != "HoanTien" &&
                            x.MaHd == null)
                .Select(x => new
                {
                    Loai = (x.LoaiThanhToan ?? string.Empty).Trim(),
                    SoTien = x.SoTien ?? 0
                })
                .ToList();

            if (prepaids.Count == 2)
            {
                var datCoc = prepaids.FirstOrDefault(x => string.Equals(x.Loai, "DatCoc", StringComparison.OrdinalIgnoreCase));
                var tamUng = prepaids.FirstOrDefault(x => string.Equals(x.Loai, LoaiTamUng, StringComparison.OrdinalIgnoreCase));
                if (datCoc != null && tamUng != null && datCoc.SoTien == tamUng.SoTien)
                {
                    // Dữ liệu cũ bị nhân đôi cọc ở bước check-in: chỉ tính 1 lần.
                    return datCoc.SoTien;
                }
            }

            return prepaids.Sum(x => x.SoTien);
        }

        private decimal ApplyVoucher(Datphong datPhong, string? voucherCode, decimal tongTienGoc, out string? message)
        {
            message = null;
            if (string.IsNullOrWhiteSpace(voucherCode))
            {
                return 0;
            }

            var code = voucherCode.Trim().ToUpperInvariant();
            var voucher = _context.Vouchers.FirstOrDefault(v => v.MaCode == code);
            if (voucher == null || voucher.TrangThai != true)
            {
                message = "Voucher không hợp lệ.";
                return 0;
            }

            if (voucher.NgayKetThuc.HasValue && voucher.NgayKetThuc.Value < DateTime.Now)
            {
                message = "Voucher đã hết hạn.";
                return 0;
            }

            if (voucher.SoLuongDaDung.HasValue && voucher.SoLuong.HasValue &&
                voucher.SoLuongDaDung.Value >= voucher.SoLuong.Value)
            {
                message = "Voucher đã hết lượt sử dụng.";
                return 0;
            }

            if (voucher.GiaToiThieu.HasValue && tongTienGoc < voucher.GiaToiThieu.Value)
            {
                message = "Chưa đạt giá trị tối thiểu để áp dụng voucher.";
                return 0;
            }

            datPhong.MaCode = voucher.MaCode;
            voucher.SoLuongDaDung = (voucher.SoLuongDaDung ?? 0) + 1;

            if (voucher.PhanTramGiam.HasValue && voucher.PhanTramGiam.Value > 0)
            {
                var discount = tongTienGoc * voucher.PhanTramGiam.Value / 100m;
                message = $"Áp dụng voucher {voucher.MaCode}.";
                return discount;
            }

            if (voucher.SoTienGiam.HasValue && voucher.SoTienGiam.Value > 0)
            {
                message = $"Áp dụng voucher {voucher.MaCode}.";
                return voucher.SoTienGiam.Value;
            }

            message = "Voucher không có giá trị giảm.";
            return 0;
        }

        private decimal PreviewVoucherDiscount(string voucherCode, decimal tongTienGoc, out string message)
        {
            message = "Voucher không hợp lệ.";
            var code = voucherCode.Trim().ToUpperInvariant();
            var voucher = _context.Vouchers.FirstOrDefault(v => v.MaCode == code);
            if (voucher == null || voucher.TrangThai != true)
            {
                return 0;
            }

            if (voucher.NgayKetThuc.HasValue && voucher.NgayKetThuc.Value < DateTime.Now)
            {
                message = "Voucher đã hết hạn.";
                return 0;
            }

            if (voucher.SoLuongDaDung.HasValue && voucher.SoLuong.HasValue &&
                voucher.SoLuongDaDung.Value >= voucher.SoLuong.Value)
            {
                message = "Voucher đã hết lượt sử dụng.";
                return 0;
            }

            if (voucher.GiaToiThieu.HasValue && tongTienGoc < voucher.GiaToiThieu.Value)
            {
                message = "Chưa đạt giá trị tối thiểu.";
                return 0;
            }

            if (voucher.PhanTramGiam.HasValue && voucher.PhanTramGiam.Value > 0)
            {
                message = $"Áp dụng voucher {voucher.MaCode}.";
                return tongTienGoc * voucher.PhanTramGiam.Value / 100m;
            }

            if (voucher.SoTienGiam.HasValue && voucher.SoTienGiam.Value > 0)
            {
                message = $"Áp dụng voucher {voucher.MaCode}.";
                return voucher.SoTienGiam.Value;
            }

            message = "Voucher không có giá trị giảm.";
            return 0;
        }

        private Khachhang FindOrCreateCustomer(WalkInRequest request)
        {
            var khach = _context.Khachhangs.FirstOrDefault(x =>
                (!string.IsNullOrWhiteSpace(request.Sdt) && x.Sdt == request.Sdt) ||
                (!string.IsNullOrWhiteSpace(request.Cccd) && x.Cccd == request.Cccd)
            );

            if (khach != null)
            {
                khach.HoVaTen = request.HoVaTen;
                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    khach.Email = request.Email;
                }
                if (!string.IsNullOrWhiteSpace(request.Cccd))
                {
                    khach.Cccd = request.Cccd;
                }
                if (!string.IsNullOrWhiteSpace(request.Sdt))
                {
                    khach.Sdt = request.Sdt;
                }
                if (!string.IsNullOrWhiteSpace(request.GioiTinh))
                {
                    khach.GioiTinh = request.GioiTinh;
                }
                if (khach.TrangThai == null)
                {
                    khach.TrangThai = true;
                }
                if (khach.NgayTao == null)
                {
                    khach.NgayTao = DateTime.Now;
                }
                return khach;
            }

            khach = new Khachhang
            {
                HoVaTen = request.HoVaTen,
                Email = request.Email,
                Cccd = request.Cccd,
                Sdt = request.Sdt,
                GioiTinh = request.GioiTinh,
                TrangThai = true,
                NgayTao = DateTime.Now
            };
            _context.Khachhangs.Add(khach);
            _context.SaveChanges();
            return khach;
        }

        private void MarkPaymentSuccess(int maTt)
        {
            var thanhToan = _context.Thanhtoans.FirstOrDefault(x => x.MaTt == maTt);
            if (thanhToan == null)
            {
                return;
            }

            if (thanhToan.TrangThai == TrangThaiDaThanhToan)
            {
                return;
            }

            thanhToan.TrangThai = TrangThaiDaThanhToan;
            _context.SaveChanges();
        }

        private string NormalizePaymentMethod(string? method)
        {
            return (method ?? string.Empty).Trim().ToLowerInvariant();
        }

        private bool IsOnlineMethod(string? method)
        {
            var normalized = NormalizePaymentMethod(method);
            return normalized == "vnpay" || normalized == "momo";
        }

        private async Task<string?> CreatePaymentRedirectUrl(string method, int maTt, decimal soTien)
        {
            if (method == "vnpay")
            {
                return CreateVnPayUrlInternal(maTt.ToString(), (double)soTien);
            }
            if (method == "momo")
            {
                var orderId = BuildMomoOrderId(maTt);
                return await CreateMomoUrlInternal(orderId, (long)Math.Round(soTien, 0));
            }
            return null;
        }

        private string BuildFrontendUrl(string pathAndQuery)
        {
            var baseUrl = _configuration["Frontend:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = "http://localhost:5173";
            }
            return baseUrl.TrimEnd('/') + pathAndQuery;
        }

        private string CreateVnPayUrlInternal(string orderRefId, double amount)
        {
            var vnPayHelper = new VnPayLibrary();
            var timeNow = DateTime.Now;
            var vnpTmnCode = _configuration["VnPay:TmnCode"];
            var vnpHashSecret = _configuration["VnPay:HashSecret"];
            var vnpUrl = _configuration["VnPay:BaseUrl"];
            var vnpReturnUrl = ResolveVnPayReturnUrl();

            vnPayHelper.AddRequestData("vnp_Version", _configuration["VnPay:Version"] ?? "2.1.0");
            vnPayHelper.AddRequestData("vnp_Command", _configuration["VnPay:Command"] ?? "pay");
            vnPayHelper.AddRequestData("vnp_TmnCode", vnpTmnCode);
            vnPayHelper.AddRequestData("vnp_Amount", ((long)Math.Round(amount, 0) * 100).ToString());
            vnPayHelper.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            vnPayHelper.AddRequestData("vnp_CurrCode", _configuration["VnPay:CurrCode"] ?? "VND");
            vnPayHelper.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(HttpContext));
            vnPayHelper.AddRequestData("vnp_Locale", _configuration["VnPay:Locale"] ?? "vn");
            vnPayHelper.AddRequestData("vnp_OrderInfo", "Thanh toan checkout " + orderRefId);
            vnPayHelper.AddRequestData("vnp_OrderType", "other");
            vnPayHelper.AddRequestData("vnp_ReturnUrl", vnpReturnUrl);
            vnPayHelper.AddRequestData("vnp_TxnRef", orderRefId);
            vnPayHelper.AddRequestData("vnp_SecureHashType", "HmacSHA512");

            return vnPayHelper.CreateRequestUrl(vnpUrl, vnpHashSecret);
        }

        private string ResolveVnPayReturnUrl()
        {
            var configured = _configuration["VnPay:ReturnUrl"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                if (Uri.TryCreate(configured, UriKind.Absolute, out var uri))
                {
                    var host = uri.Host?.ToLowerInvariant();
                    if (host != "localhost" && host != "127.0.0.1")
                    {
                        return configured;
                    }
                }
                else
                {
                    return configured;
                }
            }

            var request = HttpContext?.Request;
            if (request == null)
            {
                return configured ?? string.Empty;
            }

            return $"{request.Scheme}://{request.Host}/api/admin/CheckInOut/vnpay-callback";
        }

        private async Task<string?> CreateMomoUrlInternal(string orderRefId, long amount)
        {
            var momoConfig = _configuration.GetSection("Momo");
            string endpoint = momoConfig["ApiEndpoint"];
            string partnerCode = momoConfig["PartnerCode"];
            string accessKey = momoConfig["AccessKey"];
            string secretKey = momoConfig["SecretKey"];
            string returnUrl = ResolveMomoReturnUrl();
            string notifyUrl = ResolveMomoNotifyUrl();

            if (string.IsNullOrWhiteSpace(endpoint) ||
                string.IsNullOrWhiteSpace(partnerCode) ||
                string.IsNullOrWhiteSpace(accessKey) ||
                string.IsNullOrWhiteSpace(secretKey) ||
                string.IsNullOrWhiteSpace(returnUrl) ||
                string.IsNullOrWhiteSpace(notifyUrl))
            {
                throw new Exception("Thiếu cấu hình MoMo trong appsettings.json.");
            }

            string requestId = Guid.NewGuid().ToString();
            string orderInfo = "Thanh toan checkout " + orderRefId;
            string requestType = "captureWallet";
            string extraData = "";

            string rawHash = "accessKey=" + accessKey +
                             "&amount=" + amount +
                             "&extraData=" + extraData +
                             "&ipnUrl=" + notifyUrl +
                             "&orderId=" + orderRefId +
                             "&orderInfo=" + orderInfo +
                             "&partnerCode=" + partnerCode +
                             "&redirectUrl=" + returnUrl +
                             "&requestId=" + requestId +
                             "&requestType=" + requestType;

            string signature = MomoLibrary.ComputeHmacSha256(rawHash, secretKey);

            var request = new MomoRequest
            {
                partnerCode = partnerCode,
                requestId = requestId,
                amount = amount,
                orderId = orderRefId,
                orderInfo = orderInfo,
                redirectUrl = returnUrl,
                ipnUrl = notifyUrl,
                requestType = requestType,
                extraData = extraData,
                signature = signature
            };

            string responseJson = await MomoLibrary.SendPaymentRequest(endpoint, JsonSerializer.Serialize(request));
            if (string.IsNullOrWhiteSpace(responseJson))
            {
                throw new Exception("MoMo không trả về dữ liệu.");
            }

            var response = JsonSerializer.Deserialize<MomoResponse>(responseJson);
            if (response == null)
            {
                throw new Exception("MoMo trả về dữ liệu không hợp lệ: " + responseJson);
            }
            if (response.resultCode != 0)
            {
                var msg = string.IsNullOrWhiteSpace(response.message) ? "MoMo tạo giao dịch thất bại." : response.message;
                throw new Exception($"MoMo lỗi {response.resultCode}: {msg}");
            }
            if (string.IsNullOrWhiteSpace(response.payUrl))
            {
                throw new Exception("MoMo không trả về payUrl.");
            }

            return response.payUrl;
        }

        private string ResolveMomoReturnUrl()
        {
            var configured = _configuration["Momo:ReturnUrl"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                if (Uri.TryCreate(configured, UriKind.Absolute, out var uri))
                {
                    var host = uri.Host?.ToLowerInvariant();
                    if (host != "localhost" && host != "127.0.0.1")
                    {
                        return configured;
                    }
                }
            }

            var request = HttpContext?.Request;
            if (request == null)
            {
                return configured ?? string.Empty;
            }

            return $"{request.Scheme}://{request.Host}/api/admin/CheckInOut/momo-callback";
        }

        private string ResolveMomoNotifyUrl()
        {
            var configured = _configuration["Momo:NotifyUrl"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                if (Uri.TryCreate(configured, UriKind.Absolute, out var uri))
                {
                    var host = uri.Host?.ToLowerInvariant();
                    if (host != "localhost" && host != "127.0.0.1")
                    {
                        return configured;
                    }
                }
            }

            var request = HttpContext?.Request;
            if (request == null)
            {
                return configured ?? string.Empty;
            }

            return $"{request.Scheme}://{request.Host}/api/admin/CheckInOut/momo-ipn";
        }

        private static string BuildMomoOrderId(int maTt)
        {
            return $"CK{maTt}_{DateTime.UtcNow.Ticks}";
        }

        private static bool TryParseMomoOrderId(string orderId, out int maTt)
        {
            maTt = 0;
            if (string.IsNullOrWhiteSpace(orderId))
            {
                return false;
            }

            var parts = orderId.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return false;
            }

            var core = parts[0];
            if (core.StartsWith("CK", StringComparison.OrdinalIgnoreCase))
            {
                core = core.Substring(2);
            }

            return int.TryParse(core, out maTt);
        }

        private static string MomoSafe(string? value)
        {
            return value ?? string.Empty;
        }

        private static string BuildMomoNotifyRawHash(MomoNotifyRequest payload, string accessKey)
        {
            return "accessKey=" + MomoSafe(accessKey) +
                   "&amount=" + MomoSafe(payload.Amount) +
                   "&extraData=" + MomoSafe(payload.ExtraData) +
                   "&message=" + MomoSafe(payload.Message) +
                   "&orderId=" + MomoSafe(payload.OrderId) +
                   "&orderInfo=" + MomoSafe(payload.OrderInfo) +
                   "&orderType=" + MomoSafe(payload.OrderType) +
                   "&partnerCode=" + MomoSafe(payload.PartnerCode) +
                   "&payType=" + MomoSafe(payload.PayType) +
                   "&requestId=" + MomoSafe(payload.RequestId) +
                   "&responseTime=" + MomoSafe(payload.ResponseTime) +
                   "&resultCode=" + MomoSafe(payload.ResultCode) +
                   "&transId=" + MomoSafe(payload.TransId);
        }
    }

    public class MomoNotifyRequest
    {
        [JsonPropertyName("partnerCode")]
        public string PartnerCode { get; set; }

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("amount")]
        public string Amount { get; set; }

        [JsonPropertyName("orderInfo")]
        public string OrderInfo { get; set; }

        [JsonPropertyName("orderType")]
        public string OrderType { get; set; }

        [JsonPropertyName("transId")]
        public string TransId { get; set; }

        [JsonPropertyName("resultCode")]
        public string ResultCode { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("payType")]
        public string PayType { get; set; }

        [JsonPropertyName("responseTime")]
        public string ResponseTime { get; set; }

        [JsonPropertyName("extraData")]
        public string ExtraData { get; set; }

        [JsonPropertyName("signature")]
        public string Signature { get; set; }
    }
}
