namespace QLSanBong.Areas.KhachHang.Models
{
    public class InvoiceDetailViewModel
    {
        public string MaHoaDon { get; set; } = string.Empty;
        public DateTime? NgayXuat { get; set; }
        public int DatSanId { get; set; }

        public string TenKhachHang { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string TenSan { get; set; } = string.Empty;
        public string LoaiSan { get; set; } = string.Empty;
        public DateOnly NgayDat { get; set; }
        public string KhungGioDisplay { get; set; } = string.Empty;

        public decimal TienSan { get; set; }
        public List<InvoiceServiceItemModel> DanhSachDichVu { get; set; } = new();
        public decimal TienDichVu => DanhSachDichVu.Sum(d => d.ThanhTien);

        public string? MaVoucher { get; set; }
        public decimal TienGiamGia { get; set; } = 0;

        public decimal TongTienDon { get; set; }

        public List<InvoicePaymentItemModel> DanhSachThanhToan { get; set; } = new();
        public decimal TongDaThanhToan => DanhSachThanhToan.Where(t => t.TrangThai == "Thành công" || t.TrangThai == "Đã thanh toán").Sum(t => t.SoTien);
        public decimal ConLaiPhaiTra => Math.Max(0, TongTienDon - TongDaThanhToan);

        public string TrangThaiDatSan { get; set; } = "Chờ duyệt";
        public string TrangThaiThanhToan { get; set; } = "Chờ thanh toán";
    }
}

