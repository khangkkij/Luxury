using System.ComponentModel.DataAnnotations;

namespace Luxury.KhachHang.ViewModel
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        public string HoVaTen { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string MatKhau { get; set; }
        [Required(ErrorMessage = "Giới tính không được để trống")]
        public string? GioiTinh { get; set; }
        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        public string? Sdt { get; set; }
        [Required(ErrorMessage = "Căn cước công dân không được để trống")]
        public string? Cccd { get; set; }
    }
}
