namespace QLSanBong.Areas.KhachHang.Models
{
    public class BookingHistoryItemModel
    {
        public int DatSanId { get; set; }
        public int SanBongId { get; set; }
        public string TenSan { get; set; } = string.Empty;
        public string LoaiSan { get; set; } = string.Empty;
        public string AnhSan { get; set; } = "/uploads/sanbong/san-5-so-1.jpg";

        public DateOnly NgayDat { get; set; }
        public TimeOnly GioBatDau { get; set; }
        public TimeOnly GioKetThuc { get; set; }
        public string KhungGioDisplay => $"{GioBatDau:HH:mm} - {GioKetThuc:HH:mm}";

        public decimal TongTien { get; set; }
        public decimal DaThanhToan { get; set; }
        public decimal ConLai => Math.Max(0, TongTien - DaThanhToan);

        public string TrangThaiDatSan { get; set; } = "Chờ duyệt";
        public string TrangThaiThanhToan { get; set; } = "Chờ thanh toán";
        public string? MaHoaDon { get; set; }
        public DateTime? NgayTao { get; set; }

        public List<string> DanhSachDichVu { get; set; } = new();

        // Ràng buộc đánh giá sân (Feedback & Rating - Verified Booking Only)
        public bool DaDanhGia { get; set; }
        public int? SoSaoDanhGia { get; set; }
        public string? BinhLuanDanhGia { get; set; }

        // Time-lock Rule: Ràng buộc khóa trước giờ bóng lăn 2 tiếng
        public bool CanRescheduleOrCancel { get; set; } = true;
        public double HoursUntilKickoff { get; set; }
        public string TimeLockStatusText { get; set; } = string.Empty;
        public string? LockReason { get; set; }
    }
}

