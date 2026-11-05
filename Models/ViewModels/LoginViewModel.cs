using System.ComponentModel.DataAnnotations;

namespace QLSanBong.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Email hoặc Số điện thoại đăng nhập.")]
        [StringLength(100, ErrorMessage = "Thông tin tài khoản không được vượt quá 100 ký tự.")]
        [Display(Name = "Email / Số điện thoại")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}

