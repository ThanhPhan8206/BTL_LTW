using System.ComponentModel.DataAnnotations;

namespace QLSanBong.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên đầy đủ.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ và tên phải từ 2 đến 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ Email.")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không đúng định dạng (ví dụ: sinhvien@st.utc.edu.vn).")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
        [Display(Name = "Địa chỉ Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại liên hệ.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại phải gồm 10-11 chữ số (bắt đầu bằng 0 hoặc +84).")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự.")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu xác nhận.")]
        [DataType(DataType.Password)]
        [Compare("MatKhau", ErrorMessage = "Mật khẩu xác nhận không khớp với mật khẩu đã nhập.")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string XacNhanMatKhau { get; set; } = string.Empty;
    }
}

