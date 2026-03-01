namespace Luxury.KhachHang.Services
{
    public class EmailQR
    {
        public static string GetOnlyQRBody(string tenKhach, string maDatPhong, string qrCid)
        {
            return $@"
            <div style='font-family: Segoe UI, Arial, sans-serif; text-align: center; padding: 40px; background-color: #f9f9f9;'>
                <div style='max-width: 400px; margin: auto; background: white; padding: 30px; border-radius: 15px; border: 1px solid #ddd; box-shadow: 0 4px 10px rgba(0,0,0,0.05);'>
                    <h2 style='color: #f35525; margin-top: 0;'>XÁC NHẬN ĐẶT PHÒNG</h2>
                    <p style='color: #333;'>Chào <strong>{tenKhach}</strong>,</p>
                    <p style='color: #666;'>Giao dịch của bạn đã thành công. Dưới đây là mã QR để nhận phòng:</p>
                    
                    <div style='margin: 25px 0;'>
                        <img src='cid:{qrCid}' style='width: 220px; height: 220px; border: 1px solid #eee; padding: 5px;' />
                    </div>

                    <div style='background: #fff5f2; padding: 10px; border-radius: 8px;'>
                        <p style='margin: 0; font-size: 16px; color: #f35525;'><strong>Mã đặt phòng: #{maDatPhong}</strong></p>
                    </div>
                    
                    <p style='color: #999; font-size: 12px; margin-top: 20px;'>
                        Vui lòng trình mã này cho lễ tân khi bạn đến khách sạn.
                    </p>
                </div>
            </div>";
        }
    }
}