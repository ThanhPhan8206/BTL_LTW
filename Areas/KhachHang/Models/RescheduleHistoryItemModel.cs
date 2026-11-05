namespace QLSanBong.Areas.KhachHang.Models
{
    public class RescheduleHistoryItemModel
    {
        public int YeuCauId { get; set; }
        public int DatSanId { get; set; }

        public string TenSanCu { get; set; } = string.Empty;
        public string TenSanMoi { get; set; } = string.Empty;

        public DateOnly NgayDatCu { get; set; }
        public DateOnly NgayDatMoi { get; set; }

        public string KhungGioCu { get; set; } = string.Empty;
        public string KhungGioMoi { get; set; } = string.Empty;

        public string? LyDo { get; set; }
        public string TrangThai { get; set; } = "Chờ duyệt";
        public DateTime? NgayTao { get; set; }
    }
}

