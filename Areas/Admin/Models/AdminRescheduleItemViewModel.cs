using System;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminRescheduleItemViewModel
    {
        public int Id { get; set; }
        public int DatSanId { get; set; }

        public int NguoiDungId { get; set; }
        public string TenNguoiDung { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;

        // Lịch cũ
        public int SanBongCuId { get; set; }
        public string TenSanCu { get; set; } = string.Empty;
        public DateOnly NgayCu { get; set; }
        public string KhungGioCu { get; set; } = string.Empty;

        // Lịch mới đề xuất
        public int SanBongMoiId { get; set; }
        public string TenSanMoi { get; set; } = string.Empty;
        public DateOnly NgayMoi { get; set; }
        public int KhungGioMoiId { get; set; }
        public string KhungGioMoi { get; set; } = string.Empty;

        public string LyDo { get; set; } = string.Empty;
        public string TrangThai { get; set; } = "Chờ duyệt";
        public DateTime? NgayTao { get; set; }
    }
}
