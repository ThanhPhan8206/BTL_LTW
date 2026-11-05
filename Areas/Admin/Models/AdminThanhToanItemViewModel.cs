using System;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminThanhToanItemViewModel
    {
        public int Id { get; set; }
        public int DatSanId { get; set; }
        public string TenKhachHang { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string TenSan { get; set; } = string.Empty;
        public string KhungGioDisplay { get; set; } = string.Empty;
        public DateOnly NgayDat { get; set; }
        public decimal SoTien { get; set; }
        public string PhuongThucThanhToan { get; set; } = string.Empty;
        public string LoaiThanhToan { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
        public string? MaGiaoDich { get; set; }
        public DateTime? ThoiGianThanhToan { get; set; }
    }
}

