using System.ComponentModel.DataAnnotations;

namespace QLSanBong.Areas.KhachHang.Models
{
    public class CreateReviewViewModel
    {
        public int DatSanId { get; set; }
        public int SanBongId { get; set; }
        public string TenSan { get; set; } = string.Empty;
        public string LoaiSan { get; set; } = string.Empty;
        public string AnhSan { get; set; } = "/uploads/sanbong/san-5-so-1.jpg";

        public DateOnly NgayDa { get; set; }
        public string KhungGioDisplay { get; set; } = string.Empty;
        public decimal TongTien { get; set; }
        public string TenNguoiDung { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn số sao đánh giá (từ 1 đến 5 sao).")]
        [Range(1, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5 sao.")]
        public int SoSao { get; set; } = 5;

        [Required(ErrorMessage = "Vui lòng chia sẻ cảm nhận và nhận xét thực tế về chất lượng mặt sân.")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Nội dung bình luận phải có độ dài từ 10 đến 1000 ký tự.")]
        public string BinhLuan { get; set; } = string.Empty;
    }
}

