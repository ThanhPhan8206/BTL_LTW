using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace QLSanBong.Areas.KhachHang.Models
{
    public class UserProfileViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên của bạn.")]
        [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ Email.")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không đúng định dạng.")]
        [Display(Name = "Địa chỉ Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$", ErrorMessage = "Số điện thoại không hợp lệ (gồm 10 chữ số, đầu số Việt Nam).")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; } = string.Empty;

        [Display(Name = "Ảnh đại diện")]
        public string? AnhDaiDienUrl { get; set; }

        [Display(Name = "Tải lên ảnh mới")]
        public IFormFile? AvatarFile { get; set; }

        public string? TenQuyen { get; set; } = "Khách hàng sinh viên";
        public string? TrangThai { get; set; } = "Hoạt động";
        public DateTime? NgayTao { get; set; }
        public DateTime? NgayCapNhat { get; set; }
    }
}

