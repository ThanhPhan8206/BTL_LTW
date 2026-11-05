namespace QLSanBong.Areas.KhachHang.Models
{
    public class PitchReviewDisplayModel
    {
        public int Id { get; set; }
        public int DatSanId { get; set; }
        public int SanBongId { get; set; }
        public string TenNguoiDung { get; set; } = string.Empty;
        public string? AnhDaiDien { get; set; }
        public int SoSao { get; set; }
        public string? BinhLuan { get; set; }
        public DateTime? NgayTao { get; set; }
        public string TenSan { get; set; } = string.Empty;
        public string LoaiSan { get; set; } = string.Empty;
        public bool IsVerifiedBooking { get; set; } = true;
        public bool IsCurrentUser { get; set; }

        public string ThoiGianTuongDoi
        {
            get
            {
                if (!NgayTao.HasValue) return "Vừa xong";
                var diff = DateTime.Now - NgayTao.Value;
                if (diff.TotalMinutes < 1) return "Vừa xong";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} giờ trước";
                if (diff.TotalDays < 30) return $"{(int)diff.TotalDays} ngày trước";
                return NgayTao.Value.ToString("dd/MM/yyyy");
            }
        }
    }
}

