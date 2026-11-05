using System;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminLichBaoTriItemViewModel
    {
        public int Id { get; set; }
        public int SanBongId { get; set; }
        public string TenSan { get; set; } = string.Empty;
        public string LoaiSan { get; set; } = string.Empty;
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianKetThuc { get; set; }
        public string? GhiChu { get; set; }

        public string TrangThaiHienTai
        {
            get
            {
                var now = DateTime.Now;
                if (now < ThoiGianBatDau) return "Sắp diễn ra";
                if (now >= ThoiGianBatDau && now <= ThoiGianKetThuc) return "Đang bảo trì";
                return "Đã hoàn tất";
            }
        }
    }
}

