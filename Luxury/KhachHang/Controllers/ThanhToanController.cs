using Luxury.Helpers;
using Luxury.KhachHang.ViewModel;
using Luxury.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KhachSanLuxury.KhachHang.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThanhToanController : ControllerBase
    {
        private readonly LuxuryHotelContext _context;
        private readonly IConfiguration _configuration;

        private const string LoaiDatCoc = "DatCoc";
        private const string LoaiThanhToan = "ThanhToan";
        private const string TrangThaiChoXacNhan = "ChoXacNhan";
        private const string TrangThaiDaThanhToan = "DaThanhToan";

        public ThanhToanController(LuxuryHotelContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("dat-coc")]
        public async Task<IActionResult> DatCoc([FromBody] ThanhToanRequest request)
        {
            if (request.MaDatPhong == null || request.MaDatPhong <= 0)
            {
                return BadRequest("MaDatPhong không hợp lệ");
            }

            if (request.SoTien <= 0)
            {
                return BadRequest("Số tiền không hợp lệ");
            }

            var datPhongExists = _context.Datphongs.Any(x => x.MaDatPhong == request.MaDatPhong);
            if (!datPhongExists)
            {
                return NotFound("Không tìm thấy đặt phòng");
            }

            var thanhToan = new Thanhtoan
            {
                MaDatPhong = request.MaDatPhong,
                LoaiThanhToan = LoaiDatCoc,
                SoTien = request.SoTien,
                HinhThucThanhToan = request.HinhThucThanhToan,
                TrangThai = TrangThaiChoXacNhan
            };

            _context.Thanhtoans.Add(thanhToan);
            _context.SaveChanges();

            var method = NormalizePaymentMethod(request.HinhThucThanhToan);
            if (string.IsNullOrWhiteSpace(method))
            {
                return BadRequest("Thiếu HinhThucThanhToan.");
            }
            if (method == "visa")
            {
                MarkPaymentSuccess(thanhToan.MaTt);
                return Ok(new
                {
                    message = "Tạo đặt cọc thành công",
                    maTt = thanhToan.MaTt,
                    redirectUrl = BuildFrontendUrl("/xac-nhan-dat-phong?status=success&demo=1")
                });
            }

            if (IsOnlineMethod(method))
            {
                try
                {
                    var redirectUrl = await CreatePaymentRedirectUrl(method, thanhToan.MaTt, request.SoTien);
                    if (string.IsNullOrEmpty(redirectUrl))
                    {
                        return StatusCode(400, $"Không tạo được URL thanh toán ({method}).");
                    }
                    return Ok(new { message = "Tạo đặt cọc thành công", maTt = thanhToan.MaTt, redirectUrl });
                }
                catch (Exception ex)
                {
                    return StatusCode(400, ex.Message);
                }
            }

            return Ok(new { message = "Tạo đặt cọc thành công", maTt = thanhToan.MaTt });
        }

        [HttpPost("thanh-toan")]
        public async Task<IActionResult> ThanhToan([FromBody] ThanhToanRequest request)
        {
            if ((request.MaDatPhong == null || request.MaDatPhong <= 0) && (request.MaHd == null || request.MaHd <= 0))
            {
                return BadRequest("Cần cung cấp MaDatPhong hoặc MaHd");
            }

            if (request.SoTien <= 0)
            {
                return BadRequest("Số tiền không hợp lệ");
            }

            // Kiểm tra và xử lý Voucher nếu có
            if (!string.IsNullOrEmpty(request.MaCode))
            {
                var voucher = _context.Vouchers.FirstOrDefault(v => v.MaCode == request.MaCode.ToUpper());

                if (voucher == null || voucher.TrangThai == false)
                    return BadRequest("Mã giảm giá không tồn tại hoặc đã bị khóa.");

                if (voucher.NgayKetThuc < System.DateTime.Now)
                    return BadRequest("Mã giảm giá đã hết hạn sử dụng.");

                if (voucher.SoLuongDaDung >= voucher.SoLuong)
                    return BadRequest("Mã giảm giá đã hết lượt sử dụng.");

                // Cập nhật số lượng đã dùng của Voucher
                voucher.SoLuongDaDung += 1;

                // Gán mã Voucher vào thông tin Đặt phòng để lưu vết
                if (request.MaDatPhong != null)
                {
                    var datPhong = _context.Datphongs.FirstOrDefault(x => x.MaDatPhong == request.MaDatPhong);
                    if (datPhong != null)
                    {
                        datPhong.MaCode = voucher.MaCode;
                    }
                }
            }

            if (request.MaDatPhong != null)
            {
                var datPhongExists = _context.Datphongs.Any(x => x.MaDatPhong == request.MaDatPhong);
                if (!datPhongExists)
                {
                    return NotFound("Không tìm thấy đặt phòng");
                }
            }

            if (request.MaHd != null)
            {
                var hoaDonExists = _context.Hoadons.Any(x => x.MaHd == request.MaHd);
                if (!hoaDonExists)
                {
                    return NotFound("Không tìm thấy hóa đơn");
                }
            }

            var thanhToan = new Thanhtoan
            {
                MaDatPhong = request.MaDatPhong,
                MaHd = request.MaHd,
                LoaiThanhToan = LoaiThanhToan,
                SoTien = request.SoTien,
                HinhThucThanhToan = request.HinhThucThanhToan,
                TrangThai = TrangThaiChoXacNhan
            };

            _context.Thanhtoans.Add(thanhToan);
            _context.SaveChanges();

            var method = NormalizePaymentMethod(request.HinhThucThanhToan);
            if (string.IsNullOrWhiteSpace(method))
            {
                return BadRequest("Thiếu HinhThucThanhToan.");
            }
            if (method == "visa")
            {
                MarkPaymentSuccess(thanhToan.MaTt);
                return Ok(new
                {
                    message = "Tạo thanh toán thành công",
                    maTt = thanhToan.MaTt,
                    redirectUrl = BuildFrontendUrl("/xac-nhan-dat-phong?status=success&demo=1")
                });
            }

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

            return Ok(new { message = "Tạo thanh toán thành công", maTt = thanhToan.MaTt });
        }

        [HttpPut("xac-nhan/{maTt}")]
        public IActionResult XacNhanThanhToan(int maTt)
        {
            if (maTt <= 0)
            {
                return BadRequest("MaTt không hợp lệ");
            }

            var thanhToan = _context.Thanhtoans.FirstOrDefault(x => x.MaTt == maTt);
            if (thanhToan == null)
            {
                return NotFound("Không tìm thấy thanh toán");
            }

            if (thanhToan.TrangThai == TrangThaiDaThanhToan)
            {
                return Ok(new { message = "Thanh toán đã được xác nhận", maTt = thanhToan.MaTt });
            }

            thanhToan.TrangThai = TrangThaiDaThanhToan;

            if (thanhToan.MaDatPhong != null)
            {
                var datPhong = _context.Datphongs.FirstOrDefault(x => x.MaDatPhong == thanhToan.MaDatPhong);
                if (datPhong != null)
                {
                    datPhong.TrangThai = TrangThaiDaThanhToan;
                }
            }

            _context.SaveChanges();

            return Ok(new { message = "Xác nhận thanh toán thành công", maTt = thanhToan.MaTt });
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
                return Redirect(BuildFrontendUrl("/payment?status=fail"));
            }

            string responseCode = vnPayLibrary.GetResponseData("vnp_ResponseCode");
            string txnRef = vnPayLibrary.GetResponseData("vnp_TxnRef");
            if (responseCode == "00" && int.TryParse(txnRef, out var maTt))
            {
                MarkPaymentSuccess(maTt);
                return Redirect(BuildFrontendUrl("/xac-nhan-dat-phong?status=success"));
            }

            return Redirect(BuildFrontendUrl("/payment?status=fail"));
        }

        [HttpGet("momo-callback")]
        public IActionResult MomoCallback()
        {
            var resultCode = Request.Query["resultCode"].ToString();
            var orderId = Request.Query["orderId"].ToString();

            if (resultCode == "0" && TryParseMomoOrderId(orderId, out var maTt))
            {
                MarkPaymentSuccess(maTt);
                return Redirect(BuildFrontendUrl("/xac-nhan-dat-phong?status=success"));
            }

            return Redirect(BuildFrontendUrl("/payment?status=fail"));
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

            if (thanhToan.MaDatPhong != null)
            {
                var datPhong = _context.Datphongs
                    .Include(x => x.Chitietdatphongs)
                    .ThenInclude(x => x.MaPhongNavigation)
                    .FirstOrDefault(x => x.MaDatPhong == thanhToan.MaDatPhong);

                if (datPhong != null)
                {
                    datPhong.TrangThai = TrangThaiDaThanhToan;

                    foreach (var ct in datPhong.Chitietdatphongs)
                    {
                        if (ct.MaPhongNavigation != null)
                        {
                            ct.MaPhongNavigation.TrangThai = 2;
                        }
                    }
                }
            }

            _context.SaveChanges();
        }

        private string NormalizePaymentMethod(string? method)
        {
            return (method ?? string.Empty).Trim().ToLowerInvariant();
        }

        private bool IsOnlineMethod(string method)
        {
            return method == "vnpay" || method == "momo";
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
            vnPayHelper.AddRequestData("vnp_OrderInfo", "Thanh toan dat phong " + orderRefId);
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

            return $"{request.Scheme}://{request.Host}/api/ThanhToan/vnpay-callback";
        }

        private async Task<string?> CreateMomoUrlInternal(string orderRefId, long amount)
        {
            var momoConfig = _configuration.GetSection("Momo");
            string endpoint = momoConfig["ApiEndpoint"];
            string partnerCode = momoConfig["PartnerCode"];
            string accessKey = momoConfig["AccessKey"];
            string secretKey = momoConfig["SecretKey"];
            string returnUrl = momoConfig["ReturnUrl"];
            string notifyUrl = momoConfig["NotifyUrl"];

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
            string orderInfo = "Thanh toan dat phong " + orderRefId;
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

        private static string BuildMomoOrderId(int maTt)
        {
            return $"TT{maTt}_{DateTime.UtcNow.Ticks}";
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
            if (core.StartsWith("TT", StringComparison.OrdinalIgnoreCase))
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