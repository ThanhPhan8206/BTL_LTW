using System.ComponentModel.DataAnnotations;
using QLSanBong.Models.Entities;

namespace QLSanBong.Areas.KhachHang.Models
{
    public class RescheduleRequestViewModel
    {
        [Required]
        public int DatSanId { get; set; }

        public string TenSanHienTai { get; set; } = string.Empty;
        public string LoaiSanHienTai { get; set; } = string.Empty;
        public DateOnly NgayDatHienTai { get; set; }
        public string KhungGioHienTai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn sân bóng muốn đổi sang.")]
        [Display(Name = "Sân bóng mới")]
        public int SanBongMoiId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày thi đấu mới.")]
        [Display(Name = "Ngày đá mới")]
        public DateOnly NgayDatMoi { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        [Required(ErrorMessage = "Vui lòng chọn khung giờ thi đấu mới.")]
        [Display(Name = "Khung giờ mới")]
        public int KhungGioMoiId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lý do đổi lịch.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Lý do phải có từ 5 đến 500 ký tự.")]
        [Display(Name = "Lý do xin đổi lịch")]
        public string LyDo { get; set; } = string.Empty;

        // Time-lock check
        public bool IsTimeLockPassed { get; set; } = false;
        public string? TimeLockWarning { get; set; }

        public List<SanBong> DanhSachSan { get; set; } = new();
        public List<KhungGio> DanhSachKhungGio { get; set; } = new();
    }
}

