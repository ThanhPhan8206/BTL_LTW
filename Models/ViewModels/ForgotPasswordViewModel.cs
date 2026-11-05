using System.ComponentModel.DataAnnotations;

namespace QLSanBong.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ Email đã đăng ký.")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không đúng định dạng.")]
        [Display(Name = "Email tài khoản")]
        public string Email { get; set; } = string.Empty;
    }
}

