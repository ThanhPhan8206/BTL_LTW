using System.ComponentModel.DataAnnotations;

namespace QLSanBong.Models.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ Email.")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không đúng định dạng.")]
        [Display(Name = "Email tài khoản")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mã xác thực OTP gồm 6 chữ số.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP phải gồm đúng 6 chữ số.")]
        [Display(Name = "Mã xác thực OTP")]
        public string MaOtp { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải có từ 6 đến 100 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string MatKhauMoi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận lại mật khẩu mới.")]
        [DataType(DataType.Password)]
        [Compare("MatKhauMoi", ErrorMessage = "Mật khẩu xác nhận không khớp với mật khẩu mới.")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string XacNhanMatKhauMoi { get; set; } = string.Empty;
    }
}

