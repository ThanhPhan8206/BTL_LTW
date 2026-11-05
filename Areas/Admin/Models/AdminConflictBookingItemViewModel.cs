using System;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminConflictBookingItemViewModel
    {
        public int Id { get; set; }
        public string TenKhachHang { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string TenSan { get; set; } = string.Empty;
        public DateOnly NgayDat { get; set; }
        public string KhungGioDisplay { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
        public decimal TongTien { get; set; }
    }
}

