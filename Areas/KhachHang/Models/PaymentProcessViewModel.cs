namespace QLSanBong.Areas.KhachHang.Models
{
    public class PaymentProcessViewModel
    {
        public int DatSanId { get; set; }
        public string MaHoaDon { get; set; } = string.Empty;
        public string TenSan { get; set; } = string.Empty;
        public string LoaiSan { get; set; } = string.Empty;
        public DateOnly NgayDat { get; set; }
        public string KhungGioDisplay { get; set; } = string.Empty;

        public string HoTenNguoiDat { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;

        public decimal TongTienDon { get; set; }
        public decimal SoTienThanhToan { get; set; }
        public decimal SoTienConLai { get; set; }

        public string LoaiThanhToan { get; set; } = "Đặt cọc";
        public string PhuongThuc { get; set; } = "Chuyển khoản VietQR";
        public string TrangThaiThanhToan { get; set; } = "Chờ chuyển khoản";
        public string TrangThaiDatSan { get; set; } = "Chờ duyệt";

        // Cú pháp nội dung chuẩn VietQR
        public string NoiDungChuyenKhoan { get; set; } = string.Empty;

        // Cấu hình ngân hàng thụ hưởng VietQR
        public string BankId { get; set; } = "MB"; // Ngân hàng Quân Đội
        public string BankName { get; set; } = "MB Bank (Ngân hàng TMCP Quân Đội)";
        public string AccountNumber { get; set; } = "0988686868";
        public string AccountName { get; set; } = "UTC STADIUM SAN BONG";

        // Link sinh ảnh VietQR động
        public string VietQrImageUrl { get; set; } = string.Empty;
        public DateTime? NgayTao { get; set; }
    }
}

