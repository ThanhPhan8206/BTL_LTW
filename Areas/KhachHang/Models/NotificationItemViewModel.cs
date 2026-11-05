namespace QLSanBong.Areas.KhachHang.Models
{
    public class NotificationItemViewModel
    {
        public int Id { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
        public string LoaiThongBao { get; set; } = "HeThong";
        public bool DaDoc { get; set; }
        public DateTime NgayTao { get; set; } = DateTime.Now;

        public string ThoiGianTuongDoi
        {
            get
            {
                var diff = DateTime.Now - NgayTao;
                if (diff.TotalMinutes < 1) return "Vừa xong";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} giờ trước";
                if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} ngày trước";
                return NgayTao.ToString("dd/MM/yyyy HH:mm");
            }
        }

        public string BadgeClass => LoaiThongBao switch
        {
            "DuyetDon" => "bg-success text-white",
            "TuChoi" => "bg-danger text-white",
            "HoanThanh" => "bg-primary text-white",
            "DoiLich" => "bg-warning text-dark",
            _ => "bg-secondary text-white"
        };

        public string BadgeText => LoaiThongBao switch
        {
            "DuyetDon" => "Đã duyệt",
            "TuChoi" => "Từ chối",
            "HoanThanh" => "Hoàn thành",
            "DoiLich" => "Đổi lịch",
            _ => "Hệ thống"
        };

        public string IconClass => LoaiThongBao switch
        {
            "DuyetDon" => "bi-check-circle-fill text-success",
            "TuChoi" => "bi-x-circle-fill text-danger",
            "HoanThanh" => "bi-award-fill text-primary",
            "DoiLich" => "bi-arrow-repeat text-warning",
            _ => "bi-info-circle-fill text-info"
        };
    }
}

