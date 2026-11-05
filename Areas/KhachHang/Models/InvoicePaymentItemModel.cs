namespace QLSanBong.Areas.KhachHang.Models
{
    public class InvoicePaymentItemModel
    {
        public int Id { get; set; }
        public decimal SoTien { get; set; }
        public string PhuongThuc { get; set; } = "Chuyển khoản VietQR";
        public string LoaiThanhToan { get; set; } = "Đặt cọc";
        public string TrangThai { get; set; } = "Thành công";
        public string? MaGiaoDich { get; set; }
        public DateTime? ThoiGian { get; set; }
    }
}

