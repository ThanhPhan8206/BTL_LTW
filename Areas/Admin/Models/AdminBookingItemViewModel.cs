namespace QLSanBong.Areas.Admin.Models
{
    public class AdminBookingItemViewModel
    {
        public int Id { get; set; }
        public int SanBongId { get; set; }
        public string TenSan { get; set; } = string.Empty;
        public string LoaiSan { get; set; } = string.Empty;

        public int NguoiDungId { get; set; }
        public string TenNguoiDung { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public DateOnly NgayDat { get; set; }
        public TimeOnly GioBatDau { get; set; }
        public TimeOnly GioKetThuc { get; set; }
        public string KhungGioDisplay => $"{GioBatDau:HH:mm} - {GioKetThuc:HH:mm}";

        public decimal TongTien { get; set; }
        public decimal DaThanhToan { get; set; }
        public string TrangThai { get; set; } = "Chờ duyệt";
        public string? GhiChu { get; set; }
        public DateTime? NgayTao { get; set; }

        public List<string> DanhSachDichVu { get; set; } = new();

        public bool DaDanhGia { get; set; }
        public int? SoSao { get; set; }
        public string? BinhLuan { get; set; }
    }
}

