namespace QLSanBong.Areas.KhachHang.Models
{
    public class PitchReviewsViewModel
    {
        public int SanBongId { get; set; }
        public string TenSan { get; set; } = string.Empty;
        public string LoaiSan { get; set; } = string.Empty;
        public decimal GiaThamKhao { get; set; }
        public string AnhSan { get; set; } = "/uploads/sanbong/san-5-so-1.jpg";

        public double DiemTrungBinh { get; set; }
        public int TongSoDanhGia { get; set; }

        public int Sao5Count { get; set; }
        public int Sao4Count { get; set; }
        public int Sao3Count { get; set; }
        public int Sao2Count { get; set; }
        public int Sao1Count { get; set; }

        public List<PitchReviewDisplayModel> DanhSachDanhGia { get; set; } = new();

        // Ràng buộc điều kiện: Cho phép viết đánh giá nếu người dùng có đơn "Hoàn thành" chưa đánh giá cho sân này
        public bool CoDonChoDanhGia { get; set; }
        public int? DatSanIdChoDanhGia { get; set; }
    }
}

