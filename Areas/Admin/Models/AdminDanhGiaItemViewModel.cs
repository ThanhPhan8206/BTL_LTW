using System;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminDanhGiaItemViewModel
    {
        public int Id { get; set; }
        public int DatSanId { get; set; }

        public string TenKhachHang { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string TenSan { get; set; } = string.Empty;
        public string LoaiSan { get; set; } = string.Empty;

        public int SoSao { get; set; }
        public DateTime? NgayTao { get; set; }

        public string BinhLuanGoc { get; set; } = string.Empty;
        public string? PhanHoiAdmin { get; set; }
        public bool DaAn { get; set; }
        public string BinhLuanRaw { get; set; } = string.Empty;
    }
}

