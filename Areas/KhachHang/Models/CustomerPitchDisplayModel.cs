namespace QLSanBong.Areas.KhachHang.Models
{
    public class CustomerPitchDisplayModel
    {
        public int Id { get; set; }
        public string TenSan { get; set; } = string.Empty;
        public string LoaiSan { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public string TrangThai { get; set; } = "Sẵn sàng";
        public string AnhSan { get; set; } = "/uploads/sanbong/san-5-so-1.jpg";
        public decimal GiaThamKhao { get; set; }
        public decimal? GiaTheoKhungGio { get; set; }
        public string DacDiem { get; set; } = string.Empty;
        public bool IsAvailable { get; set; } = true;
        public string? LyDoKhongKhaDung { get; set; }

        // Đánh giá thực tế từ người dùng (Feedback & Rating)
        public double DiemDanhGiaTrungBinh { get; set; } = 5.0;
        public int TongSoDanhGia { get; set; } = 0;
    }
}
